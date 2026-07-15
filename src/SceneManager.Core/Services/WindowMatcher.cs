using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Core.Services;

/// <summary>
/// 씬의 프로그램 항목을 현재 살아있는 창(HWND)에 매칭한다. 스냅샷 직후 미세조정처럼
/// "저장된 항목 → 실제 창"을 되찾을 때 쓴다. 프로세스명 + 창 제목으로 고르고, 같은 프로세스의
/// 여러 창(예: 엑셀 2개)은 한 번 배정한 창을 다시 잡지 않아 서로 다른 핸들에 매핑된다.
/// </summary>
public static class WindowMatcher
{
    /// <summary>
    /// 각 프로그램(Id)을 대응하는 창 핸들에 매핑한다. 매칭되는 창이 없으면 그 프로그램은 결과에서 빠진다.
    /// </summary>
    public static Dictionary<string, IntPtr> ResolveHandles(
        IReadOnlyList<ProgramEntry> programs, IReadOnlyList<WindowInfo> windows)
    {
        var map = new Dictionary<string, IntPtr>();
        var claimed = new HashSet<IntPtr>();

        foreach (var program in programs)
        {
            var processName = Path.GetFileNameWithoutExtension(program.ExecPath);
            var match = SelectWindow(processName, program.WindowTitle, windows, claimed);
            if (match is null)
                continue;

            map[program.Id] = match.Handle;
            claimed.Add(match.Handle);
        }

        return map;
    }

    /// <summary>
    /// 같은 프로세스명 + 아직 배정 안 된 창 중에서 제목 정확 일치 &gt; 부분 일치 &gt; 가장 큰 창 순.
    /// 스냅샷 직후라 제목이 정확히 일치하는 것이 보통이다.
    /// </summary>
    private static WindowInfo? SelectWindow(
        string processName, string? title, IReadOnlyList<WindowInfo> windows, HashSet<IntPtr> claimed)
    {
        var candidates = windows
            .Where(w => string.Equals(w.ProcessName, processName, StringComparison.OrdinalIgnoreCase)
                        && !claimed.Contains(w.Handle))
            .ToList();
        if (candidates.Count == 0)
            return null;

        if (!string.IsNullOrEmpty(title))
        {
            var exact = candidates.FirstOrDefault(
                w => string.Equals(w.WindowTitle, title, StringComparison.Ordinal));
            if (exact is not null)
                return exact;

            var partial = candidates.FirstOrDefault(w =>
                (w.WindowTitle.Length > 0 && title.Contains(w.WindowTitle, StringComparison.OrdinalIgnoreCase))
                || w.WindowTitle.Contains(title, StringComparison.OrdinalIgnoreCase));
            if (partial is not null)
                return partial;
        }

        return candidates.OrderByDescending(w => w.Placement.Width * w.Placement.Height).First();
    }
}
