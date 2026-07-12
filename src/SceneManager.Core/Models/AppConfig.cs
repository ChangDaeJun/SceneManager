namespace SceneManager.Core.Models;

/// <summary>
/// 로그 상세 수준. 아키텍처 10.1의 4단계에 대응한다.
/// </summary>
public enum LogLevel
{
    /// <summary>개발 디버깅용 상세 정보.</summary>
    Debug,

    /// <summary>씬 적용/전환, 프로그램 실행 성공 등 일반 정보.</summary>
    Info,

    /// <summary>경로 무효, 모니터 불일치 등 경고.</summary>
    Warning,

    /// <summary>프로그램 실행 실패, 배치 실패 등 오류.</summary>
    Error
}

/// <summary>
/// 바탕화면 위젯의 화면상 위치(논리 픽셀).
/// </summary>
public sealed class WidgetPosition
{
    /// <summary>논리 픽셀 X 좌표.</summary>
    public double X { get; set; }

    /// <summary>논리 픽셀 Y 좌표.</summary>
    public double Y { get; set; }
}

/// <summary>
/// 애플리케이션 전역 설정. config.json에 저장된다.
/// </summary>
public sealed class AppConfig
{
    /// <summary>Windows 시작 시 자동 실행 여부.</summary>
    public bool RunOnStartup { get; set; }

    /// <summary>바탕화면 위젯 표시 여부.</summary>
    public bool ShowWidget { get; set; } = true;

    /// <summary>위젯의 기억된 위치. null이면 기본 위치에 표시한다.</summary>
    public WidgetPosition? WidgetPosition { get; set; }

    /// <summary>위젯 불투명도(0.0~1.0).</summary>
    public double WidgetOpacity { get; set; } = 1.0;

    /// <summary>로그 최소 수준.</summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Info;

    /// <summary>씬 파일 저장 폴더. null이면 앱이 기본 경로를 사용한다.</summary>
    public string? ScenesDirectory { get; set; }
}
