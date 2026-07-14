using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Core.Services;

/// <summary>
/// 현재 보이는 창들을 필터링해 씬으로 캡처한다.
/// v0: 프로그램 + 창 배치만 캡처(오디오·모니터 구성은 이후 단계).
/// </summary>
public sealed class SceneSnapshot : ISceneSnapshot
{
    private readonly IDesktopManager _desktop;
    private readonly ProcessFilter _filter;

    public SceneSnapshot(IDesktopManager desktop, ProcessFilter filter)
    {
        _desktop = desktop;
        _filter = filter;
    }

    public Task<Scene> CaptureFullAsync(string sceneName, CancellationToken cancellationToken = default)
        => CapturePartialAsync(sceneName, new SnapshotOptions(), cancellationToken);

    public Task<Scene> CapturePartialAsync(string sceneName, SnapshotOptions options, CancellationToken cancellationToken = default)
    {
        var windows = _desktop.GetAllVisibleWindows();
        var self = Environment.ProcessId; // 스냅샷을 요청한 프로세스(편집기 자신)는 제외
        var programs = new List<ProgramEntry>();
        var order = 0;

        foreach (var w in windows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (w.ProcessId == self)
                continue;

            // 최소화된 창은 화면 배치 대상이 아니므로 제외(좌표가 -32000 sentinel).
            if (w.Placement.State == WindowState.Minimized)
                continue;

            if (!_filter.ShouldInclude(w.ProcessName))
                continue;

            // 실행 경로를 못 구하면 되살릴 수 없으므로 제외.
            if (w.ExecPath is null)
                continue;

            programs.Add(new ProgramEntry
            {
                Id = Guid.NewGuid().ToString(),
                Name = w.ProcessName,
                ExecPath = w.ExecPath,
                // 스토어(UWP/MSIX) 앱이면 AUMID로 실행해야 하므로 Uwp 타입으로 기록한다.
                Type = w.AppUserModelId is null ? ProgramType.Win32 : ProgramType.Uwp,
                AppUserModelId = w.AppUserModelId,
                WindowTitle = w.WindowTitle,
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
}
