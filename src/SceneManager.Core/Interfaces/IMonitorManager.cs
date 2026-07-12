using SceneManager.Core.Models;

namespace SceneManager.Core.Interfaces;

/// <summary>
/// 모니터 구성을 조회하고, 저장된 레이아웃과 현재 레이아웃을 매핑하며,
/// 논리↔물리 좌표를 변환한다.
/// </summary>
public interface IMonitorManager
{
    /// <summary>현재 연결된 모니터 구성을 조회한다.</summary>
    MonitorLayout GetCurrentLayout();

    /// <summary>저장된 레이아웃을 현재 레이아웃에 매핑한다(씬 적용 시 창 재배치용).</summary>
    MonitorMappingResult MapMonitors(MonitorLayout saved, MonitorLayout current);

    /// <summary>논리 좌표(사각형)를 해당 모니터 기준 물리 좌표로 변환한다.</summary>
    PhysicalRect LogicalToPhysical(double x, double y, double width, double height, MonitorInfo monitor);

    /// <summary>물리 좌표(사각형)를 해당 모니터 기준 논리 좌표로 변환한다.</summary>
    LogicalRect PhysicalToLogical(int x, int y, int width, int height, MonitorInfo monitor);
}

/// <summary>
/// 저장된 모니터 레이아웃을 현재 레이아웃에 매핑한 결과.
/// </summary>
public sealed class MonitorMappingResult
{
    /// <summary>저장·현재 구성이 정확히 일치하는지 여부.</summary>
    public bool IsExactMatch { get; set; }

    /// <summary>저장된 모니터 ID → 현재 모니터 ID 매핑.</summary>
    public Dictionary<string, string> MonitorIdMap { get; set; } = new();

    /// <summary>현재 구성에서 대응 모니터를 찾지 못한 저장 모니터 ID 목록.</summary>
    public List<string> UnmappedSavedMonitors { get; set; } = [];

    /// <summary>매핑 과정에서 발생한 경고 메시지.</summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// 물리 픽셀 기준 사각형. (정수 좌표)
/// </summary>
/// <param name="X">좌상단 X(물리 픽셀).</param>
/// <param name="Y">좌상단 Y(물리 픽셀).</param>
/// <param name="Width">너비(물리 픽셀).</param>
/// <param name="Height">높이(물리 픽셀).</param>
public readonly record struct PhysicalRect(int X, int Y, int Width, int Height);

/// <summary>
/// 논리 픽셀 기준 사각형. (DPI 스케일 적용 후, 실수 좌표)
/// </summary>
/// <param name="X">좌상단 X(논리 픽셀).</param>
/// <param name="Y">좌상단 Y(논리 픽셀).</param>
/// <param name="Width">너비(논리 픽셀).</param>
/// <param name="Height">높이(논리 픽셀).</param>
public readonly record struct LogicalRect(double X, double Y, double Width, double Height);
