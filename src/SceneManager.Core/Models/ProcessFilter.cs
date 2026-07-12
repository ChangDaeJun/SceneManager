namespace SceneManager.Core.Models;

/// <summary>
/// 스냅샷 캡처 시 프로세스를 걸러내는 필터.
/// 씬에 종속되지 않는 전역 설정으로, 별도 파일(process-filter.json)에 저장된다.
/// </summary>
public sealed class ProcessFilter
{
    /// <summary>내장 시스템 블랙리스트. 예: svchost, explorer, dwm. 앱 배포 시 기본 제공된다.</summary>
    public List<string> SystemBlacklist { get; set; } = [];

    /// <summary>사용자가 추가한 블랙리스트(캡처에서 제외할 프로세스).</summary>
    public List<string> UserBlacklist { get; set; } = [];

    /// <summary>사용자가 명시적으로 포함시키는 화이트리스트(블랙리스트보다 우선).</summary>
    public List<string> UserWhitelist { get; set; } = [];
}
