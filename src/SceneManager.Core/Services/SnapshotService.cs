using System.Diagnostics;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;
using SceneManager.Core.Platform;

namespace SceneManager.Core.Services;

/// <summary>
/// 현재 보이는 창들을 필터링해 씬으로 캡처한다.
/// v0: 프로그램 + 창 배치만 캡처(오디오·모니터 구성은 이후 단계).
/// </summary>
public sealed class SnapshotService : ISnapshotService
{
    private readonly IWindowManager _windowManager;
    private readonly ProcessFilterEvaluator _filter;

    public SnapshotService(IWindowManager windowManager, ProcessFilterEvaluator filter)
    {
        _windowManager = windowManager;
        _filter = filter;
    }

    public Task<Scene> CaptureFullAsync(string sceneName, CancellationToken cancellationToken = default)
        => CapturePartialAsync(sceneName, new SnapshotOptions(), cancellationToken);

    public Task<Scene> CapturePartialAsync(string sceneName, SnapshotOptions options, CancellationToken cancellationToken = default)
    {
        var windows = _windowManager.GetAllVisibleWindows();
        var programs = new List<ProgramEntry>();
        var order = 0;

        foreach (var w in windows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_filter.ShouldInclude(w.ProcessName))
                continue;

            // 실행 경로를 못 구하면 되살릴 수 없으므로 제외.
            var execPath = TryGetExecPath(w.ProcessId);
            if (execPath is null)
                continue;

            // 스토어(UWP/MSIX) 앱이면 AUMID로 실행해야 하므로 Uwp 타입으로 기록한다.
            var aumid = PackagedApps.TryGetAumid(w.ProcessId);

            programs.Add(new ProgramEntry
            {
                Id = Guid.NewGuid().ToString(),
                Name = w.ProcessName,
                ExecPath = execPath,
                Type = aumid is null ? ProgramType.Win32 : ProgramType.Uwp,
                AppUserModelId = aumid,
                Order = order++,
                Window = options.CaptureWindowPlacement ? w.Placement : null,
            });
        }

        var now = DateTimeOffset.Now;
        var scene = new Scene
        {
            Id = Guid.NewGuid().ToString(),
            Name = sceneName,
            Programs = programs,
            Metadata = new SceneMetadata { CreatedAt = now, ModifiedAt = now },
        };

        return Task.FromResult(scene);
    }

    /// <summary>PID로 실행 파일 경로를 얻는다. 접근 불가(권한/스토어 앱 등)면 null.</summary>
    private static string? TryGetExecPath(int processId)
    {
        try
        {
            using var proc = Process.GetProcessById(processId);
            return proc.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
}
