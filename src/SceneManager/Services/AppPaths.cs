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

    public static string ProcessFilterFile => Path.Combine(BaseDir, "process-filter.json");
}
