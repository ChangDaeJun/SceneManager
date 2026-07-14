using System.IO;

namespace SceneManager.Services;

/// <summary>
/// 바로가기가 가리킬 SceneRunner.exe 경로를 찾는다.
/// 배포 시엔 편집기와 같은 폴더에 있고, 개발 중엔 SceneRunner 프로젝트의 빌드 산출물을 쓴다.
/// </summary>
public static class RunnerLocator
{
    private const string RunnerExe = "SceneRunner.exe";

    /// <summary>SceneRunner.exe의 전체 경로. 못 찾으면 null.</summary>
    public static string? Find()
    {
        // ① 배포: 편집기와 같은 폴더
        var sameDir = Path.Combine(AppContext.BaseDirectory, RunnerExe);
        if (File.Exists(sameDir))
            return sameDir;

        // ② 개발: ...\src\SceneManager\bin\{Config}\net8.0-windows\ 기준으로
        //    형제 프로젝트 ...\src\SceneRunner\bin\{Config}\net8.0-windows\SceneRunner.exe
        var config = IsDebugPath(AppContext.BaseDirectory) ? "Debug" : "Release";
        var devPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "SceneRunner", "bin", config, "net8.0-windows", RunnerExe));
        if (File.Exists(devPath))
            return devPath;

        return null;
    }

    private static bool IsDebugPath(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase);
}
