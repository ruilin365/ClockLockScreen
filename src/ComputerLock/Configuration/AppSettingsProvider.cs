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

            if (!appSettings.DefaultBackgroundApplied)
            {
                appSettings.ClockBackgroundMode = ClockBackgroundMode.Solid;
                appSettings.ClockBackgroundColor1 = "#000000";
                appSettings.ClockBackgroundPreset = "";
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

        var directory = Path.GetDirectoryName(AppBase.ConfigPath) ?? throw new ArgumentException(lang["ConfigFilePathError"]);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        string appConfigString = System.Text.Json.JsonSerializer.Serialize(settings);
        File.WriteAllText(AppBase.ConfigPath, appConfigString);
    }

    public void RemoveSettings()
    {
        if (File.Exists(AppBase.ConfigPath))
        {
            File.Delete(AppBase.ConfigPath);
        }
    }
}