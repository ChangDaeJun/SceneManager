using System.Diagnostics;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Core.Services;

/// <summary>
/// 씬 적용 오케스트레이터. 로드 → 의존성 순서 결정 → 실행 → 창 대기 → 배치.
/// v0: 이전 씬 정리·오디오·모니터 매핑은 생략. 부분 실패를 허용한다.
/// </summary>
public sealed class SceneRunner : ISceneRunner
{
    private readonly ISceneRepository _repository;
    private readonly IDesktopManager _desktop;
    private readonly ProcessFilter _filter;

    // 이번 적용에서 이미 특정 항목에 배정된 창 핸들(같은 프로세스의 여러 창 중복 배정 방지).
    private readonly HashSet<IntPtr> _claimed = new();

    // 같은 프로세스명으로 씬에 등록된 항목 수. >1이면(예: 엑셀 2창) 제목으로 엄격히
    // 구분하고 "가장 큰 창" 폴백을 쓰지 않는다(형제 문서 창을 잘못 잡는 것 방지).
    private readonly Dictionary<string, int> _entryCountByProcess = new(StringComparer.OrdinalIgnoreCase);

    // 이번 적용에서 배치한 창의 모서리를 각지게 처리할지(씬 옵션).
    private bool _squareCorners;

    public SceneRunner(
        ISceneRepository repository,
        IDesktopManager desktop,
        ProcessFilter filter)
    {
        _repository = repository;
        _desktop = desktop;
        _filter = filter;
    }

    public event EventHandler<SceneProgressEventArgs>? ProgressChanged;

    public async Task<SceneApplyResult> ApplyAsync(string sceneId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SceneApplyResult { SceneId = sceneId, Success = true };
        _claimed.Clear();

        var scene = await _repository.GetByIdAsync(sceneId, cancellationToken);
        if (scene is null)
        {
            result.Success = false;
            result.Steps.Add(new StepResult { StepName = "Load scene", Success = false, ErrorMessage = "씬을 찾을 수 없음" });
            result.Elapsed = stopwatch.Elapsed;
            return result;
        }

        _squareCorners = scene.SquareCorners;

        // 같은 프로세스로 등록된 항목 수 집계(창 매칭 폴백 정책에 사용).
        _entryCountByProcess.Clear();
        foreach (var p in scene.Programs)
        {
            var pn = Path.GetFileNameWithoutExtension(p.ExecPath);
            _entryCountByProcess[pn] = _entryCountByProcess.GetValueOrDefault(pn) + 1;
        }

        // 의존성 순서대로 그룹화(같은 그룹은 병렬 가능하지만 v0는 순차 실행).
        var levels = DependencyResolver.Resolve(scene.Programs);
        var totalSteps = scene.Programs.Count;
        var currentStep = 0;

        foreach (var level in levels)
        {
            foreach (var program in level)
            {
                cancellationToken.ThrowIfCancellationRequested();
                currentStep++;
                ReportProgress($"실행: {program.Name}", currentStep, totalSteps);

                var step = await ApplyProgramAsync(program, cancellationToken);
                result.Steps.Add(step);
                if (!step.Success)
                    result.Success = false;

                if (program.DelayAfterMs > 0)
                    await Task.Delay(program.DelayAfterMs, cancellationToken);
            }
        }

        result.Elapsed = stopwatch.Elapsed;
        return result;
    }

    public Task<int> ClearAsync(CancellationToken cancellationToken = default)
    {
        var self = Environment.ProcessId; // 자기 자신(러너)의 창은 닫지 않는다
        var closed = 0;

        foreach (var window in _desktop.GetAllVisibleWindows())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (window.ProcessId == self)
                continue;
            if (!_filter.ShouldInclude(window.ProcessName))
                continue; // 시스템/셸 프로세스는 건드리지 않음

            _desktop.CloseWindow(window.Handle);
            closed++;
        }

        return Task.FromResult(closed);
    }

    private async Task<StepResult> ApplyProgramAsync(ProgramEntry program, CancellationToken cancellationToken)
    {
        // 실행 파일명 기준 프로세스명. 같은 프로세스의 여러 창(예: 카카오톡 본체 vs 대화방)은
        // 창 제목으로 구분하고, 한 번 배정한 창은 다시 잡지 않는다.
        var processName = Path.GetFileNameWithoutExtension(program.ExecPath);

        // 열어야 할 특정 문서/URL이 있으면(arguments), 제목이 일치하는 창이 있을 때만
        // "이미 열림"으로 보고, 없으면 (프로세스가 떠 있어도) 그 문서/URL을 새로 연다.
        // 예: 브라우저가 이미 실행 중이어도 씬의 URL을 열어야 한다.
        // 같은 프로세스로 여러 항목이 등록된 경우(엑셀 2창)에도 형제 창 오점유 방지를 위해 폴백을 끈다.
        var hasArguments = !string.IsNullOrWhiteSpace(program.Arguments);
        var single = _entryCountByProcess.GetValueOrDefault(processName) <= 1;

        // "이미 열림" 판정: 특정 URL/파일을 여는 항목(arguments)은 제목이 일치할 때만 이미 열림으로
        // 보고, 아니면 프로세스가 떠 있어도 새로 연다(브라우저에 URL 열기 등). 다중 항목도 폴백 금지.
        var target = SelectWindow(processName, program.WindowTitle, allowFallback: !hasArguments && single);
        var alreadyOpen = target is not null;

        var step = new StepResult
        {
            StepName = $"{(alreadyOpen ? "Reposition" : "Launch")} {program.Name}",
            Success = true,
        };

        if (!alreadyOpen)
        {
            var launch = await _desktop.LaunchAsync(program, cancellationToken);
            if (!launch.Success)
            {
                step.Success = false;
                step.ErrorMessage = launch.ErrorMessage ?? "실행 실패";
                return step;
            }

            if (program.Window is null)
                return step; // 배치 없이 실행만

            // 실행 후에는 방금 연 창을 찾는 단계이므로, 형제 항목이 없으면(단일) 폴백을 허용한다.
            target = await WaitForWindowAsync(processName, program.WindowTitle, single, program.SettleTimeoutMs, cancellationToken);
            if (target is null)
            {
                step.Success = false;
                step.ErrorMessage = "창을 찾지 못함(타임아웃)";
                return step;
            }
        }

        // 이 창을 이 항목에 배정(같은 프로세스의 다른 항목이 다시 잡지 못하도록).
        _claimed.Add(target!.Handle);

        if (_squareCorners)
            _desktop.SetCornerPreference(target.Handle, true);

        if (program.Window is null)
            return step; // 이미 실행 중 + 배치 없음

        var (settled, last) = await SettleHandleAsync(
            target.Handle, processName, program.WindowTitle, single, program.Window, program.SettleTimeoutMs, cancellationToken);
        if (!settled)
        {
            var t = program.Window;
            step.Success = false;
            step.ErrorMessage =
                $"위치 안정화 실패(타임아웃) 목표=({t.X},{t.Y}) {t.Width}x{t.Height}/{t.State}, " +
                $"현재=({last.X},{last.Y}) {last.Width}x{last.Height}/{last.State}";
        }

        return step;
    }

    /// <summary>
    /// 이 항목에 배정할 창을 고른다: 같은 프로세스명 + 아직 배정 안 된 창 중에서
    /// 제목 정확 일치 &gt; 부분 일치 순. 매칭 실패 시 <paramref name="allowFallback"/>가
    /// true면 가장 큰 창으로 폴백하고, false면 null(→ 새로 실행하게 함).
    /// </summary>
    private WindowInfo? SelectWindow(string processName, string? title, bool allowFallback)
    {
        var candidates = _desktop.GetAllVisibleWindows()
            .Where(w => string.Equals(w.ProcessName, processName, StringComparison.OrdinalIgnoreCase)
                        && !_claimed.Contains(w.Handle))
            .ToList();
        if (candidates.Count == 0)
            return null;

        if (!string.IsNullOrEmpty(title))
        {
            var exact = candidates.FirstOrDefault(
                w => string.Equals(w.WindowTitle, title, StringComparison.Ordinal));
            if (exact is not null)
                return exact;

            var partial = candidates.FirstOrDefault(w =>
                (w.WindowTitle.Length > 0 && title.Contains(w.WindowTitle, StringComparison.OrdinalIgnoreCase))
                || w.WindowTitle.Contains(title, StringComparison.OrdinalIgnoreCase));
            if (partial is not null)
                return partial;
        }

        // 매칭 실패: 폴백이 허용될 때만 가장 큰 창으로(스플래시 제외 효과).
        // arguments로 특정 문서/URL을 여는 항목이나 같은 프로세스 다중 항목은 폴백 금지.
        return allowFallback
            ? candidates.OrderByDescending(w => w.Placement.Width * w.Placement.Height).First()
            : null;
    }

    /// <summary>실행 직후, 배정 가능한 창이 나타날 때까지 대기한다. 타임아웃 시 null.</summary>
    private async Task<WindowInfo?> WaitForWindowAsync(
        string processName, string? title, bool allowFallback, int settleTimeoutMs, CancellationToken cancellationToken)
    {
        var timeoutMs = settleTimeoutMs > 0 ? settleTimeoutMs : DefaultSettleTimeoutMs;
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var window = SelectWindow(processName, title, allowFallback);
            if (window is not null)
                return window;

            await Task.Delay(PlacementPollIntervalMs, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// 지정한 창(<paramref name="hwnd"/>)이 <paramref name="target"/> 위치에 안정적으로
    /// 자리 잡을 때까지 주기적으로 재배치한다(무거운 앱·자기 레이아웃 복원 대응).
    /// 연속 <see cref="StableChecksNeeded"/>회 목표와 일치하면 성공.
    /// </summary>
    private async Task<(bool Settled, WindowPlacement Last)> SettleHandleAsync(
        IntPtr hwnd, string processName, string? title, bool allowFallback, WindowPlacement target,
        int settleTimeoutMs, CancellationToken cancellationToken)
    {
        var timeoutMs = settleTimeoutMs > 0 ? settleTimeoutMs : DefaultSettleTimeoutMs;
        var stopwatch = Stopwatch.StartNew();
        var stableCount = 0;
        var current = _desktop.GetPlacement(hwnd);

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            current = _desktop.GetPlacement(hwnd);

            // 잡고 있던 창이 사라짐(예: 엑셀이 초기 임시 창을 실제 문서 창으로 교체).
            // 죽은 핸들(0x0)이면 점유를 풀고 제목으로 다시 창을 선택한다.
            if (IsDeadHandle(current))
            {
                _claimed.Remove(hwnd);
                var replacement = SelectWindow(processName, title, allowFallback);
                if (replacement is null)
                {
                    await Task.Delay(PlacementPollIntervalMs, cancellationToken);
                    continue; // 새 창이 나타날 때까지 대기
                }

                hwnd = replacement.Handle;
                _claimed.Add(hwnd);
                stableCount = 0;
                continue;
            }

            if (IsAtTarget(current, target))
            {
                if (++stableCount >= StableChecksNeeded)
                    return (true, current);
            }
            else
            {
                _desktop.SetPlacement(hwnd, target);
                stableCount = 0;
            }

            await Task.Delay(PlacementPollIntervalMs, cancellationToken);
        }

        return (false, current);
    }

    /// <summary>죽은(파괴된) 핸들이면 GetWindowRect가 실패해 크기가 0×0으로 나온다.</summary>
    private static bool IsDeadHandle(WindowPlacement p) => p.Width == 0 && p.Height == 0;

    /// <summary>현재 배치가 목표와 허용 오차(<see cref="PositionTolerancePx"/>) 내로 일치하는지.</summary>
    private static bool IsAtTarget(WindowPlacement current, WindowPlacement target)
        => Math.Abs(current.X - target.X) <= PositionTolerancePx
        && Math.Abs(current.Y - target.Y) <= PositionTolerancePx
        && Math.Abs(current.Width - target.Width) <= PositionTolerancePx
        && Math.Abs(current.Height - target.Height) <= PositionTolerancePx;

    private const int PlacementPollIntervalMs = 200;
    private const int StableChecksNeeded = 2;
    private const int DefaultSettleTimeoutMs = 6000;
    private const double PositionTolerancePx = 4;

    private void ReportProgress(string description, int current, int total)
    {
        ProgressChanged?.Invoke(this, new SceneProgressEventArgs
        {
            StepDescription = description,
            CurrentStep = current,
            TotalSteps = total,
            ProgressPercent = total == 0 ? 100 : (double)current / total * 100,
        });
    }
}
