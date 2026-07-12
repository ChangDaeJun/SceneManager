using SceneManager.Core.Models;

namespace SceneManager.Core.Services;

/// <summary>
/// <see cref="ProcessFilter"/> 규칙을 평가해 특정 프로세스를 스냅샷에 포함할지 판정한다.
/// 우선순위: 화이트리스트 > (시스템 · 사용자) 블랙리스트 > 기본 포함.
/// 프로세스명 비교는 대소문자를 구분하지 않는다.
/// </summary>
public sealed class ProcessFilterEvaluator
{
    private readonly ProcessFilter _filter;

    public ProcessFilterEvaluator(ProcessFilter filter) => _filter = filter;

    /// <summary>이 프로세스명을 스냅샷에 포함해야 하면 true.</summary>
    public bool ShouldInclude(string processName)
    {
        if (Contains(_filter.UserWhitelist, processName))
            return true; // 화이트리스트가 최우선

        if (Contains(_filter.SystemBlacklist, processName))
            return false;

        if (Contains(_filter.UserBlacklist, processName))
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
    public static ProcessFilter CreateSystemDefault() => new()
    {
        SystemBlacklist =
        [
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
