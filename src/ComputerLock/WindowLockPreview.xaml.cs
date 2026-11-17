using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ComputerLock;

/// <summary>
/// 锁屏预览窗口：全屏显示锁屏效果，但鼠标键盘全部穿透，不会真的锁屏。
/// 用来边调设置边看效果。
/// </summary>
public partial class WindowLockPreview : Window
{
    private readonly AppSettings _appSettings;
    private readonly ILogger _logger;

    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    public WindowLockPreview(AppSettings appSettings, ILogger logger)
    {
        InitializeComponent();

        _appSettings = appSettings;
        _logger = logger;

        // 只铺工作区，不盖任务栏：预览一旦置顶，任务栏和托盘就是唯一的逃生口。
        // 真正的锁屏窗口仍然是铺满整屏的，这里只是预览。
        var area = SystemParameters.WorkArea;
        Left = area.Left;
        Top = area.Top;
        Width = area.Width;
        Height = area.Height;

        Loaded += (_, _) => Surface.Apply(_appSettings, _logger);
    }

    /// <summary>
    /// 设置变更后刷新画面
    /// </summary>
    public void Apply(AppSettings appSettings)
    {
        Surface.Apply(appSettings, _logger);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            var style = GetWindowLong(handle, GwlExStyle);
            SetWindowLong(handle, GwlExStyle,
                style | WsExTransparent | WsExLayered | WsExNoActivate | WsExToolWindow);
        }
        catch
        {
            // 拿不到窗口句柄就退化成普通覆盖窗口，不影响主流程
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        Surface.StopClock();
        base.OnClosed(e);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int index, int newStyle);
}
