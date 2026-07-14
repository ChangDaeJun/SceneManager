using SceneManager.Core.Models;

namespace SceneManager.Core.Interfaces;

/// <summary>
/// 프로세스의 실행·조회를 담당한다. Win32/UWP 실행 방식의 차이를 내부에서 흡수한다.
/// </summary>
public interface IProcessManager
{
    /// <summary>프로그램을 실행한다(Win32 + UWP 통합).</summary>
    Task<ProcessLaunchResult> LaunchAsync(ProgramEntry entry, CancellationToken cancellationToken = default);

    /// <summary>특정 프로세스가 실행 중인지 확인한다.</summary>
    bool IsRunning(string processName);
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
