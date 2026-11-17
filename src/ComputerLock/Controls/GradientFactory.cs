using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ComputerLock.Models;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Point = System.Windows.Point;
using ColorConverter = System.Windows.Media.ColorConverter;
using LinearGradientBrush = System.Windows.Media.LinearGradientBrush;
using GradientStop = System.Windows.Media.GradientStop;
using RotateTransform = System.Windows.Media.RotateTransform;

namespace ComputerLock.Controls;

/// <summary>
/// 把 WLED 调色板（多点渐变）转成 WPF 画刷。
/// </summary>
public static class GradientFactory
{
    /// <summary>
    /// 取预设的色标，并按亮度系数压暗。preset 为空时用两个自定义颜色拼一个两色渐变。
    /// </summary>
    public static IReadOnlyList<(double Offset, Color Color)> GetStops(
        ColorPreset? preset,
        string fallbackColor1,
        string fallbackColor2,
        double extraDim = 1d)
    {
        if (preset == null)
        {
            var c1 = ParseColor(fallbackColor1, Colors.Black);
            var c2 = ParseColor(fallbackColor2, c1);
            return [(0d, Scale(c1, extraDim)), (1d, Scale(c2, extraDim))];
        }

        var scale = preset.Brightness * extraDim;
        return preset.Stops
            .Select(s => (s.Offset, Scale(ParseColor(s.Hex, Colors.Black), scale)))
            .ToArray();
    }

    /// <summary>
    /// 静态渐变（不做流动），角度按设置
    /// </summary>
    public static LinearGradientBrush CreateStatic(IReadOnlyList<(double Offset, Color Color)> stops, double angleDegrees)
    {
        var brush = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5)
        };

        foreach (var (offset, color) in stops)
        {
            brush.GradientStops.Add(new GradientStop(color, offset));
        }

        if (Math.Abs(angleDegrees) > 0.01)
        {
            brush.RelativeTransform = new RotateTransform(angleDegrees, 0.5, 0.5);
        }

        return brush;
    }

    /// <summary>
    /// 可流动渐变：把同一份调色板重复 <paramref name="repeats"/> 遍铺满整块区域。
    /// 每份占 1/repeats，因此位移一个份宽后首尾完全重合，循环无缝——这就是 WLED 的「调色板滚动」。
    /// </summary>
    public static LinearGradientBrush CreateTiled(IReadOnlyList<(double Offset, Color Color)> stops, int repeats)
    {
        if (repeats < 1)
        {
            repeats = 1;
        }

        var brush = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5)
        };

        for (var i = 0; i < repeats; i++)
        {
            var baseOffset = i / (double)repeats;
            foreach (var (offset, color) in stops)
            {
                brush.GradientStops.Add(new GradientStop(color, baseOffset + offset / repeats));
            }
        }

        return brush;
    }

    /// <summary>
    /// 在色标序列上按位置取样（线性插值），用于给网格渐变的每个色块取一个颜色
    /// </summary>
    public static Color SampleAt(IReadOnlyList<(double Offset, Color Color)> stops, double position)
    {
        if (stops.Count == 0)
        {
            return Colors.Black;
        }
        if (stops.Count == 1)
        {
            return stops[0].Color;
        }

        position = Math.Clamp(position, 0d, 1d);
        for (var i = 1; i < stops.Count; i++)
        {
            if (position > stops[i].Offset)
            {
                continue;
            }
            var left = stops[i - 1];
            var right = stops[i];
            var span = right.Offset - left.Offset;
            var blend = span <= 0 ? 0d : (position - left.Offset) / span;
            return Blend(left.Color, right.Color, blend);
        }
        return stops[^1].Color;
    }

    /// <summary>
    /// 把调色板补成首尾同色的往返序列。
    /// 平铺时一份的结尾正好接下一份的开头，不会再「跳一下」。
    /// </summary>
    public static IReadOnlyList<(double Offset, Color Color)> MakeSeamless(
        IReadOnlyList<(double Offset, Color Color)> stops)
    {
        if (stops.Count < 2)
        {
            return stops;
        }

        var loop = new List<(double Offset, Color Color)>(stops);
        for (var i = stops.Count - 2; i >= 1; i--)
        {
            loop.Add(stops[i]);
        }

        var count = loop.Count;
        return loop.Select((s, i) => (i / (double)(count - 1), s.Color)).ToArray();
    }

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0d, 1d);
        return Color.FromArgb(
            (byte)Math.Round(from.A + ((to.A - from.A) * amount)),
            (byte)Math.Round(from.R + ((to.R - from.R) * amount)),
            (byte)Math.Round(from.G + ((to.G - from.G) * amount)),
            (byte)Math.Round(from.B + ((to.B - from.B) * amount)));
    }

    public static Color ParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>
    /// 按系数压暗/提亮，保持色相不变
    /// </summary>
    public static Color Scale(Color color, double factor)
    {
        if (Math.Abs(factor - 1d) < 0.001)
        {
            return color;
        }
        factor = Math.Clamp(factor, 0d, 1d);
        return Color.FromArgb(
            color.A,
            (byte)Math.Round(color.R * factor),
            (byte)Math.Round(color.G * factor),
            (byte)Math.Round(color.B * factor));
    }

    /// <summary>
    /// 提高饱和度：把颜色往远离灰度的方向推，色相不变
    /// </summary>
    public static Color Saturate(Color color, double factor)
    {
        var gray = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
        byte Channel(byte value) => (byte)Math.Clamp(Math.Round(gray + ((value - gray) * factor)), 0, 255);
        return Color.FromArgb(color.A, Channel(color.R), Channel(color.G), Channel(color.B));
    }

    /// <summary>
    /// 速度百分比 → 实际的单圈秒数（百分比越大越快）
    /// </summary>
    public static double ToPeriod(double basePeriodSeconds, double speedPercent)
    {
        if (basePeriodSeconds <= 0)
        {
            return 0;
        }
        var speed = Math.Clamp(speedPercent, 10, 400) / 100d;
        return Math.Clamp(basePeriodSeconds / speed, 1.5, 600);
    }

    public static string ToCssRgb(Color color) =>
        string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}");
}
