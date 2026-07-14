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
        var target = SelectWindow(processName, program.WindowTitle);
        var alreadyRunning = target is not null;

        var step = new StepResult
        {
            StepName = $"{(alreadyRunning ? "Reposition" : "Launch")} {program.Name}",
            Success = true,
        };

        if (!alreadyRunning)
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

            target = await WaitForWindowAsync(processName, program.WindowTitle, program.SettleTimeoutMs, cancellationToken);
            if (target is null)
            {
                step.Success = false;
                step.ErrorMessage = "창을 찾지 못함(타임아웃)";
                return step;
            }
        }

        // 이 창을 이 항목에 배정(같은 프로세스의 다른 항목이 다시 잡지 못하도록).
        _claimed.Add(target!.Handle);

        if (program.Window is null)
            return step; // 이미 실행 중 + 배치 없음

        var settled = await SettleHandleAsync(target.Handle, program.Window, program.SettleTimeoutMs, cancellationToken);
        if (!settled)
        {
            step.Success = false;
            step.ErrorMessage = "위치 안정화 실패(타임아웃)";
        }

        return step;
    }

    /// <summary>
    /// 이 항목에 배정할 창을 고른다: 같은 프로세스명 + 아직 배정 안 된 창 중에서
    /// 제목 정확 일치 &gt; 부분 일치 &gt; 가장 큰 창 순. 없으면 null.
    /// </summary>
    private WindowInfo? SelectWindow(string processName, string? title)
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

        // 폴백: 가장 큰 창(스플래시 제외 효과)
        return candidates.OrderByDescending(w => w.Placement.Width * w.Placement.Height).First();
    }

    /// <summary>실행 직후, 배정 가능한 창이 나타날 때까지 대기한다. 타임아웃 시 null.</summary>
    private async Task<WindowInfo?> WaitForWindowAsync(
        string processName, string? title, int settleTimeoutMs, CancellationToken cancellationToken)
    {
        var timeoutMs = settleTimeoutMs > 0 ? settleTimeoutMs : DefaultSettleTimeoutMs;
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var window = SelectWindow(processName, title);
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
    private async Task<bool> SettleHandleAsync(
        IntPtr hwnd, WindowPlacement target, int settleTimeoutMs, CancellationToken cancellationToken)
    {
        var timeoutMs = settleTimeoutMs > 0 ? settleTimeoutMs : DefaultSettleTimeoutMs;
        var stopwatch = Stopwatch.StartNew();
        var stableCount = 0;

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = _desktop.GetPlacement(hwnd);
            if (IsAtTarget(current, target))
            {
                if (++stableCount >= StableChecksNeeded)
                    return true;
            }
            else
            {
                _desktop.SetPlacement(hwnd, target);
                stableCount = 0;
            }

            await Task.Delay(PlacementPollIntervalMs, cancellationToken);
        }

        return false;
    }

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
