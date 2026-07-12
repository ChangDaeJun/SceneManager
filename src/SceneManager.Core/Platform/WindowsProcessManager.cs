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

    /// <summary>
    /// 프로그램을 종료한다. v0: 실행 파일명으로 프로세스를 찾아
    /// WM_CLOSE(정상 종료) 우선, <paramref name="gracefulTimeoutMs"/> 초과 시 강제 종료.
    /// 매칭되는 프로세스가 없으면(이미 없음) 성공으로 본다.
    /// </summary>
    public async Task<bool> CloseAsync(ProgramEntry entry, int gracefulTimeoutMs = 5000, CancellationToken cancellationToken = default)
    {
        var processName = Path.GetFileNameWithoutExtension(entry.ExecPath);
        var processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0)
            return true;

        var allClosed = true;
        foreach (var proc in processes)
        {
            try
            {
                // 메인 창이 있으면 WM_CLOSE로 정상 종료 요청.
                if (proc.MainWindowHandle != IntPtr.Zero)
                    proc.CloseMainWindow();

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(gracefulTimeoutMs);
                try
                {
                    await proc.WaitForExitAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Graceful 타임아웃 → 강제 종료.
                    proc.Kill();
                    await proc.WaitForExitAsync(cancellationToken);
                }
            }
            catch (Exception)
            {
                allClosed = false;
            }
            finally
            {
                proc.Dispose();
            }
        }

        return allClosed;
    }
}
