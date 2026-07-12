using System.Diagnostics;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Core.Platform;

/// <summary>
/// <see cref="Process"/> 기반 <see cref="IProcessManager"/> 구현.
/// v0: Win32 exe 실행만 지원(UWP/AUMID·종료는 이후 단계).
/// </summary>
public sealed class WindowsProcessManager : IProcessManager
{
    public Task<ProcessLaunchResult> LaunchAsync(ProgramEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
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

    public Task<List<RunningProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException("이후 단계에서 구현");

    public Task<bool> CloseAsync(ProgramEntry entry, int gracefulTimeoutMs = 5000, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("이후 단계에서 구현(이전 씬 정리)");
}
