namespace SceneManager.Core.Models;

/// <summary>
/// 모니터 한 대의 정보. 씬 저장 시점의 디스플레이 구성을 기록해,
/// 적용 시점의 구성과 비교해 창 배치를 매핑하는 데 쓴다.
/// </summary>
public sealed class MonitorInfo
{
    /// <summary>모니터 고유 식별자. <see cref="WindowPlacement.MonitorId"/>와 매칭된다.</summary>
    public required string Id { get; set; }

    /// <summary>OS 디바이스명. 예: <c>\\.\DISPLAY1</c></summary>
    public required string DeviceName { get; set; }

    /// <summary>물리 해상도 너비(픽셀).</summary>
    public int PhysicalWidth { get; set; }

    /// <summary>물리 해상도 높이(픽셀).</summary>
    public int PhysicalHeight { get; set; }

    /// <summary>논리 해상도 너비(DPI 스케일 적용 후).</summary>
    public double LogicalWidth { get; set; }

    /// <summary>논리 해상도 높이(DPI 스케일 적용 후).</summary>
    public double LogicalHeight { get; set; }

    /// <summary>DPI 스케일(%). 예: 100, 125, 150.</summary>
    public int DpiScale { get; set; } = 100;

    /// <summary>논리 좌표계 내 이 모니터의 X 위치.</summary>
    public double PositionX { get; set; }

    /// <summary>논리 좌표계 내 이 모니터의 Y 위치.</summary>
    public double PositionY { get; set; }

    /// <summary>주 모니터 여부.</summary>
    public bool IsPrimary { get; set; }
}

/// <summary>
/// 씬 저장 시점의 전체 모니터 구성 스냅샷.
/// </summary>
public sealed class MonitorLayout
{
    /// <summary>연결된 모니터 목록.</summary>
    public List<MonitorInfo> Monitors { get; set; } = [];
}
