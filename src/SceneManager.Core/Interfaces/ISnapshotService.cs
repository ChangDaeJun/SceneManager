using SceneManager.Core.Models;

namespace SceneManager.Core.Interfaces;

/// <summary>
/// 현재 데스크톱 상태(실행 중 프로그램·창 배치·오디오)를 캡처해 씬으로 만든다.
/// </summary>
public interface ISnapshotService
{
    /// <summary>전체 스냅샷을 캡처한다(프로그램 + 창 배치 + 오디오).</summary>
    Task<Scene> CaptureFullAsync(string sceneName, CancellationToken cancellationToken = default);

    /// <summary>지정한 항목만 부분 캡처한다.</summary>
    Task<Scene> CapturePartialAsync(string sceneName, SnapshotOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// 부분 스냅샷에서 캡처할 대상을 지정한다. 기본값은 모두 캡처(전체 스냅샷과 동일).
/// </summary>
public sealed class SnapshotOptions
{
    /// <summary>실행 중인 프로그램을 캡처할지 여부.</summary>
    public bool CapturePrograms { get; set; } = true;

    /// <summary>창 배치를 캡처할지 여부(프로그램 캡처가 전제).</summary>
    public bool CaptureWindowPlacement { get; set; } = true;

    /// <summary>오디오 설정을 캡처할지 여부.</summary>
    public bool CaptureAudio { get; set; } = true;
}
