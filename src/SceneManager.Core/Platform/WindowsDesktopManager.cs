using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Core.Platform;

/// <summary>
/// Win32/Process 기반 <see cref="IDesktopManager"/> 구현.
/// 프로세스 실행(Win32 exe + 스토어 UWP)과 윈도우 열거·배치·닫기를 담당한다.
/// v0: 좌표는 물리 픽셀 그대로 다룬다(논리 변환 생략).
/// </summary>
public sealed class WindowsDesktopManager : IDesktopManager
{
    // ────────────── 프로세스 ──────────────

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

    // ────────────── 윈도우 ──────────────

    public List<WindowInfo> GetAllVisibleWindows()
    {
        var results = new List<WindowInfo>();

        EnumWindows((hwnd, _) =>
        {
            // 보이지 않거나, 다른 창에 소유된(대화상자 등) 창은 제외
            if (!IsWindowVisible(hwnd))
                return true;

            // DWM으로 "가려진(cloaked)" 창 제외. IsWindowVisible이 true여도
            // 실제로는 화면에 안 그려지는 창(일시중단된 UWP 등)을 걸러낸다.
            if (IsCloaked(hwnd))
                return true;

            // 바탕화면 셸 창(Progman/WorkerW) 제외. explorer의 "Program Manager".
            var className = GetClassNameString(hwnd);
            if (className is "Progman" or "WorkerW")
                return true;

            if (GetWindow(hwnd, GW_OWNER) != IntPtr.Zero)
                return true;

            var title = GetWindowTextString(hwnd);
            if (string.IsNullOrWhiteSpace(title))
                return true;

            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0)
                return true;

            string processName;
            string? execPath = null;
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
                try
                {
                    execPath = proc.MainModule?.FileName; // 보호/승격 프로세스는 실패 → null 유지
                }
                catch
                {
                    // 실행 경로를 못 구해도 창 자체는 유효하므로 계속 진행
                }
            }
            catch
            {
                return true; // 프로세스명조차 못 얻으면 스킵
            }

            if (!GetWindowRect(hwnd, out var rect))
                return true;

            results.Add(new WindowInfo
            {
                Handle = hwnd,
                ProcessId = (int)pid,
                ProcessName = processName,
                WindowTitle = title,
                ExecPath = execPath,
                AppUserModelId = TryGetAumid((int)pid), // 스토어 앱이면 AUMID, 아니면 null
                Placement = ToPlacement(rect)
            });

            return true; // 계속 열거
        }, IntPtr.Zero);

        return results;
    }

    public WindowPlacement GetPlacement(IntPtr hwnd)
    {
        GetWindowRect(hwnd, out var rect);
        return ToPlacement(rect);
    }

    public void CloseWindow(IntPtr hwnd)
        => PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); // 큐에 넣고 즉시 반환(비차단)

    public void SetPlacement(IntPtr hwnd, WindowPlacement placement)
    {
        switch (placement.State)
        {
            case WindowState.Minimized:
                ShowWindow(hwnd, SW_MINIMIZE);
                return;
            case WindowState.Maximized:
                ShowWindow(hwnd, SW_MAXIMIZE);
                return;
        }

        // Normal: 최대화/최소화 상태였다면 먼저 복원한 뒤 위치·크기 지정
        ShowWindow(hwnd, SW_RESTORE);
        SetWindowPos(
            hwnd, IntPtr.Zero,
            (int)placement.X, (int)placement.Y,
            (int)placement.Width, (int)placement.Height,
            SWP_NOZORDER | SWP_NOACTIVATE);
    }

    public MonitorLayout GetMonitorLayout()
    {
        var monitors = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data) =>
        {
            var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                var r = mi.rcMonitor;
                monitors.Add(new MonitorInfo
                {
                    Id = mi.szDevice,
                    DeviceName = mi.szDevice,
                    PhysicalWidth = r.Right - r.Left,
                    PhysicalHeight = r.Bottom - r.Top,
                    LogicalWidth = r.Right - r.Left,   // v0: DPI 변환 생략
                    LogicalHeight = r.Bottom - r.Top,
                    DpiScale = 100,
                    PositionX = r.Left,                // 가상 스크린 물리 좌표
                    PositionY = r.Top,
                    IsPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0,
                });
            }
            return true; // 계속 열거
        }, IntPtr.Zero);

        return new MonitorLayout { Monitors = monitors };
    }

    private static WindowPlacement ToPlacement(RECT rect) => new()
    {
        MonitorId = "primary", // v0: 모니터 매핑 생략
        X = rect.Left,
        Y = rect.Top,
        Width = rect.Right - rect.Left,
        Height = rect.Bottom - rect.Top,
        State = WindowState.Normal
    };

    private static string GetWindowTextString(IntPtr hwnd)
    {
        var len = GetWindowTextLength(hwnd);
        if (len == 0)
            return string.Empty;

        var sb = new StringBuilder(len + 1);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetClassNameString(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        var len = GetClassName(hwnd, sb, sb.Capacity);
        return len == 0 ? string.Empty : sb.ToString();
    }

    private static bool IsCloaked(IntPtr hwnd)
    {
        // DwmGetWindowAttribute 성공(0) 시 cloaked 값이 0이 아니면 가려진 상태.
        var hr = DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out var cloaked, sizeof(int));
        return hr == 0 && cloaked != 0;
    }

    /// <summary>
    /// 실행 중 프로세스의 AUMID(Application User Model ID)를 얻는다.
    /// 패키지(UWP/MSIX) 앱이 아니거나 접근 불가면 null.
    /// </summary>
    private static string? TryGetAumid(int processId)
    {
        var handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (handle == IntPtr.Zero)
            return null;

        try
        {
            uint length = 0;
            // 1차: 길이만 조회(버퍼 null). 패키지 앱이면 ERROR_INSUFFICIENT_BUFFER + 필요 길이.
            var rc = GetApplicationUserModelId(handle, ref length, null);
            if (rc != ERROR_INSUFFICIENT_BUFFER || length == 0)
                return null; // 패키지 앱 아님(ERROR_NO_APPLICATION 등)

            var buffer = new StringBuilder((int)length);
            rc = GetApplicationUserModelId(handle, ref length, buffer);
            return rc == ERROR_SUCCESS ? buffer.ToString() : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    // ────── P/Invoke ──────
    private const uint GW_OWNER = 4;
    private const int DWMWA_CLOAKED = 14;

    // ShowWindow 명령
    private const int SW_MAXIMIZE = 3;
    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;

    // SetWindowPos 플래그
    private const uint SWP_NOZORDER = 0x0004;   // Z-순서 유지
    private const uint SWP_NOACTIVATE = 0x0010; // 활성화(포커스) 안 함

    private const uint WM_CLOSE = 0x0010;

    // AUMID 조회(kernel32)
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hwnd, uint uCmd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetApplicationUserModelId(IntPtr hProcess, ref uint applicationUserModelIdLength, StringBuilder? applicationUserModelId);

    private const int MONITORINFOF_PRIMARY = 1;

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}
