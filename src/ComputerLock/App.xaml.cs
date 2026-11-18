using ComputerLock.Interfaces;
using JiuLing.TitleBarKit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudExtensions.Services;
using System.Globalization;
using System.IO;
using System.Windows;
using Application = System.Windows.Application;

namespace ComputerLock;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex _mutex = default!;
    private WindowMain? _mainWindow;
    private ILogger _logger;

    public App()
    {
        // 闪退时如果连日志都没留下，就完全没法查。
        // 这三个钩子挂在 DI 之前，任何未捕获异常都会落到 crash.log。
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            WriteCrash(e.ExceptionObject as Exception, "AppDomain");
        DispatcherUnhandledException += (_, e) =>
            WriteCrash(e.Exception, "Dispatcher");
        TaskScheduler.UnobservedTaskException += (_, e) =>
            WriteCrash(e.Exception, "TaskScheduler");
    }

    /// <summary>
    /// 把未捕获异常追加写到配置目录下的 crash.log
    /// </summary>
    private static void WriteCrash(Exception? exception, string source)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppBase.FriendlyName);
            Directory.CreateDirectory(directory);

            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {source}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(directory, "crash.log"), text);
        }
        catch
        {
            // 记录失败就算了，不能在崩溃处理里再抛一次
        }
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, AppBase.FriendlyName);
        if (!_mutex.WaitOne(0, false))
        {
            System.Windows.MessageBox.Show("程序已经运行 The program has been run");
            Application.Current.Shutdown();
            return;
        }

        Environment.CurrentDirectory = Path.GetDirectoryName(AppBase.ExecutablePath);

        Init();
    }

    private void Init()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<AppSettingsProvider>();

        services.AddSingleton<AppSettings>((sp) =>
        {
            var appSettings = sp.GetRequiredService<AppSettingsProvider>().LoadSettings();
            var hotkeyTools = sp.GetRequiredService<HotkeyTools>();
            appSettings.Initialize(hotkeyTools);
            return appSettings;
        });
        services.AddSingleton(LogManager.GetLogger());
        services.AddSingleton<HotkeyHook>();
        services.AddSingleton<MouseHook>();
        services.AddSingleton<AutostartHook>();
        services.AddSingleton<TaskManagerHook>();
        services.AddSingleton<UserActivityMonitor>();
        services.AddSingleton<PopupService>();
        services.AddSingleton<WindowMain>();
        services.AddTransient<WindowLockScreen>();
        services.AddTransient<WindowBlankScreen>();
        services.AddSingleton<SystemKeyHook>();
        services.AddSingleton<HotkeyTools>();
        services.AddSingleton<ThemeSwitchService>();
        services.AddSingleton<LockPreviewService>();
        services.AddSingleton<PowerManager>();
        services.AddSingleton<IWindowsMessageBox, WindowsMessageBox>();
        services.AddSingleton<IGlobalLockService, GlobalLockService>();
        services.AddKeyedSingleton<IScreenLockService, PasswordScreenLockService>(ScreenUnlockMethods.Password);
        services.AddKeyedSingleton<IScreenLockService, HotkeyScreenLockService>(ScreenUnlockMethods.Hotkey);

        services.AddWpfTitleBarKit();
        services.AddLocalization();
        services.AddWpfBlazorWebView();
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopLeft;
            config.SnackbarConfiguration.ShowCloseIcon = false;
            config.SnackbarConfiguration.VisibleStateDuration = 1500;
            config.SnackbarConfiguration.ShowTransitionDuration = 200;
            config.SnackbarConfiguration.HideTransitionDuration = 400;
        });
        services.AddMudExtensions();
        services.AddMudMarkdownServices();

        var sp = services.BuildServiceProvider();
        Resources.Add("services", sp);

        // 自动启动迁移：在应用启动时执行一次注册表 -> 计划任务的迁移
        sp.GetRequiredService<AutostartHook>().MigrateRegistryToTaskIfNeeded();

        var cultureInfo = new CultureInfo(sp.GetRequiredService<AppSettings>().Lang.ToString());
        CultureInfo.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;

        if (sp.GetRequiredService<AppSettings>().IsEnableSoftwareRendering)
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        }

        _logger = sp.GetRequiredService<ILogger>();
        // 确保 HotkeyHook 已被创建
        sp.GetRequiredService<HotkeyHook>();
        KeepMessageLive();

        _mainWindow = sp.GetRequiredService<WindowMain>();
        Application.Current.MainWindow = _mainWindow;
        _mainWindow.Show();
    }

    private void KeepMessageLive()
    {
        // 启动后台消息循环，防止主线程被挂起
        Task.Run(() =>
        {
            try
            {
                while (WinApi.GetMessage(out var msg, IntPtr.Zero, 0, 0))
                {
                    WinApi.TranslateMessage(ref msg);
                    WinApi.DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("热键消息保活服务异常", ex);
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainWindow?.Dispose();
        base.OnExit(e);
    }
}