using System.Runtime.InteropServices;
using System.Text;

namespace SceneManager.Core.Platform;

/// <summary>
/// 패키지(UWP/MSIX) 앱 관련 Win32 헬퍼.
/// </summary>
public static class PackagedApps
{
    /// <summary>
    /// 실행 중 프로세스의 AUMID(Application User Model ID)를 얻는다.
    /// 패키지 앱이 아니거나 접근 불가면 null.
    /// </summary>
    public static string? TryGetAumid(int processId)
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

    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetApplicationUserModelId(IntPtr hProcess, ref uint applicationUserModelIdLength, StringBuilder? applicationUserModelId);
}
