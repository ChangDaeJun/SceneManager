using SceneManager.Core.Models;

namespace SceneManager.Core.Interfaces;

/// <summary>
/// 윈도우의 위치·크기·상태를 읽고 적용한다.
/// 저장·조회는 논리 픽셀 기준이며, 논리↔물리 변환은 구현 내부에서 처리한다.
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// 특정 프로세스의 메인 윈도우 핸들이 생성될 때까지 폴링하며 대기한다.
    /// 타임아웃 시 <see cref="IntPtr.Zero"/>를 반환한다.
    /// </summary>
    Task<IntPtr> WaitForWindowAsync(int processId, int timeoutMs = 10000, CancellationToken cancellationToken = default);

    /// <summary>윈도우의 현재 배치 정보를 읽는다(논리 픽셀).</summary>
    WindowPlacement GetPlacement(IntPtr hwnd);

    /// <summary>윈도우에 배치를 적용한다(논리 픽셀 → 물리 픽셀 변환은 내부 처리).</summary>
    void SetPlacement(IntPtr hwnd, WindowPlacement placement);

    /// <summary>현재 보이는 모든 윈도우의 정보를 가져온다.</summary>
    List<WindowInfo> GetAllVisibleWindows();
}

/// <summary>
/// 보이는 윈도우 하나의 정보.
/// </summary>
public sealed class WindowInfo
{
    /// <summary>윈도우 핸들(HWND).</summary>
    public IntPtr Handle { get; set; }

    /// <summary>소유 프로세스 ID.</summary>
    public int ProcessId { get; set; }

    /// <summary>소유 프로세스명.</summary>
    public required string ProcessName { get; set; }

    /// <summary>윈도우 제목. 제목이 없으면 빈 문자열.</summary>
    public required string WindowTitle { get; set; }

    /// <summary>현재 배치 정보(논리 픽셀).</summary>
    public required WindowPlacement Placement { get; set; }
}
