using System.IO;
using System.Windows;
using Brushes = System.Windows.Media.Brushes;
using ComputerLock.Interfaces;
using ComputerLock.Platforms;
using ComputerLock.Services;

namespace ComputerLock;

public partial class WindowMain : Window, IDisposable
{
    private readonly AppSettings _appSettings;
    private readonly IGlobalLockService _globalLockService;
    private readonly ILogger _logger;
    private readonly HotkeyHook _hotkeyHook;
    private readonly IStringLocalizer<Lang> _lang;
    private readonly IWindowsMessageBox _windowsMessageBox;
    private readonly ThemeSwitchService _themeSwitchService;

    private bool _acrylicEnabled;

    private readonly NotifyIcon _notifyIcon = new();
    private readonly ContextMenuStrip _contextMenuStrip = new();

    public WindowMain(AppSettings appSettings, IGlobalLockService globalLockService, ILogger logger, HotkeyHook hotkeyHook, IStringLocalizer<Lang> lang, IWindowsMessageBox windowsMessageBox, ThemeSwitchService themeSwitchService)
    {
        InitializeComponent();

        _themeSwitchService = themeSwitchService;

        _appSettings = appSettings;
        _globalLockService = globalLockService;
        _logger = logger;
        _hotkeyHook = hotkeyHook;
        _lang = lang;
        _windowsMessageBox = windowsMessageBox;

        InitializeNotifyIcon();

        EnableAcrylicBackground();

        _logger.Info("系统启动");

        if (_appSettings.LockHotkeyString.IsNotEmpty())
        {
            RegisterLockHotkey();
        }
        _hotkeyHook.HotkeyPressed += (id) =>
        {
            if (id == (int)HotkeyType.Lock)
            {
                if (!_globalLockService.IsLocked)
                {
                    _logger.Info("快捷键锁定");
                    _globalLockService.Lock();
                }
                else
                {
                    if (_appSettings.ScreenUnlockMethod == ScreenUnlockMethods.Hotkey && _appSettings.IsUnlockUseLockHotkey)
                    {
                        _logger.Info("快捷键解锁");
                        _globalLockService.Unlock();
                    }
                }
            }
            else if (id == (int)HotkeyType.Unlock)
            {
                if (_globalLockService.IsLocked)
                {
                    _logger.Info("快捷键解锁（独立解锁）");
                    _globalLockService.Unlock();
                }
            }
        };

        if (_appSettings.LockOnStartup)
        {
            _logger.Info("程序启动时锁定屏幕");
            _globalLockService.Lock();
        }

    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyWebViewTransparency();
        this.Title = Lang.Title;
        EnsureNormalSize();
        this.WindowState = _appSettings.IsHideWindowWhenLaunch ? WindowState.Minimized : WindowState.Normal;
    }

    /// <summary>
    /// 正常显示时的窗口尺寸
    /// </summary>
    private const double NormalWidth = 800d;
    private const double NormalHeight = 600d;

    /// <summary>
    /// 窗口被压缩成一条（标题栏文字会竖排、内容出现滚动条）时，撑回正常大小并居中。
    /// 这种尺寸异常通常来自系统恢复窗口时只给了最小跟踪尺寸，XAML 里的 Width/Height 管不到。
    /// </summary>
    private void EnsureNormalSize()
    {
        if (Width >= NormalWidth - 1 && Height >= NormalHeight - 1)
        {
            return;
        }

        _logger.Info($"主窗口尺寸异常（{Width:0}x{Height:0}），恢复为 {NormalWidth:0}x{NormalHeight:0}");
        Width = NormalWidth;
        Height = NormalHeight;
        Left = Math.Max(0, (SystemParameters.PrimaryScreenWidth - NormalWidth) / 2);
        Top = Math.Max(0, (SystemParameters.PrimaryScreenHeight - NormalHeight) / 2);
    }

    /// <summary>
    /// 开启 Windows 11 的亚克力背景。失败就保持原有不透明外观，不影响使用。
    /// </summary>
    private void EnableAcrylicBackground()
    {
        SourceInitialized += (_, _) =>
        {
            var isDark = ResolveIsDark();
            if (!AcrylicHelper.TryEnableAcrylic(this, isDark))
            {
                _logger.Info("亚克力背景 -> 不支持，使用普通背景");
                return;
            }

            _acrylicEnabled = true;
            Background = Brushes.Transparent;
            _logger.Info($"亚克力背景 -> 已启用（{(isDark ? "深色" : "浅色")}）");
            ApplyWebViewTransparency();

            _themeSwitchService.OnThemeChanged += theme =>
            {
                var dark = theme switch
                {
                    ThemeEnum.Dark => true,
                    ThemeEnum.Light => false,
                    _ => ResolveIsDark()
                };
                AcrylicHelper.TryEnableAcrylic(this, dark);
            };
        };
    }

    /// <summary>
    /// 让 WebView2 的背景透明，桌面/材质才能透过来
    /// </summary>
    private void ApplyWebViewTransparency()
    {
        if (!_acrylicEnabled)
        {
            return;
        }

        void Apply(object? sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializedEventArgs e)
        {
            e.WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
        }

        BlazorView.BlazorWebViewInitialized -= Apply;
        BlazorView.BlazorWebViewInitialized += Apply;

        if (BlazorView.WebView is not null)
        {
            BlazorView.WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
        }
    }

    private bool ResolveIsDark()
    {
        if (_appSettings.AppTheme == ThemeEnum.Dark)
        {
            return true;
        }
        if (_appSettings.AppTheme == ThemeEnum.Light)
        {
            return false;
        }
        return IsSystemDark();
    }

    private static bool IsSystemDark()
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

    private void InitializeNotifyIcon()
    {

        var btnShowWindow = new ToolStripMenuItem(Lang.ShowMainWindow);
        btnShowWindow.Click += (_, _) => ShowMainWindow();
        _contextMenuStrip.Items.Add(btnShowWindow);

        var btnLock = new ToolStripMenuItem(Lang.DoLock);
        btnLock.Click += (_, _) =>
        {
            _logger.Info("托盘锁定");
            _globalLockService.Lock();
        };
        _contextMenuStrip.Items.Add(btnLock);

        var btnClose = new ToolStripMenuItem(Lang.Exit);
        btnClose.Click += (_, _) =>
        {
            _logger.Info("托盘关闭");
            System.Windows.Application.Current.Shutdown();
        };
        _contextMenuStrip.Items.Add(btnClose);

        _notifyIcon.ContextMenuStrip = _contextMenuStrip;
        var iconUri = new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name};component/icon.ico");
        Stream iconStream = System.Windows.Application.GetResourceStream(iconUri).Stream;
        _notifyIcon.Icon = new Icon(iconStream);
        _notifyIcon.Text = Lang.Title;
        _notifyIcon.Click += (_, e) =>
        {
            var args = e as MouseEventArgs;
            if (args is not { Button: MouseButtons.Left })
            {
                return;
            }
            ShowMainWindow();
        };
        _notifyIcon.Visible = true;
    }

    private void ShowMainWindow()
    {
        EnsureNormalSize();
        this.ShowInTaskbar = true;
        this.WindowState = WindowState.Normal;
        this.Activate();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (this.WindowState == WindowState.Minimized)
        {
            this.ShowInTaskbar = false;
            return;
        }

        // 从最小化恢复的路径不止托盘菜单一个（点任务栏、Alt+Tab 都算），统一在这里兜住尺寸
        this.ShowInTaskbar = true;
        EnsureNormalSize();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_appSettings.IsHideWindowWhenClose)
        {
            this.WindowState = WindowState.Minimized;
            e.Cancel = true;
        }
    }
    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_appSettings.IsHideWindowWhenEsc)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
            }
        }
    }

    private void RegisterLockHotkey()
    {
        try
        {
            if (_appSettings.LockHotkey != null)
            {
                _logger.Info("注册锁屏热键");
                _hotkeyHook.Register((int)HotkeyType.Lock, _appSettings.LockHotkey);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"绑定锁屏热键失败", ex);
            _windowsMessageBox.Show($"{_lang["ExRegistFailed"]}{ex.Message}");
        }
    }
    public void Dispose()
    {
        _logger.Info("系统资源释放，系统关闭");
        _notifyIcon.Dispose();
        _globalLockService.Dispose();
    }
}