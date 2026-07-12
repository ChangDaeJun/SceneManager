using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Core.Platform;

/// <summary>
/// Win32 API 기반 <see cref="IWindowManager"/> 구현.
/// v0: 좌표는 물리 픽셀 그대로 다룬다(논리 변환 생략).
/// </summary>
public sealed class WindowsWindowManager : IWindowManager
{
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
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
            }
            catch
            {
                return true; // 접근 불가 프로세스는 스킵
            }

            if (!GetWindowRect(hwnd, out var rect))
                return true;

            results.Add(new WindowInfo
            {
                Handle = hwnd,
                ProcessId = (int)pid,
                ProcessName = processName,
                WindowTitle = title,
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

    public Task<IntPtr> WaitForWindowAsync(int processId, int timeoutMs = 10000, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("4단계에서 구현");

    public void SetPlacement(IntPtr hwnd, WindowPlacement placement)
        => throw new NotImplementedException("4단계에서 구현");

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

    // ────── P/Invoke ──────
    private const uint GW_OWNER = 4;
    private const int DWMWA_CLOAKED = 14;

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

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
