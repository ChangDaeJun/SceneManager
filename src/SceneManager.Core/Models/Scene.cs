namespace SceneManager.Core.Models;

/// <summary>
/// 씬의 메타데이터. 스키마 버전과 생성/수정 시각을 담는다.
/// </summary>
public sealed class SceneMetadata
{
    /// <summary>현재 스키마 버전. 향후 마이그레이션 시 기준값.</summary>
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>이 씬이 따르는 스키마 버전.</summary>
    public string SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>생성 시각.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>마지막 수정 시각.</summary>
    public DateTimeOffset ModifiedAt { get; set; }
}

/// <summary>
/// 씬 하나. 프로그램 실행 + 화면 배치 + 사운드 설정의 조합을 정의하는 최상위 모델.
/// </summary>
public sealed class Scene
{
    /// <summary>씬 고유 식별자(GUID).</summary>
    public required string Id { get; set; }

    /// <summary>씬 이름. 예: <c>업무</c>, <c>게임</c></summary>
    public required string Name { get; set; }

    /// <summary>아이콘 파일 경로. 없으면 null.</summary>
    public string? IconPath { get; set; }

    /// <summary>할당된 글로벌 단축키. 예: <c>Ctrl+Alt+1</c>. 없으면 null.</summary>
    public string? Hotkey { get; set; }

    /// <summary>씬 전환 시 이전 씬의 프로그램을 종료할지 여부.</summary>
    public bool ClosePreviousScene { get; set; } = true;

    /// <summary>실행 대상 프로그램 목록.</summary>
    public List<ProgramEntry> Programs { get; set; } = [];

    /// <summary>오디오 설정. null이면 이 씬은 오디오를 건드리지 않는다.</summary>
    public AudioConfig? Audio { get; set; }

    /// <summary>저장 시점의 모니터 구성. 창 배치 복원 시 현재 구성과 비교하는 기준. 없으면 null.</summary>
    public MonitorLayout? MonitorSnapshot { get; set; }

    /// <summary>메타데이터(스키마 버전, 생성/수정 시각).</summary>
    public SceneMetadata Metadata { get; set; } = new();
}
