using System.Diagnostics;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Core.Platform;

/// <summary>
/// <see cref="Process"/> 기반 <see cref="IProcessManager"/> 구현.
/// Win32 exe 실행과 스토어(UWP/MSIX) 앱 실행(AUMID)을 지원한다.
/// </summary>
public sealed class WindowsProcessManager : IProcessManager
{
    public Task<ProcessLaunchResult> LaunchAsync(ProgramEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            // 스토어(UWP/MSIX) 앱: WindowsApps의 exe는 ACL로 직접 실행이 막히므로
            // 셸의 AppsFolder를 통해 AUMID로 활성화한다.
            if (entry.Type == ProgramType.Uwp && !string.IsNullOrEmpty(entry.AppUserModelId))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"shell:AppsFolder\\{entry.AppUserModelId}",
                    UseShellExecute = true,
                });

                // explorer가 앱을 대신 띄우므로 앱 PID는 알 수 없다(창은 프로세스명으로 탐색).
                return Task.FromResult(new ProcessLaunchResult { Success = true });
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = entry.ExecPath,
                Arguments = entry.Arguments ?? string.Empty,
                WorkingDirectory = Path.GetDirectoryName(entry.ExecPath) ?? string.Empty,
                UseShellExecute = true, // 실행 파일 연결/작업 폴더 처리를 셸에 위임
            };

            if (entry.RequiresAdmin)
                startInfo.Verb = "runas"; // UAC 승격 요청

            var proc = Process.Start(startInfo);

            return Task.FromResult(new ProcessLaunchResult
            {
                Success = proc is not null,
                ProcessId = proc?.Id,
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ProcessLaunchResult
            {
                Success = false,
                ErrorMessage = ex.Message,
            });
        }
    }

    public bool IsRunning(string processName)
        => Process.GetProcessesByName(processName).Length > 0;
}
