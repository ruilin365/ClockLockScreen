using System.Windows;
using Application = System.Windows.Application;

namespace ComputerLock.Services;

/// <summary>
/// 锁屏预览：开一个全屏、鼠标穿透的窗口显示锁屏效果，不会真的锁屏。
/// 同时把设置窗口置顶，方便一边调一边看。
/// </summary>
public class LockPreviewService
{
    private WindowLockPreview? _window;
    private Window? _mainWindow;

    /// <summary>
    /// 预览是否正在运行
    /// </summary>
    public bool IsRunning => _window != null;

    /// <summary>
    /// 预览被外部关掉（例如设置窗口最小化）时通知界面，免得开关还停在打开状态
    /// </summary>
    public event Action? Stopped;

    public void Start(AppSettings appSettings, ILogger logger)
    {
        if (_window != null)
        {
            _window.Apply(appSettings);
            return;
        }

        _window = new WindowLockPreview(appSettings, logger);
        _window.Closed += (_, _) =>
        {
            _window = null;
            RestoreMainWindow();
        };
        _window.Show();

        // 设置窗口压在上面，预览在下面透出来
        if (Application.Current.MainWindow is { } main)
        {
            _mainWindow = main;
            main.Topmost = true;
            main.Activate();

            // 设置窗口一旦最小化/隐藏，就把预览关掉。
            // 否则那层置顶的预览会挡在托盘和任务栏前面，用户既看不见设置也点不到托盘，只能强杀进程。
            main.StateChanged -= MainWindowStateChanged;
            main.StateChanged += MainWindowStateChanged;
            main.Closed -= MainWindowClosed;
            main.Closed += MainWindowClosed;
        }

        logger.Info("锁屏预览 -> 已开启");
    }

    public void Stop()
    {
        if (_window == null)
        {
            RestoreMainWindow();
            return;
        }

        var window = _window;
        _window = null;
        window.Close();
        RestoreMainWindow();
        Platforms.MemoryTrimmer.TrimAsync();
        Stopped?.Invoke();
    }

    /// <summary>
    /// 设置变更后刷新预览
    /// </summary>
    public void Refresh(AppSettings appSettings)
    {
        _window?.Apply(appSettings);
    }

    private void MainWindowStateChanged(object? sender, EventArgs e)
    {
        if (sender is Window window && window.WindowState == WindowState.Minimized)
        {
            Stop();
        }
    }

    private void MainWindowClosed(object? sender, EventArgs e) => Stop();

    private void RestoreMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.StateChanged -= MainWindowStateChanged;
            _mainWindow.Closed -= MainWindowClosed;
            _mainWindow = null;
        }

        if (Application.Current?.MainWindow is { } main)
        {
            main.Topmost = false;
        }
    }
}
