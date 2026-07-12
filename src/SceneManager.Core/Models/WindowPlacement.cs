namespace SceneManager.Core.Models;

/// <summary>
/// 윈도우의 표시 상태.
/// </summary>
public enum WindowState
{
    /// <summary>일반(창 모드).</summary>
    Normal,

    /// <summary>최대화.</summary>
    Maximized,

    /// <summary>최소화.</summary>
    Minimized
}

/// <summary>
/// 프로그램 창의 배치 정보.
/// 좌표·크기는 모두 <b>논리 픽셀</b>(DPI 스케일 적용 후) 기준이며,
/// 가상 스크린 전체를 기준으로 한 절대 좌표다.
/// </summary>
public sealed class WindowPlacement
{
    /// <summary>대상 모니터 식별자. <see cref="MonitorInfo.Id"/>와 매칭된다.</summary>
    public required string MonitorId { get; set; }

    /// <summary>논리 픽셀 X 좌표.</summary>
    public double X { get; set; }

    /// <summary>논리 픽셀 Y 좌표.</summary>
    public double Y { get; set; }

    /// <summary>논리 픽셀 너비.</summary>
    public double Width { get; set; }

    /// <summary>논리 픽셀 높이.</summary>
    public double Height { get; set; }

    /// <summary>창 표시 상태.</summary>
    public WindowState State { get; set; } = WindowState.Normal;
}
