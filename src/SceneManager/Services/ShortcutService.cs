using System.IO;
using System.Runtime.InteropServices;
using SceneManager.Core.Models;

namespace SceneManager.Services;

/// <summary>
/// 씬 실행 바로가기(.lnk)를 만든다. WScript.Shell / Shell.Application COM을 지연 바인딩(dynamic)으로
/// 사용해 별도 COM 참조 없이 생성한다.
/// </summary>
public static class ShortcutService
{
    /// <summary>
    /// 바탕화면에 "SceneRunner.exe {씬이름}"을 실행하는 바로가기를 만든다.
    /// </summary>
    /// <returns>생성된 .lnk 전체 경로.</returns>
    public static string CreateOnDesktop(Scene scene, string runnerExePath, bool clean = false)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var lnkPath = Path.Combine(desktop, $"SceneManager - {Sanitize(scene.Name)}.lnk");

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell을 사용할 수 없습니다.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic lnk = shell.CreateShortcut(lnkPath);
            lnk.TargetPath = runnerExePath;
            lnk.Arguments = clean ? $"\"{scene.Name}\" --clean" : $"\"{scene.Name}\"";
            lnk.WorkingDirectory = Path.GetDirectoryName(runnerExePath) ?? string.Empty;
            lnk.Description = $"SceneManager: '{scene.Name}' 씬 실행";

            if (!string.IsNullOrWhiteSpace(scene.IconPath) && File.Exists(scene.IconPath))
                lnk.IconLocation = $"{scene.IconPath},0";
            if (!string.IsNullOrWhiteSpace(scene.Hotkey))
                lnk.Hotkey = scene.Hotkey; // 예: "CTRL+ALT+D"

            lnk.Save();
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }

        return lnkPath;
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
