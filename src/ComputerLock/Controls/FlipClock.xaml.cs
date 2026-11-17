using System.Windows.Media;
using System.Windows.Threading;
using UserControl = System.Windows.Controls.UserControl;
using FontFamily = System.Windows.Media.FontFamily;
using Visibility = System.Windows.Visibility;

namespace ComputerLock.Controls;

/// <summary>
/// 翻页时钟：HH:mm，可选 HH:mm:ss，24 小时制
/// </summary>
public partial class FlipClock : UserControl
{
    /// <summary>
    /// 四张牌的总宽度
    /// </summary>
    private const double FourCardWidth = 852d;

    /// <summary>
    /// 六张牌的总宽度
    /// </summary>
    private const double SixCardWidth = 1292d;

    private readonly DispatcherTimer _timer = new();
    private string _lastTime = "";

    /// <summary>
    /// 是否显示秒
    /// </summary>
    public bool ShowSeconds { get; private set; }

    /// <summary>
    /// 宽高比，外层按它算缩放
    /// </summary>
    public double AspectRatio => Width / Height;

    public FlipClock()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 把所有牌的外观设置刷一遍（字体、圆角、牌面浓度、边框、中缝、翻页速度）
    /// </summary>
    public void ApplyStyle(FontFamily fontFamily, double cornerRadius, double faceShade, bool showBorder, bool showSeam, double flipSpeed, double frameRate = 60)
    {
        foreach (var card in AllCards)
        {
            card.ApplyStyle(fontFamily, cornerRadius, faceShade, showBorder, showSeam, flipSpeed, frameRate);
        }
    }

    private FlipCard[] AllCards =>
    [
        CardHourTens, CardHourOnes, CardMinuteTens, CardMinuteOnes, CardSecondTens, CardSecondOnes
    ];

    /// <summary>
    /// 切换是否显示秒
    /// </summary>
    public void SetShowSeconds(bool show)
    {
        ShowSeconds = show;

        var visibility = show ? Visibility.Visible : Visibility.Collapsed;
        CardSecondTens.Visibility = visibility;
        CardSecondOnes.Visibility = visibility;

        Width = show ? SixCardWidth : FourCardWidth;
        _lastTime = "";
    }

    /// <summary>
    /// 开始走时
    /// </summary>
    public void Start()
    {
        UpdateTime(animate: false);

        if (_timer.IsEnabled)
        {
            return;
        }

        // 对齐到下一个整秒再触发，一秒一次就够。
        // 之前显示秒时用 80ms 轮询，一秒会白醒十来次，屏幕越花这笔开销越明显。
        _timer.Interval = IntervalToNextSecond();
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    /// <summary>
    /// 停止走时（窗口关闭时调用，避免定时器泄漏）
    /// </summary>
    public void Stop()
    {
        if (!_timer.IsEnabled)
        {
            return;
        }
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateTime(animate: true);
        // 每次重新算到下一整秒的距离，长时间跑也不会累积漂移
        _timer.Interval = IntervalToNextSecond();
    }

    private static TimeSpan IntervalToNextSecond()
    {
        var now = DateTime.Now;
        // +12ms 余量，避免刚好卡在边界上漏掉这一秒
        var ms = 1000 - now.Millisecond + 12;
        return TimeSpan.FromMilliseconds(ms);
    }

    private void UpdateTime(bool animate)
    {
        var now = DateTime.Now;
        var text = ShowSeconds ? now.ToString("HHmmss") : now.ToString("HHmm");
        if (text == _lastTime)
        {
            return;
        }

        var first = _lastTime.Length == 0;
        _lastTime = text;

        FlipCard[] cards = ShowSeconds
            ? [CardHourTens, CardHourOnes, CardMinuteTens, CardMinuteOnes, CardSecondTens, CardSecondOnes]
            : [CardHourTens, CardHourOnes, CardMinuteTens, CardMinuteOnes];

        for (var i = 0; i < cards.Length && i < text.Length; i++)
        {
            var digit = text[i].ToString();
            if (first)
            {
                cards[i].SetDigitImmediate(digit);
            }
            else
            {
                // 每张牌各自判断，只有变了的才翻
                cards[i].SetDigit(digit, animate);
            }
        }
    }
}
