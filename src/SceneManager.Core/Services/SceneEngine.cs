using System.Diagnostics;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Core.Services;

/// <summary>
/// 씬 적용 오케스트레이터. 로드 → 의존성 순서 결정 → 실행 → 창 대기 → 배치.
/// v0: 이전 씬 정리·오디오·모니터 매핑은 생략. 부분 실패를 허용한다.
/// </summary>
public sealed class SceneEngine : ISceneEngine
{
    private readonly ISceneRepository _repository;
    private readonly IDesktopManager _desktop;
    private readonly DependencyResolver _dependencyResolver;
    private readonly ProcessFilterEvaluator _filter;

    public SceneEngine(
        ISceneRepository repository,
        IDesktopManager desktop,
        DependencyResolver dependencyResolver,
        ProcessFilterEvaluator filter)
    {
        _repository = repository;
        _desktop = desktop;
        _dependencyResolver = dependencyResolver;
        _filter = filter;
    }

    public event EventHandler<SceneProgressEventArgs>? ProgressChanged;

    public async Task<SceneApplyResult> ApplyAsync(string sceneId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SceneApplyResult { SceneId = sceneId, Success = true };

        var scene = await _repository.GetByIdAsync(sceneId, cancellationToken);
        if (scene is null)
        {
            result.Success = false;
            result.Steps.Add(new StepResult { StepName = "Load scene", Success = false, ErrorMessage = "씬을 찾을 수 없음" });
            result.Elapsed = stopwatch.Elapsed;
            return result;
        }

        // 의존성 순서대로 그룹화(같은 그룹은 병렬 가능하지만 v0는 순차 실행).
        var levels = _dependencyResolver.Resolve(scene.Programs);
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
        // 실행 파일명 기준 프로세스명(런처 별칭·패키지 앱은 실제 PID가 달라질 수 있어
        // "같은 이름의 창"으로 식별한다).
        var processName = Path.GetFileNameWithoutExtension(program.ExecPath);
        var alreadyRunning = FindMainWindow(processName) != IntPtr.Zero;

        // 멱등: 이미 실행 중이면 새로 띄우지 않고 기존 창을 재배치한다(창이 쌓이지 않음).
        // 실행 중이 아니면 새로 실행한다.
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
        }

        // 창 배치를 저장하지 않았으면 실행만 하고 종료.
        if (program.Window is null)
            return step;

        // 창이 나타나 자리 잡을 때까지 반복 배치(무거운 앱·자기 레이아웃 복원 대응).
        var settled = await SettlePlacementAsync(processName, program.Window, program.SettleTimeoutMs, cancellationToken);
        if (!settled)
        {
            step.Success = false;
            step.ErrorMessage = "위치 안정화 실패(타임아웃)";
        }

        return step;
    }

    /// <summary>
    /// 대상 프로세스의 메인 창이 <paramref name="target"/> 위치에 안정적으로 자리 잡을 때까지
    /// 주기적으로 재배치한다. 연속 <see cref="StableChecksNeeded"/>회 목표와 일치하면 성공.
    /// 매 반복마다 메인 창을 다시 찾으므로 스플래시→본창 전환도 따라간다.
    /// </summary>
    private async Task<bool> SettlePlacementAsync(
        string processName, WindowPlacement target, int settleTimeoutMs, CancellationToken cancellationToken)
    {
        var timeoutMs = settleTimeoutMs > 0 ? settleTimeoutMs : DefaultSettleTimeoutMs;
        var stopwatch = Stopwatch.StartNew();
        var stableCount = 0;

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hwnd = FindMainWindow(processName);
            if (hwnd != IntPtr.Zero)
            {
                var current = _desktop.GetPlacement(hwnd);
                if (IsAtTarget(current, target))
                {
                    if (++stableCount >= StableChecksNeeded)
                        return true; // 연속으로 목표에 머무름 → 안정
                }
                else
                {
                    _desktop.SetPlacement(hwnd, target); // 어긋남(또는 첫 배치) → 다시 배치
                    stableCount = 0;
                }
            }

            await Task.Delay(PlacementPollIntervalMs, cancellationToken);
        }

        return false; // 타임아웃까지 안정화 실패
    }

    /// <summary>프로세스의 메인 창 = 같은 이름의 보이는 창 중 면적이 가장 큰 것(스플래시 제외).</summary>
    private IntPtr FindMainWindow(string processName)
    {
        var main = _desktop.GetAllVisibleWindows()
            .Where(w => string.Equals(w.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(w => w.Placement.Width * w.Placement.Height)
            .FirstOrDefault();

        return main?.Handle ?? IntPtr.Zero;
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
