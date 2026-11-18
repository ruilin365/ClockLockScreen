using System.Text.Json.Serialization;

using ComputerLock.Models;

namespace ComputerLock.Configuration;
public class AppSettings
{
    [JsonIgnore]
    private HotkeyTools? _hotkeyTools;

    public void Initialize(HotkeyTools hotkeyTools)
    {
        _hotkeyTools = hotkeyTools;
    }

    /// <summary>
    /// 主题
    /// </summary>
    [JsonPropertyName("AppThemeInt")]
    public ThemeEnum AppTheme { get; set; } = ThemeEnum.System;

    /// <summary>
    /// 按下 ESC 键后最小化到托盘
    /// </summary>
    public bool IsHideWindowWhenEsc { get; set; } = false;

    /// <summary>
    /// 关闭窗口时最小化到托盘
    /// </summary>
    public bool IsHideWindowWhenClose { get; set; } = true;

    /// <summary>
    /// 启动后最小化到托盘
    /// </summary>
    public bool IsHideWindowWhenLaunch { get; set; } = true;

    /// <summary>
    /// 自动移动鼠标
    /// </summary>
    public bool IsDisableWindowsLock { get; set; } = true;

    /// <summary>
    /// Windows 锁定时自动解锁程序
    /// </summary>
    public bool IsUnlockWhenWindowsLock { get; set; } = true;

    /// <summary>
    /// 锁定时显示动画
    /// </summary>
    public bool LockAnimation { get; set; } = true;

    /// <summary>
    /// 锁定提示
    /// </summary>
    public bool LockTips { get; set; } = false;

    /// <summary>
    /// 自定义锁定提示文案
    /// </summary>
    public string? LockTipsMessage { get; set; }

    /// <summary>
    /// 程序启动时锁定
    /// </summary>
    public bool LockOnStartup { get; set; } = false;

    /// <summary>
    /// 自动锁定的秒数
    /// </summary>
    public int AutoLockSecond { get; set; } = 0;

    /// <summary>
    /// 自动锁定的分钟数
    /// </summary>
    public int AutoLockMinute => AutoLockSecond / 60;

    /// <summary>
    /// 密码框的位置
    /// </summary>
    public ScreenLocationEnum PasswordInputLocation { get; set; } = ScreenLocationEnum.Center;

    /// <summary>
    /// 语言
    /// </summary>
    public LangEnum Lang { get; set; } = LangEnum.zh;

    /// <summary>
    /// 解锁密码
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// 锁屏快捷键
    /// </summary>
    [JsonPropertyName("ShortcutKeyForLock")]
    public string LockHotkeyString { get; set; } = "";

    /// <summary>
    /// 锁屏快捷键(映射到按键名称，用于主界面显示)
    /// </summary>
    [JsonIgnore]
    public string LockHotkeyDisplay => _hotkeyTools?.StringKeyToDisplay(LockHotkeyString) ?? "";

    /// <summary>
    /// 锁屏快捷键
    /// </summary>
    [JsonIgnore]
    public Hotkey? LockHotkey => _hotkeyTools?.StringKeyToHotkey(LockHotkeyString);

    /// <summary>
    /// 解锁时使用锁屏快捷键
    /// </summary>
    public bool IsUnlockUseLockHotkey { get; set; } = true;

    /// <summary>
    /// 解锁快捷键
    /// </summary>
    public string UnlockHotkeyString { get; set; } = "";

    /// <summary>
    /// 解锁快捷键
    /// </summary>
    [JsonIgnore]
    public Hotkey? UnlockHotkey => _hotkeyTools?.StringKeyToHotkey(UnlockHotkeyString);

    /// <summary>
    /// 启用密码框
    /// </summary>
    public bool EnablePasswordBox { get; set; } = true;

    /// <summary>
    /// 自动隐藏密码框
    /// </summary>
    public bool IsHidePasswordWindow { get; set; } = true;

    /// <summary>
    /// 密码框激活方式
    /// </summary>
    public PasswordBoxActiveMethodEnum PasswordBoxActiveMethod { get; set; } = PasswordBoxActiveMethodEnum.KeyboardDown | PasswordBoxActiveMethodEnum.MouseDown;

    /// <summary>
    /// 自动隐藏鼠标光标
    /// </summary>
    public bool IsHideMouseCursor { get; set; } = false;

    /// <summary>
    /// 屏幕解锁方式
    /// </summary>
    public ScreenUnlockMethods ScreenUnlockMethod { get; set; } = ScreenUnlockMethods.Hotkey;

    /// <summary>
    /// 锁屏状态展示
    /// </summary>
    public LockStatusDisplay LockStatusDisplay { get; set; } = LockStatusDisplay.None;

    /// <summary>
    /// 启用软件渲染
    /// </summary>
    public bool IsEnableSoftwareRendering { get; set; } = true;

    /// <summary>
    /// 锁定后执行关机/休眠（秒）
    /// </summary>
    public int AutoPowerSecond { get; set; } = 0;

    /// <summary>
    /// 锁定后执行关机/休眠（分钟）
    /// </summary>
    public int AutoPowerMinute => AutoPowerSecond / 60;

    /// <summary>
    /// 锁定后自动执行的电源操作类型
    /// </summary>
    public PowerActionType AutoPowerActionType { get; set; } = PowerActionType.Shutdown;

    /// <summary>
    /// 锁屏样式：0 完全透明 / 1 黑色遮罩 / 2 翻页时钟
    /// </summary>
    public LockScreenStyle LockStyle { get; set; } = LockScreenStyle.Clock;

    /// <summary>
    /// 黑色遮罩的透明度（0-100，越大越黑）
    /// </summary>
    public double LockMaskOpacity { get; set; } = 60;

    /// <summary>
    /// 翻页时钟（数字与牌面）的不透明度（0-100）
    /// </summary>
    public double ClockOpacity { get; set; } = 80;

    /// <summary>
    /// 时钟背景的不透明度（0-100），与时钟本身分开调
    /// </summary>
    public double ClockBackgroundOpacity { get; set; } = 100;

    /// <summary>
    /// 时钟字体预设 Id（见 ClockFonts）
    /// </summary>
    public string ClockFontId { get; set; } = ClockFonts.DefaultId;

    /// <summary>
    /// 用户导入的字体文件路径（ClockFontId 为 custom 时使用）
    /// </summary>
    public string ClockCustomFontPath { get; set; } = "";

    /// <summary>
    /// 用户导入字体的家族名
    /// </summary>
    public string ClockCustomFontName { get; set; } = "";

    /// <summary>
    /// 时钟垂直位置偏移，屏幕高度的百分比（-30 ~ 30）
    /// </summary>
    public double ClockOffsetY { get; set; }

    /// <summary>
    /// 牌面圆角（0-60）
    /// </summary>
    public double ClockCardRadius { get; set; } = 14;

    /// <summary>
    /// 牌面浓度：牌面底色的不透明度（0-100，0 只剩数字）
    /// </summary>
    public double ClockCardShade { get; set; } = 65;

    /// <summary>
    /// 是否显示牌面边框
    /// </summary>
    public bool ClockCardBorder { get; set; } = true;

    /// <summary>
    /// 是否显示牌面中缝
    /// </summary>
    public bool ClockSeamLine { get; set; } = true;

    /// <summary>
    /// 翻页速度百分比（50-200，越大越快）
    /// </summary>
    public double ClockFlipSpeed { get; set; } = 100;

    /// <summary>
    /// 动画帧率上限（15-60）。翻页、光条、背景漂移都受它约束，调低省 CPU，调高更顺
    /// </summary>
    public double AnimationFrameRate { get; set; } = 60;

    /// <summary>
    /// 时钟大小百分比（50-150）
    /// </summary>
    public double ClockScale { get; set; } = 100;

    /// <summary>
    /// 翻页时钟是否显示秒
    /// </summary>
    public bool ClockShowSeconds { get; set; }

    /// <summary>
    /// 时分秒各自两位数字之间的间距（像素，0-80）
    /// </summary>
    public double ClockCardGap { get; set; } = 12;

    /// <summary>
    /// 时 / 分 / 秒 三组之间的间距（像素，0-160）
    /// </summary>
    public double ClockGroupGap { get; set; } = 28;

    /// <summary>
    /// 时钟背景方式
    /// </summary>
    public ClockBackgroundMode ClockBackgroundMode { get; set; } = ClockBackgroundMode.Solid;

    /// <summary>
    /// 时钟背景色（单色模式使用，渐变模式为起始色）
    /// </summary>
    public string ClockBackgroundColor1 { get; set; } = "#000000";

    /// <summary>
    /// 时钟背景色（渐变模式为结束色）
    /// </summary>
    public string ClockBackgroundColor2 { get; set; } = "#2B2B45";

    /// <summary>
    /// 时钟渐变角度（0-360，0 为从左到右）
    /// </summary>
    public double ClockBackgroundAngle { get; set; } = 45;

    /// <summary>
    /// 锁屏状态提示（呼吸灯/圆点）的起始颜色，两个颜色相同时为单色
    /// </summary>
    public string BreathingColor1 { get; set; } = "#fb8c00";

    /// <summary>
    /// 锁屏状态提示（呼吸灯/圆点）的结束颜色
    /// </summary>
    public string BreathingColor2 { get; set; } = "#fb8c00";

    /// <summary>
    /// 时钟背景当前选中的预设方案 Id，空表示自定义
    /// </summary>
    public string ClockBackgroundPreset { get; set; } = "";

    /// <summary>
    /// 状态提示当前选中的预设方案 Id，空表示自定义
    /// </summary>
    public string BreathingPreset { get; set; } = "";

    /// <summary>
    /// 时钟背景渐变是否流动
    /// </summary>
    public bool ClockBackgroundAnimated { get; set; } = true;

    /// <summary>
    /// 流动速度百分比（50-300），100 表示预设自带速度
    /// </summary>
    public double ClockBackgroundSpeed { get; set; } = 100;

    /// <summary>
    /// 状态提示（顶部光条）渐变是否流动
    /// </summary>
    public bool BreathingAnimated { get; set; } = true;

    /// <summary>
    /// 状态提示流动速度百分比
    /// </summary>
    public double BreathingSpeed { get; set; } = 100;

    /// <summary>
    /// 是否已经把老配置迁移到「纯黑纯色背景」这套默认值。
    /// 老配置文件里没有这个字段，反序列化后是 false，正好用来做一次性迁移。
    /// </summary>
    public bool DefaultBackgroundApplied { get; set; }

    /// <summary>
    /// 是否已经把老配置的默认字体迁移到 Bebas Neue，作用同上
    /// </summary>
    public bool DefaultFontApplied { get; set; }
}
