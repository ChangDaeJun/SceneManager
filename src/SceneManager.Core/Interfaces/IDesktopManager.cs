using SceneManager.Core.Models;

namespace SceneManager.Core.Interfaces;

/// <summary>
/// 데스크톱 제어를 담당한다: 프로세스 실행·조회 + 윈도우 열거·배치·닫기.
/// 씬 실행과 스냅샷 모두 "실행 중인 앱과 그 창"을 다루므로 하나의 매니저로 통합한다.
/// Win32/UWP 실행 방식과 논리↔물리 좌표 차이는 구현 내부에서 흡수한다.
/// </summary>
public interface IDesktopManager
{
    // ────── 프로세스 ──────

    /// <summary>프로그램을 실행한다(Win32 + UWP 통합).</summary>
    Task<ProcessLaunchResult> LaunchAsync(ProgramEntry entry, CancellationToken cancellationToken = default);

    /// <summary>특정 프로세스가 실행 중인지 확인한다.</summary>
    bool IsRunning(string processName);

    // ────── 윈도우 ──────

    /// <summary>현재 보이는 모든 윈도우의 정보를 가져온다.</summary>
    List<WindowInfo> GetAllVisibleWindows();

    /// <summary>윈도우의 현재 배치 정보를 읽는다(논리 픽셀).</summary>
    WindowPlacement GetPlacement(IntPtr hwnd);

    /// <summary>윈도우에 배치를 적용한다(논리 픽셀 → 물리 픽셀 변환은 내부 처리).</summary>
    void SetPlacement(IntPtr hwnd, WindowPlacement placement);

    /// <summary>
    /// 윈도우에 닫기(WM_CLOSE)를 요청한다. 사용자가 X를 누른 것과 동일하며,
    /// 앱이 저장 대화상자를 띄울 수 있다(강제 종료 아님). 프로세스는 죽이지 않는다.
    /// </summary>
    void CloseWindow(IntPtr hwnd);

    // ────── 모니터 ──────

    /// <summary>현재 연결된 모니터 구성을 가져온다(물리 픽셀, 가상 스크린 좌표).</summary>
    MonitorLayout GetMonitorLayout();
}

/// <summary>
/// 보이는 윈도우 하나의 정보.
/// </summary>
public sealed class WindowInfo
{
    /// <summary>윈도우 핸들(HWND).</summary>
    public IntPtr Handle { get; set; }

    /// <summary>소유 프로세스 ID.</summary>
    public int ProcessId { get; set; }

    /// <summary>소유 프로세스명.</summary>
    public required string ProcessName { get; set; }

    /// <summary>윈도우 제목. 제목이 없으면 빈 문자열.</summary>
    public required string WindowTitle { get; set; }

    /// <summary>윈도우 클래스명. 같은 프로세스의 창 종류(예: 메인 vs 대화방) 구분에 쓴다.</summary>
    public string WindowClass { get; set; } = string.Empty;

    /// <summary>실행 파일 경로. 접근 불가(보호/승격 프로세스) 시 null.</summary>
    public string? ExecPath { get; set; }

    /// <summary>스토어(UWP/MSIX) 앱의 AUMID. Win32 앱은 null.</summary>
    public string? AppUserModelId { get; set; }

    /// <summary>현재 배치 정보(논리 픽셀).</summary>
    public required WindowPlacement Placement { get; set; }
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
