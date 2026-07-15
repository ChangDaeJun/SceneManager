using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;
using SceneManager.Core.Services;

namespace SceneManager.Core.Tests.Services;

public class WindowMatcherTests
{
    private static ProgramEntry Program(string id, string exec, string? title) => new()
    {
        Id = id, Name = id, ExecPath = exec, WindowTitle = title,
        Window = new WindowPlacement { MonitorId = "M", X = 0, Y = 0, Width = 100, Height = 100 },
    };

    private static WindowInfo Window(nint handle, string processName, string title, int w = 100, int h = 100) => new()
    {
        Handle = handle, ProcessId = (int)handle, ProcessName = processName, WindowTitle = title,
        Placement = new WindowPlacement { MonitorId = "M", X = 0, Y = 0, Width = w, Height = h },
    };

    [Fact]
    public void ResolveHandles_TwoSameProcessWindows_MappedByTitleToDistinctHandles()
    {
        var programs = new[]
        {
            Program("p1", @"C:\Office\EXCEL.EXE", "작업 리스트 - Excel"),
            Program("p2", @"C:\Office\EXCEL.EXE", "코딩테스트 준비 - Excel"),
        };
        var windows = new[]
        {
            Window(101, "EXCEL", "코딩테스트 준비 - Excel"),
            Window(102, "EXCEL", "작업 리스트 - Excel"),
        };

        var map = WindowMatcher.ResolveHandles(programs, windows);

        Assert.Equal(new IntPtr(102), map["p1"]); // 작업 리스트
        Assert.Equal(new IntPtr(101), map["p2"]); // 코딩테스트 준비
        Assert.NotEqual(map["p1"], map["p2"]);     // 서로 다른 창
    }

    [Fact]
    public void ResolveHandles_NoMatchingWindow_ProgramOmitted()
    {
        var programs = new[] { Program("p1", @"C:\a\notepad.exe", "메모장") };
        var windows = new[] { Window(1, "chrome", "Google") };

        var map = WindowMatcher.ResolveHandles(programs, windows);

        Assert.False(map.ContainsKey("p1"));
    }

    [Fact]
    public void ResolveHandles_SingleWindowTitleDrift_FallsBackToLargest()
    {
        var programs = new[] { Program("p1", @"C:\a\app.exe", "예전 제목") };
        var windows = new[] { Window(5, "app", "완전히 다른 제목", 200, 200) };

        var map = WindowMatcher.ResolveHandles(programs, windows);

        Assert.Equal(new IntPtr(5), map["p1"]); // 폴백: 유일한 같은 프로세스 창
    }
}
