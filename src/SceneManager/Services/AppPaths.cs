using System.IO;

namespace SceneManager.Services;

/// <summary>
/// 편집기·실행기가 공유하는 파일 경로. 러너와 동일한 규칙(%LOCALAPPDATA%\SceneManager).
/// </summary>
public static class AppPaths
{
    public static string BaseDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SceneManager");

    public static string ScenesDir => Path.Combine(BaseDir, "scenes");

    /// <summary>통합 설정 파일(필터 + 인자 도우미).</summary>
    public static string AppConfigFile => Path.Combine(BaseDir, "app-config.json");

    /// <summary>구 버전 필터 파일. 통합 설정 최초 생성 시 사용자 목록 이관 원본으로만 참조.</summary>
    public static string LegacyProcessFilterFile => Path.Combine(BaseDir, "process-filter.json");
}
