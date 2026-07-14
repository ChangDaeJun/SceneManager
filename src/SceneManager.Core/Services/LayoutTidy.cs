using SceneManager.Core.Models;

namespace SceneManager.Core.Services;

/// <summary>
/// 씬의 창 배치에서 인접 창의 마주보는 모서리와 화면 경계에 가까운 모서리를 정렬해
/// 작은 틈·겹침을 메운다. 모서리 값을 허용 오차 내로 묶어(clustering) 공통 좌표로 스냅한다.
/// 배치가 없는 항목과 최대화·최소화 창은 건드리지 않는다. 순수 함수(상태 없음).
/// </summary>
public static class LayoutTidy
{
    /// <summary>이 값 이하로 벌어지거나 겹친 모서리끼리 하나로 정렬한다(픽셀).</summary>
    public const double DefaultTolerancePx = 30;

    /// <summary>
    /// 틈·겹침을 메운다. 좌표가 바뀐 창의 수를 반환한다.
    /// </summary>
    public static int SnapGaps(Scene scene, MonitorLayout monitors, double tolerance = DefaultTolerancePx)
    {
        // 대상: 배치가 있고 일반(Normal) 상태인 창만. 최대화·최소화는 정렬 대상이 아니다.
        var items = scene.Programs
            .Where(p => p.Window is { State: WindowState.Normal })
            .ToList();
        if (items.Count == 0)
            return 0;

        var changed = 0;

        // 모니터 경계를 넘나드는 잘못된 정렬을 막기 위해, 창이 가장 많이 겹치는 모니터별로 묶는다.
        foreach (var group in items.GroupBy(p => BestMonitor(p.Window!, monitors)))
        {
            var monitor = group.Key;
            var windows = group.ToList();

            var xMap = BuildSnapMap(
                windows.SelectMany(w => new[] { w.Window!.X, w.Window!.X + w.Window!.Width }),
                MonitorAnchorsX(monitor), tolerance);
            var yMap = BuildSnapMap(
                windows.SelectMany(w => new[] { w.Window!.Y, w.Window!.Y + w.Window!.Height }),
                MonitorAnchorsY(monitor), tolerance);

            foreach (var p in windows)
            {
                var win = p.Window!;
                var left = Snap(xMap, win.X);
                var right = Snap(xMap, win.X + win.Width);
                var top = Snap(yMap, win.Y);
                var bottom = Snap(yMap, win.Y + win.Height);

                // 과도한 병합으로 크기가 사라지면 이 창은 건너뛴다.
                if (right - left < 1 || bottom - top < 1)
                    continue;

                if (left != win.X || top != win.Y
                    || right - left != win.Width || bottom - top != win.Height)
                {
                    win.X = left;
                    win.Y = top;
                    win.Width = right - left;
                    win.Height = bottom - top;
                    changed++;
                }
            }
        }

        return changed;
    }

    /// <summary>창이 가장 많이 겹치는 모니터. 어느 모니터와도 겹치지 않으면 null.</summary>
    private static MonitorInfo? BestMonitor(WindowPlacement w, MonitorLayout monitors)
    {
        MonitorInfo? best = null;
        double bestArea = 0;
        foreach (var m in monitors.Monitors)
        {
            var ox = Overlap(w.X, w.X + w.Width, m.PositionX, m.PositionX + m.PhysicalWidth);
            var oy = Overlap(w.Y, w.Y + w.Height, m.PositionY, m.PositionY + m.PhysicalHeight);
            var area = ox * oy;
            if (area > bestArea)
            {
                bestArea = area;
                best = m;
            }
        }
        return best;
    }

    private static double Overlap(double a1, double a2, double b1, double b2)
        => Math.Max(0, Math.Min(a2, b2) - Math.Max(a1, b1));

    private static double[] MonitorAnchorsX(MonitorInfo? m)
        => m is null ? [] : [m.PositionX, m.PositionX + m.PhysicalWidth];

    private static double[] MonitorAnchorsY(MonitorInfo? m)
        => m is null ? [] : [m.PositionY, m.PositionY + m.PhysicalHeight];

    /// <summary>
    /// 모서리 값들을 허용 오차 내로 묶어 각 값 → 대표 좌표 매핑을 만든다.
    /// 묶음에 화면 경계(anchor)가 포함되면 그 경계로 스냅하고, 아니면 묶음 평균으로 스냅한다.
    /// </summary>
    private static Dictionary<double, double> BuildSnapMap(
        IEnumerable<double> edgeValues, double[] anchors, double tolerance)
    {
        var all = edgeValues.Distinct().Select(v => (val: v, isAnchor: false))
            .Concat(anchors.Select(a => (val: a, isAnchor: true)))
            .OrderBy(t => t.val)
            .ToList();

        var map = new Dictionary<double, double>();
        var i = 0;
        while (i < all.Count)
        {
            var clusterMin = all[i].val;
            var j = i;
            while (j + 1 < all.Count && all[j + 1].val - clusterMin <= tolerance)
                j++;

            var anchorVals = new List<double>();
            var edgeVals = new List<double>();
            for (var k = i; k <= j; k++)
            {
                if (all[k].isAnchor) anchorVals.Add(all[k].val);
                else edgeVals.Add(all[k].val);
            }

            if (edgeVals.Count > 0)
            {
                var rep = anchorVals.Count > 0 ? anchorVals[0] : Math.Round(edgeVals.Average());
                foreach (var e in edgeVals)
                    map[e] = rep;
            }

            i = j + 1;
        }

        return map;
    }

    private static double Snap(Dictionary<double, double> map, double value)
        => map.TryGetValue(value, out var rep) ? rep : value;
}
