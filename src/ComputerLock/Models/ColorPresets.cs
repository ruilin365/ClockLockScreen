using System.Globalization;

namespace ComputerLock.Models;

/// <summary>
/// 预设配色方案（多点渐变 + 流动速度）。
/// 色标数据直接取自 WLED 源码 <c>wled00/palettes.cpp</c> 里的调色板定义，
/// 位置沿用 WLED 的 0-255 索引，换算成 0-1 偏移。
/// </summary>
public sealed class ColorPreset
{
    private IReadOnlyList<ColorStop>? _stops;

    public ColorPreset(string id, string nameZh, string nameEn, double periodSeconds, double brightness, string rawStops)
    {
        Id = id;
        NameZh = nameZh;
        NameEn = nameEn;
        PeriodSeconds = periodSeconds;
        Brightness = brightness;
        RawStops = rawStops;
    }

    public string Id { get; }

    public string NameZh { get; }

    public string NameEn { get; }

    /// <summary>
    /// 渐变流动一圈的秒数（对应 WLED 的 effect speed，数值越大越慢）
    /// </summary>
    public double PeriodSeconds { get; }

    /// <summary>
    /// 亮度系数。WLED 的调色板是给 LED 条用的，直接铺满屏幕会过亮，背景用途要压暗。
    /// </summary>
    public double Brightness { get; }

    /// <summary>
    /// 原始色标，形如 <c>"0:01052D 128:00FF00 255:01052D"</c>
    /// </summary>
    public string RawStops { get; }

    public string DisplayName => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh"
        ? NameZh
        : NameEn;

    public IReadOnlyList<ColorStop> Stops => _stops ??= Parse(RawStops);

    /// <summary>
    /// 给 CSS 用的渐变串（设置界面的预览条）
    /// </summary>
    public string CssGradient
    {
        get
        {
            // 预览按实际渲染亮度显示（略提亮，否则暗色预设看起来就是一片黑）
            var factor = Math.Min(1d, Brightness * 1.6);
            var parts = Stops.Select(s =>
                $"#{ToRgb(s.Hex, factor)} {(s.Offset * 100).ToString("0.#", CultureInfo.InvariantCulture)}%");
            return $"linear-gradient(90deg, {string.Join(", ", parts)})";
        }
    }

    private static string ToRgb(string hex, double factor)
    {
        var value = Convert.ToInt32(hex.TrimStart('#'), 16);
        var r = (byte)Math.Round(((value >> 16) & 0xFF) * factor);
        var g = (byte)Math.Round(((value >> 8) & 0xFF) * factor);
        var b = (byte)Math.Round((value & 0xFF) * factor);
        return $"{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// 一份调色板横跨几个屏幕宽度。色标多的调色板（如 17 个色标的 April Night）
    /// 若只铺一屏会变成密集彩虹条纹，跨度拉宽后色带更宽、更像环境光。
    /// </summary>
    public double SpanScreens => Math.Clamp(Stops.Count / 6d, 1d, 3d);

    private static IReadOnlyList<ColorStop> Parse(string raw)
    {
        var result = new List<ColorStop>();
        foreach (var token in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = token.Split(':');
            if (kv.Length != 2)
            {
                continue;
            }
            if (!int.TryParse(kv[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pos))
            {
                continue;
            }
            result.Add(new ColorStop(Math.Clamp(pos / 255d, 0d, 1d), "#" + kv[1]));
        }
        return result;
    }
}

public sealed record ColorStop(double Offset, string Hex);

/// <summary>
/// 预设表。配色与色标位置来自 WLED 源码，命名沿用 WLED 官方调色板名。
/// </summary>
public static class ColorPresets
{
    /// <summary>
    /// 时钟背景用：铺满整屏，因此整体压暗；流动周期拉长到几十秒，避免抢注意力
    /// </summary>
    public static readonly IReadOnlyList<ColorPreset> Background =
    [
        new("bg-royal", "皇家石南", "Royal Heath", 42, 1.00,
            "0:79DDE8 63:F58CC2 140:C46BE8 200:FFD9E8 255:8FA8F0"),
        new("bg-eggsour", "奶油橙", "Egg Sour", 48, 0.98,
            "0:FFF0C2 45:FFB26B 110:F2603F 170:FFE066 255:8B4BD6"),
        new("bg-snowmint", "雪薄荷", "Snowy Mint", 44, 1.00,
            "0:DCEFFF 55:A8F2C8 130:B9C6FF 200:E8F6FF 255:9FE6FF"),
        new("bg-pinkhaze", "粉雾", "Pink Haze", 40, 0.98,
            "0:FF6FA8 60:D46BE0 130:8E6BE8 200:FFD5E5 255:B06BE8"),
        new("bg-sunsetpop", "落日", "Sunset Pop", 46, 0.96,
            "0:FFD24A 55:FF8A3D 120:00C2B8 190:2F7BE8 255:7BE0FF"),
        new("bg-peach", "桃紫", "Peach Lilac", 50, 1.00,
            "0:FFB08A 55:F2A8D0 125:C9B6F2 195:FFE9DC 255:A8C4F5"),
        new("bg-black", "纯黑", "Pure Black", 0, 1,
            "0:000000 255:000000"),
        new("bg-graphite", "石墨", "Graphite", 0, 1,
            "0:101014 255:22222A"),
    ];

    /// <summary>
    /// 呼吸灯 / 圆点用：细条上要看得清，保持 WLED 原色不做压暗
    /// </summary>
    public static readonly IReadOnlyList<ColorPreset> Indicator =
    [
        new("ind-orange", "琥珀", "Amber", 0, 1,
            "0:FB8C00 255:FB8C00"),
        new("ind-aurora", "极光", "Aurora", 10, 1,
            "0:01052D 64:00C817 128:00FF00 170:00F32D 200:008707 255:01052D"),
        new("ind-sunset", "日落", "Sunset", 9, 1,
            "0:B50000 22:DA5500 51:FFAA00 85:D3554D 135:A700A9 198:4900BC 255:0000CF"),
        new("ind-ocean", "海洋", "Ocean", 10, 1,
            "0:103033 89:1BA6AF 153:C5E9FF 255:009198"),
        new("ind-lava", "熔岩", "Lava", 8, 1,
            "0:4D0000 96:B10000 146:EB731D 202:FFCC29 234:FFFF29 255:FFFF8F"),
        new("ind-tiamat", "提亚马特", "Tiamat", 12, 1,
            "0:020523 100:0D875C 120:2BFFC1 140:F707F9 180:27FF9A 200:04D5EC 240:C1D5FD 255:FFF9FF"),
        new("ind-sakura", "樱花", "Sakura", 9, 1,
            "0:C4130A 65:FF452D 130:DF2D48 195:FF5267 255:DF0D11"),
        new("ind-candy", "糖果", "Candy", 8, 1,
            "0:FF6629 43:FF8C5A 86:FF335A 127:FF99A9 170:FFFFF9 209:71FF55 255:9DFF89"),
        new("ind-orangeteal", "橙青", "Orange & Teal", 11, 1,
            "0:00965C 55:00965C 200:FF4800 255:FF4800"),
        new("ind-pinksplash", "粉溅", "Pink Splash", 9, 1,
            "0:BA3FFF 127:E30955 175:EACDD5 221:CD26B0 255:CD26B0"),
        new("ind-analog", "类似色", "Analogous", 12, 1,
            "0:2600FF 63:5600FF 127:8B00FF 191:C40075 255:FF0000"),
        new("ind-drywet", "干湿", "Dry Wet", 11, 1,
            "0:776121 42:EBC758 84:A9EE7C 127:25EEE8 170:0778EC 212:1B01AF 255:043365"),
    ];

    public static ColorPreset? Find(IReadOnlyList<ColorPreset> list, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        return list.FirstOrDefault(p => p.Id == id);
    }
}
