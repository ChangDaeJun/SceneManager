namespace SceneManager.Core.Models;

/// <summary>
/// 스냅샷 캡처·화면 정리 시 프로세스를 걸러내는 필터.
/// 씬에 종속되지 않는 전역 설정으로, 별도 파일(process-filter.json)에 저장된다.
/// </summary>
public sealed class ProcessFilter
{
    /// <summary>내장 시스템 블랙리스트. 예: TextInputHost, ApplicationFrameHost. 앱 배포 시 기본 제공된다.</summary>
    public List<string> SystemBlacklist { get; set; } = [];

    /// <summary>사용자가 추가한 블랙리스트(캡처에서 제외할 프로세스).</summary>
    public List<string> UserBlacklist { get; set; } = [];

    /// <summary>사용자가 명시적으로 포함시키는 화이트리스트(블랙리스트보다 우선).</summary>
    public List<string> UserWhitelist { get; set; } = [];

    /// <summary>
    /// 이 프로세스명을 대상에 포함해야 하면 true.
    /// 우선순위: 화이트리스트 &gt; (시스템·사용자) 블랙리스트 &gt; 기본 포함. 대소문자 무시.
    /// </summary>
    public bool ShouldInclude(string processName)
    {
        if (Contains(UserWhitelist, processName))
            return true; // 화이트리스트가 최우선

        if (Contains(SystemBlacklist, processName))
            return false;

        if (Contains(UserBlacklist, processName))
            return false;

        return true; // 어디에도 안 걸리면 기본 포함
    }

    private static bool Contains(List<string> list, string value)
        => list.Contains(value, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 앱 배포 시 기본 제공되는 시스템 블랙리스트.
    /// 항상 떠 있는 셸/시스템 창들의 프로세스명(확장자 제외).
    /// explorer는 실제 파일 탐색기 창이 유효하므로 제외하지 않는다(바탕화면은 창 클래스로 걸러짐).
    /// </summary>
    public static ProcessFilter CreateDefault() => new()
    {
        SystemBlacklist =
        [
            "SceneManager",            // 편집기 자신(스냅샷에서 제외; PID 자기 제외의 보조)
            "TextInputHost",           // Windows 입력 환경(IME/터치 키보드)
            "ApplicationFrameHost",    // UWP 앱 프레임 호스트
            "SystemSettings",          // 설정 앱 콘텐츠 프로세스
            "ShellExperienceHost",     // 알림 센터 / 작업 표시줄 셸
            "StartMenuExperienceHost", // 시작 메뉴
            "SearchHost",              // 검색 UI
            "SearchApp",               // 검색 UI(구버전)
            "LockApp",                 // 잠금 화면
            "PhoneExperienceHost",     // 휴대폰과 연결
        ],
    };
}
