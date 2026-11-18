using ComputerLock.Interfaces;
using JiuLing.TitleBarKit;

namespace ComputerLock.Shared;

public partial class MainLayout
{
    private bool _isDarkMode;
    private MudThemeProvider _mudThemeProvider = null!;
    private MudTheme _customTheme = null!;

    [Inject]
    private TitleBarService TitleBarService { get; set; } = null!;

    [Inject]
    private AppSettings AppSettings { get; set; } = null!;

    [Inject]
    private IStringLocalizer<Lang> Lang { get; set; } = null!;

    [Inject]
    private IGlobalLockService GlobalLockService { get; set; } = null!;

    [Inject]
    private ILogger Logger { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private ThemeSwitchService ThemeSwitchService { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private static bool _isInitialized = false;
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        _customTheme = new MudTheme()
        {
            LayoutProperties = new LayoutProperties
            {
                AppbarHeight = "36px",
                DrawerWidthLeft = "200px"
            },

            PaletteLight = new PaletteLight()
            {
                Primary = "#0078D4",
                Secondary = "#005FB8",
                // 半透明色：窗口底层是系统亚克力，页面只叠一层薄玻璃
                AppbarBackground = "#FAFAFCC7",
                AppbarText = "#0078D4",
                // 半透明底：桌面透过亚克力能看到，但保留足够对比度
                Background = "#FFFFFF61",
                Surface = "#FFFFFF52",
                LinesDefault = "#0000001A",

                Success = "#43A047",
                Error = "#E53935",
                Warning = "#FDD835",
                Info = "#29B6F6",
                TextPrimary = "#212121",
                TextSecondary = "#616161",
                DrawerBackground = "#FAFAFCBD",
                DrawerText = "#212121",
                ActionDefault = "#FFE2B0",
                HoverOpacity = 0.2
            },

            PaletteDark = new PaletteDark()
            {
                Primary = "#4CC2FF",
                Secondary = "#99EBFF",
                AppbarBackground = "#242428CC",
                AppbarText = "#4CC2FF",
                Background = "#14141857",
                Surface = "#FFFFFF12",
                LinesDefault = "#FFFFFF1A",

                Success = "#66BB6A",
                Error = "#EF5350",
                Warning = "#FFEE58",
                Info = "#4FC3F7",
                TextPrimary = "#FFFFFF",
                TextSecondary = "#B0BEC5",
                DrawerBackground = "#1E1E22C7",
                DrawerText = "#FFFFFF",
                ActionDefault = "#505050",
                HoverOpacity = 0.2
            }
        };

        await SwitchThemeAsync(AppSettings.AppTheme);

        InitializeEventBinding();
    }

    /// <summary>
    /// 首次渲染后 MudThemeProvider 才被 @ref 绑定，主题设成「跟随系统」时要在这里重新取一次，
    /// 否则拿不到真实的系统主题
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
        {
            return;
        }

        if (AppSettings.AppTheme == ThemeEnum.System && _mudThemeProvider != null)
        {
            await SwitchThemeAsync(ThemeEnum.System);
        }

        // 没有锁屏快捷键的话，除了托盘就没别的入口了，第一次启动引导一次
        if (string.IsNullOrWhiteSpace(AppSettings.LockHotkeyString) && !AppSettings.HotkeyGuideShown)
        {
            // 等界面完全就绪，否则对话框可能挂在半渲染的容器上
            await Task.Delay(400);
            await ShowHotkeyGuideAsync();
        }
    }

    /// <summary>
    /// 弹出「设置锁屏快捷键」引导
    /// </summary>
    private async Task ShowHotkeyGuideAsync()
    {
        var options = new DialogOptions
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<HotkeyGuide>("", options);
        await dialog.Result;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SwitchThemeAsync(ThemeEnum theme)
    {
        _isDarkMode = theme switch
        {
            // OnInitializedAsync 阶段 @ref 还没绑定，_mudThemeProvider 是 null，
            // 直接 await 会 NRE 导致启动闪退（默认设置就是「跟随系统」）。这时先读注册表。
            ThemeEnum.System => _mudThemeProvider != null
                ? await _mudThemeProvider.GetSystemDarkModeAsync()
                : IsSystemDarkMode(),
            ThemeEnum.Light => false,
            ThemeEnum.Dark => true,
            _ => _isDarkMode
        };
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// 读注册表判断系统是否为深色模式（不依赖 MudThemeProvider）
    /// </summary>
    private static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    private void InitializeEventBinding()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;

        ThemeSwitchService.OnThemeChanged += async theme =>
        {
            await SwitchThemeAsync(theme);
        };
    }

    private void MouseDown()
    {
        TitleBarService.DragHandler.DragMove();
    }

}