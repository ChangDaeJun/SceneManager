using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SceneManager.Core.Models;

namespace SceneManager.Controls;

/// <summary>
/// 모니터 구성과 씬의 프로그램 창 배치를 물리 px 바운딩 박스 기준으로 비례 축소해 그린다.
/// 모니터는 외곽선, 프로그램은 채운 사각형 + 이름 라벨.
/// </summary>
public sealed class LayoutPreview : Canvas
{
    public static readonly DependencyProperty MonitorsProperty =
        DependencyProperty.Register(nameof(Monitors), typeof(MonitorLayout), typeof(LayoutPreview),
            new PropertyMetadata(null, OnChanged));

    public static readonly DependencyProperty SceneProperty =
        DependencyProperty.Register(nameof(Scene), typeof(Scene), typeof(LayoutPreview),
            new PropertyMetadata(null, OnChanged));

    public MonitorLayout? Monitors
    {
        get => (MonitorLayout?)GetValue(MonitorsProperty);
        set => SetValue(MonitorsProperty, value);
    }

    public Scene? Scene
    {
        get => (Scene?)GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public LayoutPreview()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;
        SizeChanged += (_, _) => Redraw();
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((LayoutPreview)d).Redraw();

    /// <summary>최소화(또는 -32000 sentinel) 창은 배치 지도에서 제외한다.</summary>
    private static bool IsOnScreen(WindowPlacement w)
        => w.State != Core.Models.WindowState.Minimized && w.X > -30000 && w.Y > -30000;

    private void Redraw()
    {
        Children.Clear();

        double w = ActualWidth, h = ActualHeight;
        if (w < 4 || h < 4)
            return;

        var monitors = Monitors?.Monitors ?? new List<MonitorInfo>();
        var programs = Scene?.Programs
            .Where(p => p.Window is not null && IsOnScreen(p.Window!))
            .ToList() ?? new List<ProgramEntry>();
        if (monitors.Count == 0 && programs.Count == 0)
            return;

        // 월드 바운딩 박스(물리 px)
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        void Extend(double x, double y, double ww, double hh)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x + ww);
            maxY = Math.Max(maxY, y + hh);
        }

        foreach (var m in monitors)
            Extend(m.PositionX, m.PositionY, m.PhysicalWidth, m.PhysicalHeight);
        foreach (var p in programs)
            Extend(p.Window!.X, p.Window!.Y, p.Window!.Width, p.Window!.Height);

        double worldW = maxX - minX, worldH = maxY - minY;
        if (worldW <= 0 || worldH <= 0)
            return;

        const double pad = 8;
        double scale = Math.Min((w - 2 * pad) / worldW, (h - 2 * pad) / worldH);
        double offX = (w - worldW * scale) / 2, offY = (h - worldH * scale) / 2;
        double Sx(double x) => (x - minX) * scale + offX;
        double Sy(double y) => (y - minY) * scale + offY;

        // 모니터 외곽선
        foreach (var m in monitors)
        {
            var rect = new Rectangle
            {
                Width = Math.Max(1, m.PhysicalWidth * scale),
                Height = Math.Max(1, m.PhysicalHeight * scale),
                Stroke = Brushes.Gray,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)),
            };
            SetLeft(rect, Sx(m.PositionX));
            SetTop(rect, Sy(m.PositionY));
            Children.Add(rect);
        }

        // 프로그램 사각형 + 이름 라벨
        var accent = Color.FromRgb(0x3B, 0x82, 0xF6);
        foreach (var p in programs)
        {
            var win = p.Window!;
            double rw = Math.Max(2, win.Width * scale), rh = Math.Max(2, win.Height * scale);

            var rect = new Rectangle
            {
                Width = rw,
                Height = rh,
                Stroke = new SolidColorBrush(accent),
                StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Color.FromArgb(60, accent.R, accent.G, accent.B)),
                ToolTip = $"{p.Name}  ({win.X},{win.Y}) {win.Width}x{win.Height}",
            };
            SetLeft(rect, Sx(win.X));
            SetTop(rect, Sy(win.Y));
            Children.Add(rect);

            var label = new TextBlock
            {
                Text = p.Name,
                FontSize = 11,
                Foreground = Brushes.Black,
                Margin = new Thickness(3, 1, 0, 0),
                MaxWidth = rw,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            SetLeft(label, Sx(win.X));
            SetTop(label, Sy(win.Y));
            Children.Add(label);
        }
    }
}
