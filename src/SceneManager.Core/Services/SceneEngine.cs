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
    private readonly IProcessManager _processManager;
    private readonly IWindowManager _windowManager;
    private readonly DependencyResolver _dependencyResolver;

    public SceneEngine(
        ISceneRepository repository,
        IProcessManager processManager,
        IWindowManager windowManager,
        DependencyResolver dependencyResolver)
    {
        _repository = repository;
        _processManager = processManager;
        _windowManager = windowManager;
        _dependencyResolver = dependencyResolver;
    }

    public string? CurrentSceneId { get; private set; }

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

        // 새 씬 실행 전에 이전 씬 프로그램을 정리한다.
        await ClosePreviousSceneAsync(scene, result, cancellationToken);

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

        CurrentSceneId = sceneId;
        result.Elapsed = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// 새 씬 적용 전, 직전에 적용된 씬(<see cref="CurrentSceneId"/>)의 프로그램을 정리한다.
    /// 새 씬의 <see cref="Scene.ClosePreviousScene"/>가 켜져 있고, 이전 씬 프로그램 중
    /// <see cref="ProgramEntry.CloseOnSceneExit"/>가 켜진 항목만 종료한다.
    /// </summary>
    private async Task ClosePreviousSceneAsync(Scene newScene, SceneApplyResult result, CancellationToken cancellationToken)
    {
        if (!newScene.ClosePreviousScene || CurrentSceneId is null)
            return;

        var previous = await _repository.GetByIdAsync(CurrentSceneId, cancellationToken);
        if (previous is null)
            return;

        foreach (var program in previous.Programs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!program.CloseOnSceneExit)
                continue;

            var step = new StepResult { StepName = $"Close {program.Name}", Success = true };
            try
            {
                if (!await _processManager.CloseAsync(program, cancellationToken: cancellationToken))
                {
                    step.Success = false;
                    step.ErrorMessage = "종료 실패";
                    result.Success = false;
                }
            }
            catch (Exception ex)
            {
                step.Success = false;
                step.ErrorMessage = ex.Message;
                result.Success = false;
            }

            result.Steps.Add(step);
        }
    }

    private async Task<StepResult> ApplyProgramAsync(ProgramEntry program, CancellationToken cancellationToken)
    {
        var step = new StepResult { StepName = $"Launch {program.Name}", Success = true };

        // 실행 파일명 기준 프로세스명(런처 별칭·패키지 앱은 실제 PID가 달라질 수 있어
        // "새로 나타난 같은 이름의 창"으로 식별한다).
        var processName = Path.GetFileNameWithoutExtension(program.ExecPath);
        var existingHandles = HandlesOf(processName);

        var launch = await _processManager.LaunchAsync(program, cancellationToken);
        if (!launch.Success)
        {
            step.Success = false;
            step.ErrorMessage = launch.ErrorMessage ?? "실행 실패";
            return step;
        }

        // 창 배치를 저장하지 않았으면 실행만 하고 종료.
        if (program.Window is null)
            return step;

        var hwnd = await WaitForNewWindowAsync(processName, existingHandles, cancellationToken);
        if (hwnd == IntPtr.Zero)
        {
            step.Success = false;
            step.ErrorMessage = "창을 찾지 못함(타임아웃)";
            return step;
        }

        _windowManager.SetPlacement(hwnd, program.Window);
        return step;
    }

    /// <summary>지정한 프로세스명의 현재 보이는 창 핸들 집합.</summary>
    private HashSet<IntPtr> HandlesOf(string processName)
        => _windowManager.GetAllVisibleWindows()
            .Where(w => string.Equals(w.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
            .Select(w => w.Handle)
            .ToHashSet();

    /// <summary>실행 전 목록(<paramref name="existing"/>)에 없던, 같은 프로세스명의 새 창을 대기한다.</summary>
    private async Task<IntPtr> WaitForNewWindowAsync(
        string processName, HashSet<IntPtr> existing, CancellationToken cancellationToken, int timeoutMs = 10000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var match = _windowManager.GetAllVisibleWindows().FirstOrDefault(w =>
                string.Equals(w.ProcessName, processName, StringComparison.OrdinalIgnoreCase)
                && !existing.Contains(w.Handle));

            if (match is not null)
                return match.Handle;

            await Task.Delay(WindowPollIntervalMs, cancellationToken);
        }

        return IntPtr.Zero;
    }

    private const int WindowPollIntervalMs = 150;

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
