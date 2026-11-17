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

    private async Task SwitchThemeAsync(ThemeEnum theme)
    {
        _isDarkMode = theme switch
        {
            ThemeEnum.System => await _mudThemeProvider.GetSystemDarkModeAsync(),
            ThemeEnum.Light => false,
            ThemeEnum.Dark => true,
            _ => _isDarkMode
        };
        await InvokeAsync(StateHasChanged);
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