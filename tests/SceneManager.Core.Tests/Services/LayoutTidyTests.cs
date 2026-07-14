using SceneManager.Core.Models;
using SceneManager.Core.Services;

namespace SceneManager.Core.Tests.Services;

public class LayoutTidyTests
{
    private static MonitorLayout OneMonitor(double x, double y, int w, int h) => new()
    {
        Monitors = [new MonitorInfo
        {
            Id = "M", DeviceName = "M",
            PositionX = x, PositionY = y, PhysicalWidth = w, PhysicalHeight = h,
        }]
    };

    private static ProgramEntry Win(string id, double x, double y, double w, double h,
        WindowState state = WindowState.Normal) => new()
    {
        Id = id, Name = id, ExecPath = $"C:\\{id}.exe",
        WindowTitle = id,
        Window = new WindowPlacement { MonitorId = "M", X = x, Y = y, Width = w, Height = h, State = state },
    };

    private static Scene SceneOf(params ProgramEntry[] programs) => new()
    {
        Id = "s", Name = "s", Programs = [.. programs],
    };

    [Fact]
    public void SnapGaps_ClosesSmallVerticalGap_WindowsMeet()
    {
        var top = Win("A", 0, 0, 1000, 500);      // 아래 모서리 500
        var bottom = Win("B", 0, 505, 1000, 495); // 위 모서리 505 (5px 틈)
        var scene = SceneOf(top, bottom);

        var changed = LayoutTidy.SnapGaps(scene, OneMonitor(0, 0, 1000, 1000));

        Assert.True(changed > 0);
        var aBottom = top.Window!.Y + top.Window!.Height;
        Assert.Equal(aBottom, bottom.Window!.Y); // 틈 없이 맞닿음
    }

    [Fact]
    public void SnapGaps_ClosesSmallOverlap_WindowsMeet()
    {
        var left = Win("A", 0, 0, 505, 1000);   // 오른쪽 모서리 505
        var right = Win("B", 500, 0, 500, 1000); // 왼쪽 모서리 500 (5px 겹침)
        var scene = SceneOf(left, right);

        LayoutTidy.SnapGaps(scene, OneMonitor(0, 0, 1000, 1000));

        var aRight = left.Window!.X + left.Window!.Width;
        Assert.Equal(aRight, right.Window!.X);
    }

    [Fact]
    public void SnapGaps_SnapsEdgesNearMonitorBoundary_ToBoundary()
    {
        var w = Win("A", 5, 6, 988, 989); // 990 근처가 아니라 경계(0,0,1000,1000)에 가까움
        var scene = SceneOf(w);

        LayoutTidy.SnapGaps(scene, OneMonitor(0, 0, 1000, 1000));

        Assert.Equal(0, w.Window!.X);
        Assert.Equal(0, w.Window!.Y);
        Assert.Equal(1000, w.Window!.Width);
        Assert.Equal(1000, w.Window!.Height);
    }

    [Fact]
    public void SnapGaps_LargeGap_LeftUnchanged()
    {
        // 모니터 중앙에 떨어뜨려 경계 스냅 영향 배제, 세로 간격 100(허용오차 초과).
        var a = Win("A", 500, 500, 400, 200); // 아래 700
        var b = Win("B", 500, 800, 400, 200); // 위 800
        var scene = SceneOf(a, b);

        var changed = LayoutTidy.SnapGaps(scene, OneMonitor(0, 0, 2000, 2000));

        Assert.Equal(0, changed);
        Assert.Equal(700, a.Window!.Y + a.Window!.Height);
        Assert.Equal(800, b.Window!.Y);
    }

    [Fact]
    public void SnapGaps_MaximizedWindow_Untouched()
    {
        var m = Win("A", -8, -8, 1936, 1096, WindowState.Maximized);
        var scene = SceneOf(m);

        var changed = LayoutTidy.SnapGaps(scene, OneMonitor(0, 0, 1920, 1080));

        Assert.Equal(0, changed);
        Assert.Equal(-8, m.Window!.X);
    }
}
