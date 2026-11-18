using System.IO;
using System.Text.Json;

namespace ComputerLock.Configuration;
public class AppSettingsProvider(IStringLocalizer<Lang> lang)
{
    public AppSettings LoadSettings()
    {
        if (!File.Exists(AppBase.ConfigPath))
        {
            return new AppSettings();
        }
        string json = File.ReadAllText(AppBase.ConfigPath);
        try
        {
            var appSettings = JsonSerializer.Deserialize<AppSettings>(json);
            if (appSettings == null)
            {
                return new AppSettings();
            }

            // 老配置里没有 DefaultBackgroundApplied 字段，读出来是 false：
            // 说明这份配置是「网格渐变当默认」那个年代留下的，替它迁移一次到纯黑纯色。
            // 迁移完立刻落盘，之后用户再怎么改都不会被覆盖。
            var migrated = false;

            // 「时钟背景」现在是「遮罩颜色」：两者本来就是同一件事 —— 铺满整屏的一层颜色。
            // 老配置里的配色、预设、流动设置整体搬过去，顺带决定遮罩要不要打开。
            if (!appSettings.MaskFillMigrated)
            {
                MigrateMaskFill(rootOf(json), appSettings);
                migrated = true;
            }

            if (!appSettings.DefaultBackgroundApplied)
            {
                appSettings.LockMaskEnabled = true;
                appSettings.MaskFillMode = ClockBackgroundMode.Solid;
                appSettings.MaskColor1 = "#000000";
                appSettings.MaskPreset = "";
                appSettings.LockMaskOpacity = 95;
                appSettings.ClockOpacity = 80;
                migrated = true;
            }

            // 默认字体从 Barlow Condensed 换成 Bebas Neue，老配置换成新默认。
            // 只在当前还是「旧默认」时才动，用户自己选过别的字体就不碰。
            if (!appSettings.DefaultFontApplied)
            {
                if (string.IsNullOrWhiteSpace(appSettings.ClockFontId) || appSettings.ClockFontId == "barlow")
                {
                    appSettings.ClockFontId = Models.ClockFonts.DefaultId;
                }
                migrated = true;
            }

            // 锁屏样式原来是「完全透明 / 黑色遮罩 / 翻页时钟」三选一，
            // 现在拆成模糊、遮罩、时钟三个独立开关。老配置按原来那一项把对应开关打开。
#pragma warning disable CS0618 // LockStyle 只为读取老配置保留
            if (!appSettings.SplitLockLayersApplied)
            {
                appSettings.LockBlurEnabled = false;
                appSettings.LockMaskEnabled = appSettings.LockStyle == LockScreenStyle.BlackMask;
                appSettings.LockClockEnabled = appSettings.LockStyle == LockScreenStyle.Clock;
                migrated = true;
            }
#pragma warning restore CS0618

            // 注意：SaveSettings 会把两个标记都置 true，所以必须在两段判断都跑完之后再落盘
            if (migrated)
            {
                SaveSettings(appSettings);
            }

            return appSettings;
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        // 只要落过盘，就认为默认值迁移已经是用户的选择，不再自动改回来
        settings.DefaultBackgroundApplied = true;
        settings.DefaultFontApplied = true;
        settings.SplitLockLayersApplied = true;
        settings.MaskFillMigrated = true;

        var directory = Path.GetDirectoryName(AppBase.ConfigPath) ?? throw new ArgumentException(lang["ConfigFilePathError"]);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        string appConfigString = System.Text.Json.JsonSerializer.Serialize(settings);
        File.WriteAllText(AppBase.ConfigPath, appConfigString);
    }

    /// <summary>
    /// 把老的「时钟背景」一组字段搬到「遮罩颜色」上
    /// </summary>
    private static void MigrateMaskFill(JsonElement root, AppSettings settings)
    {
        settings.MaskFillMode = ReadEnum(root, "ClockBackgroundMode", ClockBackgroundMode.Solid);
        settings.MaskColor1 = ReadString(root, "ClockBackgroundColor1", "#000000");
        settings.MaskColor2 = ReadString(root, "ClockBackgroundColor2", "#2B2B45");
        settings.MaskAngle = ReadDouble(root, "ClockBackgroundAngle", 45);
        settings.MaskPreset = ReadString(root, "ClockBackgroundPreset", "");
        settings.MaskAnimated = ReadBool(root, "ClockBackgroundAnimated", true);
        settings.MaskSpeed = ReadDouble(root, "ClockBackgroundSpeed", 100);

        var hadMask = settings.LockMaskEnabled;
        var hadBackground = settings.MaskFillMode != ClockBackgroundMode.None;
        settings.LockMaskEnabled = hadMask || hadBackground;

        // 原来只开了时钟背景的，用它的不透明度当遮罩不透明度
        if (!hadMask && hadBackground)
        {
            settings.LockMaskOpacity = ReadDouble(root, "ClockBackgroundOpacity", 95);
        }
    }

    private static JsonElement rootOf(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static bool TryGet(JsonElement root, string name, out JsonElement value)
    {
        value = default;
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out value);
    }

    private static string ReadString(JsonElement root, string name, string fallback)
    {
        if (!TryGet(root, name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return fallback;
        }
        return value.GetString() ?? fallback;
    }

    private static double ReadDouble(JsonElement root, string name, double fallback)
    {
        if (!TryGet(root, name, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return fallback;
        }
        return value.GetDouble();
    }

    private static bool ReadBool(JsonElement root, string name, bool fallback)
    {
        if (!TryGet(root, name, out var value)
            || (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
        {
            return fallback;
        }
        return value.GetBoolean();
    }

    private static ClockBackgroundMode ReadEnum(JsonElement root, string name, ClockBackgroundMode fallback)
    {
        var raw = ReadDouble(root, name, -1);
        return raw < 0 ? fallback : (ClockBackgroundMode)(int)raw;
    }

    public void RemoveSettings()
    {
        if (File.Exists(AppBase.ConfigPath))
        {
            File.Delete(AppBase.ConfigPath);
        }
    }
}