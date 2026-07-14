namespace SceneManager.Core.Models;

/// <summary>
/// 프로그램의 종류. 실행 방식과 윈도우 식별 방식이 달라진다.
/// </summary>
public enum ProgramType
{
    /// <summary>일반 Win32 데스크톱 앱. exe 경로로 실행.</summary>
    Win32,

    /// <summary>UWP(Store) 앱. AUMID로 실행. (Phase 3)</summary>
    Uwp
}

/// <summary>
/// 씬에 등록된 실행 대상 프로그램 하나. 실행 방법·순서·의존성·창 배치를 담는다.
/// </summary>
public sealed class ProgramEntry
{
    /// <summary>항목 고유 식별자(GUID). 의존성 참조(<see cref="DependsOnId"/>)의 대상이 된다.</summary>
    public required string Id { get; set; }

    /// <summary>표시명. 예: <c>Chrome</c></summary>
    public required string Name { get; set; }

    /// <summary>실행 경로(exe). Win32 앱의 실행 대상. (UWP는 Phase 3에서 재검토)</summary>
    public required string ExecPath { get; set; }

    /// <summary>실행 인자. 없으면 null.</summary>
    public string? Arguments { get; set; }

    /// <summary>프로그램 종류.</summary>
    public ProgramType Type { get; set; } = ProgramType.Win32;

    /// <summary>UWP 전용 AUMID(Application User Model ID). Win32에서는 null.</summary>
    public string? AppUserModelId { get; set; }

    /// <summary>실행 순서(작을수록 먼저). 의존성이 없을 때의 정렬 기준.</summary>
    public int Order { get; set; }

    /// <summary>실행 후 대기 시간(ms). 다음 프로그램 실행 전 지연.</summary>
    public int DelayAfterMs { get; set; }

    /// <summary>
    /// 창 배치가 자리 잡을 때까지 재시도할 최대 시간(ms). 0이면 엔진 기본값 사용.
    /// VS·포토샵처럼 로딩이 길고 자기 레이아웃을 복원하는 앱은 크게 설정한다.
    /// </summary>
    public int SettleTimeoutMs { get; set; }

    /// <summary>선행 실행되어야 하는 프로그램의 <see cref="Id"/>. 없으면 null.</summary>
    public string? DependsOnId { get; set; }

    /// <summary>관리자 권한으로 실행해야 하는지 여부. true면 UAC 승격 요청.</summary>
    public bool RequiresAdmin { get; set; }

    /// <summary>창 배치 정보. 배치를 저장하지 않은 경우 null.</summary>
    public WindowPlacement? Window { get; set; }
}
