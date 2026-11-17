using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ComputerLock.Controls;
using ComputerLock.Models;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Brush = System.Windows.Media.Brush;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace ComputerLock;

/// <summary>
/// 锁屏状态提示（顶部呼吸灯、角落圆点）。
/// 配色与动效取自 WLED 调色板：颜色沿光条流动，而不是静止的两色渐变。
/// </summary>
public static class BreathingLightHelper
{
    private const int FlowRepeats = 3;

    /// <summary>
    /// 光条这类"一直在动但很慢"的动画跟着设置里的「动画帧率」走，默认 24fps
    /// </summary>
    private const int DefaultFrameRate = 60;
    private const double DefaultBarPeriod = 10d;

    private sealed record BarState(IReadOnlyList<(double Offset, Color Color)> Stops, double Period, int FrameRate);

    /// <param name="element">提示元素</param>
    /// <param name="settings">当前配置</param>
    /// <param name="flow">是否让渐变沿光条流动（只有顶部光条适合，圆点太小看不出）</param>
    public static void InitializeBreathingLight(FrameworkElement element, AppSettings? settings = null, bool flow = false)
    {
        var frameRate = (int)Math.Round(Math.Clamp(settings?.AnimationFrameRate ?? DefaultFrameRate, 10, 240));
        ApplyBrush(element, settings, flow, frameRate);
        element.Visibility = Visibility.Visible;
        StartBreathingAnimation(element, frameRate);
    }

    private static void ApplyBrush(FrameworkElement element, AppSettings? settings, bool flow, int frameRate)
    {
        if (settings == null)
        {
            return;
        }

        var preset = ColorPresets.Find(ColorPresets.Indicator, settings.BreathingPreset);
        var stops = GradientFactory.GetStops(preset, settings.BreathingColor1, settings.BreathingColor2);

        if (flow && element is Border border && settings.BreathingAnimated && stops.Count > 1)
        {
            var basePeriod = preset?.PeriodSeconds > 0 ? preset.PeriodSeconds : DefaultBarPeriod;
            var period = GradientFactory.ToPeriod(basePeriod, settings.BreathingSpeed);
            border.Tag = new BarState(stops, period, frameRate);
            border.ClipToBounds = true;
            border.Background = null;
            border.SizeChanged -= BarSizeChanged;
            border.SizeChanged += BarSizeChanged;
            UpdateBar(border);
            return;
        }

        // 静态模式 / 圆点：直接铺渐变
        Brush brush = stops.Count > 1
            ? GradientFactory.CreateStatic(stops, 0)
            : new SolidColorBrush(stops[0].Color);

        switch (element)
        {
            case Border target:
                target.Background = brush;
                break;
            case Shape shape:
                shape.Fill = brush;
                break;
        }
    }

    private static void BarSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Border border)
        {
            UpdateBar(border);
        }
    }

    /// <summary>
    /// 光条内部放一条 3 倍宽的矩形，渐变铺 3 份，位移一个条宽后首尾重合，形成连续流动
    /// </summary>
    private static void UpdateBar(Border border)
    {
        if (border.Tag is not BarState state)
        {
            return;
        }

        var width = border.ActualWidth;
        if (width <= 0)
        {
            return;
        }

        // 同样要放在 Canvas 里，否则超宽矩形会被 Border 做布局裁剪
        if (border.Child is not Canvas canvas)
        {
            canvas = new Canvas();
            border.Child = canvas;
        }

        if (canvas.Children.Count == 0 || canvas.Children[0] is not Rectangle rect)
        {
            rect = new Rectangle { RenderTransform = new TranslateTransform() };
            canvas.Children.Add(rect);
        }

        var height = border.ActualHeight > 0 ? border.ActualHeight : 4;
        rect.Width = width * FlowRepeats;
        rect.Height = height;
        rect.Fill = GradientFactory.CreateTiled(state.Stops, FlowRepeats);

        var animation = new DoubleAnimation(0, -width, TimeSpan.FromSeconds(state.Period))
        {
            RepeatBehavior = RepeatBehavior.Forever,
            FillBehavior = FillBehavior.HoldEnd
        };
        // 光条是极慢的流动，24fps 完全够，但每帧都要重新合成一遍整屏
        Timeline.SetDesiredFrameRate(animation, state.FrameRate);
        if (rect.RenderTransform is TranslateTransform transform)
        {
            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }
    }

    private static void StartBreathingAnimation(FrameworkElement element, int frameRate)
    {
        var animation = new Storyboard();

        var opacityAnimation = new DoubleAnimation
        {
            From = 0.1,
            To = 0.5,
            Duration = TimeSpan.FromSeconds(1),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Timeline.SetDesiredFrameRate(opacityAnimation, frameRate);

        Storyboard.SetTarget(opacityAnimation, element);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));

        animation.Children.Add(opacityAnimation);
        animation.Begin();
    }
}
