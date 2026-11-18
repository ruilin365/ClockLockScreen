using System.Windows;
using System.Windows.Input;

namespace ComputerLock
{
    /// <summary>
    /// WindowBlankScreen.xaml 的交互逻辑
    /// </summary>
    public partial class WindowBlankScreen : Window
    {
        private bool _isUnlock = false;
        public event EventHandler<EventArgs>? OnDeviceInput;

        private readonly AppSettings _appSettings;
        private readonly ILogger _logger;
        public WindowBlankScreen(AppSettings appSettings, ILogger logger)
        {
            InitializeComponent();
            _appSettings = appSettings;
            _logger = logger;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _logger.Info("空白屏幕 -> 准备锁定");

            // 实时模糊要截「自己背后的画面」，不把自己排除掉就会越截越糊
            if (!Platforms.DesktopSnapshot.ExcludeFromCapture(
                    new System.Windows.Interop.WindowInteropHelper(this).Handle))
            {
                _logger.Info("空白屏幕 -> 排除截屏标记失败，实时模糊可能失效");
            }
            if (_appSettings.LockStatusDisplay.HasFlag(LockStatusDisplay.BreathingTop))
            {
                _logger.Info("空白屏幕 -> 启用顶部呼吸灯");
                BreathingLightHelper.InitializeBreathingLight(TopBreathingLight, _appSettings, flow: true);
            }
            if (_appSettings.LockStatusDisplay.HasFlag(LockStatusDisplay.DotTopLeft))
            {
                _logger.Info("空白屏幕 -> 启用左上角圆点");
                BreathingLightHelper.InitializeBreathingLight(DotTopLeft, _appSettings);
            }
            if (_appSettings.LockStatusDisplay.HasFlag(LockStatusDisplay.DotTopRight))
            {
                _logger.Info("空白屏幕 -> 启用右上角圆点");
                BreathingLightHelper.InitializeBreathingLight(DotTopRight, _appSettings);
            }

            // 锁屏画面（黑色遮罩 / 翻页时钟）由共用控件负责
            Surface.Apply(_appSettings, _logger);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            _logger.Info($"空白屏幕 -> 检测到按键 {e.Key}");
            if (e.Key != Key.Escape)
            {
                return;
            }
            _logger.Info("空白屏幕 -> 按下ESC功能键");
            if (_appSettings.EnablePasswordBox)
            {
                _logger.Info("空白屏幕 -> 密码框显示已启用");
                if ((_appSettings.PasswordBoxActiveMethod & PasswordBoxActiveMethodEnum.KeyboardDown) != PasswordBoxActiveMethodEnum.KeyboardDown)
                {
                    return;
                }
            }
            _logger.Info("空白屏幕 -> 通知设备输入");
            OnDeviceInput?.Invoke(this, EventArgs.Empty);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _logger.Info($"空白屏幕 -> 准备关闭，当前解锁状态：{_isUnlock}");
            if (!_isUnlock)
            {
                e.Cancel = true;
                return;
            }
            Surface.StopClock();
        }
        public void Unlock()
        {
            _logger.Info($"空白屏幕 -> 解锁");
            _isUnlock = true;
        }
    }
}
