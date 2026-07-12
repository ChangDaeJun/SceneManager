using SceneManager.Core.Models;

namespace SceneManager.Core.Interfaces;

/// <summary>
/// 프로세스의 실행·종료·조회를 담당한다. Win32/UWP 실행 방식의 차이를 내부에서 흡수한다.
/// </summary>
public interface IProcessManager
{
    /// <summary>현재 실행 중인 UI 프로세스 목록(스냅샷 캡처용).</summary>
    Task<List<RunningProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default);

    /// <summary>프로그램을 실행한다(Win32 + UWP 통합).</summary>
    Task<ProcessLaunchResult> LaunchAsync(ProgramEntry entry, CancellationToken cancellationToken = default);

    /// <summary>프로그램을 종료한다. Graceful(WM_CLOSE) 우선, 타임아웃 후 강제 종료.</summary>
    Task<bool> CloseAsync(ProgramEntry entry, int gracefulTimeoutMs = 5000, CancellationToken cancellationToken = default);

    /// <summary>특정 프로세스가 실행 중인지 확인한다.</summary>
    bool IsRunning(string processName);
}

/// <summary>
/// 실행 중인 프로세스의 정보. 스냅샷 캡처 시 현재 프로세스를 나타낸다.
/// </summary>
public sealed class RunningProcessInfo
{
    /// <summary>프로세스명. 예: <c>chrome.exe</c></summary>
    public required string ProcessName { get; set; }

    /// <summary>실행 파일 경로. 접근 불가 시 null.</summary>
    public string? ExecPath { get; set; }

    /// <summary>프로세스 ID.</summary>
    public int ProcessId { get; set; }

    /// <summary>프로그램 종류.</summary>
    public ProgramType Type { get; set; } = ProgramType.Win32;

    /// <summary>UWP 전용 AUMID. Win32에서는 null.</summary>
    public string? AppUserModelId { get; set; }

    /// <summary>메인 윈도우 핸들(HWND). 창이 없으면 <see cref="IntPtr.Zero"/>.</summary>
    public IntPtr MainWindowHandle { get; set; }

    /// <summary>윈도우 제목. 없으면 null.</summary>
    public string? WindowTitle { get; set; }
}

/// <summary>
/// 프로그램 실행 결과.
/// </summary>
public sealed class ProcessLaunchResult
{
    /// <summary>실행 성공 여부.</summary>
    public bool Success { get; set; }

    /// <summary>실행된 프로세스 ID. 실패 시 null.</summary>
    public int? ProcessId { get; set; }

    /// <summary>감지된 메인 윈도우 핸들. 아직 없으면 null.</summary>
    public IntPtr? WindowHandle { get; set; }

    /// <summary>실패 시 오류 메시지. 성공 시 null.</summary>
    public string? ErrorMessage { get; set; }
}
