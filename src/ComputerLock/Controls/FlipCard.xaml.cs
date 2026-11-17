using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using UserControl = System.Windows.Controls.UserControl;
using FontFamily = System.Windows.Media.FontFamily;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace ComputerLock.Controls;

/// <summary>
/// 单张翻页牌：显示一个数字，切换时上半翻落、下半翻起。
/// <para>
/// 四层结构：
/// ① 静态上半 —— 新值，按翻页面高度裁剪，只露出「已经翻过去」的那部分；
/// ② 静态下半 —— 旧值，翻起过程中按翻页面高度裁剪，只露出「还没被盖住」的那部分；
/// ③ 上翻页层 —— 旧值上半，铰链在底边，1→0 压扁（模拟向下翻落）；
/// ④ 下翻页层 —— 新值下半，铰链在顶边，0→1 展开（模拟从背面翻起）。
/// </para>
/// <para>
/// 靠裁剪而不是靠不透明底板遮挡，两个数字在空间上互不相交，
/// 所以既不会叠影，也能保住牌面的半透明质感。
/// </para>
/// </summary>
public partial class FlipCard : UserControl
{
    private const double CardWidth = 200d;
    private const double HalfHeight = 140d;

    /// <summary>
    /// 数字字形的目标尺寸：高度撑满、宽度不超，具体字号按字体实测字形算
    /// </summary>
    private const double GlyphTargetHeight = 172d;
    private const double GlyphTargetWidth = 140d;
    private const double BaseFontSize = 250d;

    /// <summary>
    /// 翻页动画的帧率上限，由设置里的「动画帧率」决定
    /// </summary>
    private int _frameRate = 60;

    private static readonly Rect FullTop = new(0, 0, CardWidth, HalfHeight);
    private static readonly Rect EmptyTop = new(0, 0, CardWidth, 0);
    /// <summary>下半裁剪区收到中缝处（高度为 0，但锚在下方），这样它是从上方被"吃掉"的</summary>
    private static readonly Rect EmptyBottom = new(0, HalfHeight, CardWidth, 0);

    /// <summary>
    /// 动画时长倍率，仅用于离屏渲染调试（环境变量 COMPUTERLOCK_FLIP_TIMESCALE 放大后便于抓中间帧），正常运行为 1
    /// </summary>
    private static readonly double TimeScale = ReadTimeScale();

    private readonly RectangleGeometry _revealClip;
    private readonly RectangleGeometry _riseClip;

    private string _current = "";
    private bool _aligned;
    private bool _animating;
    private int _generation;

    private double _cornerRadius = 14;
    private double _flipSpeed = 1d;

    public FlipCard()
    {
        InitializeComponent();

        _revealClip = new RectangleGeometry(FullTop);
        TopPanel.Clip = _revealClip;

        _riseClip = new RectangleGeometry(FullTop);
        BottomPanel.Clip = _riseClip;
    }

    /// <summary>
    /// 给动画挂上帧率上限（WPF 的 DesiredFrameRate 是附加属性，只能用静态方法设）
    /// </summary>
    private T LimitFps<T>(T timeline) where T : Timeline
    {
        Timeline.SetDesiredFrameRate(timeline, _frameRate);
        return timeline;
    }

    private static double ReadTimeScale()
    {
        var raw = Environment.GetEnvironmentVariable("COMPUTERLOCK_FLIP_TIMESCALE");
        return double.TryParse(raw, out var value) && value > 0 ? value : 1d;
    }

    private TimeSpan FallTime => TimeSpan.FromMilliseconds(170 * TimeScale / _flipSpeed);
    private TimeSpan RiseTime => TimeSpan.FromMilliseconds(200 * TimeScale / _flipSpeed);

    private TextBlock[] Digits => [TopText, BottomText, FlipText, FlipBottomText];

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        AlignDigits();
    }

    /// <summary>
    /// 应用外观设置：字体、牌面圆角、牌面浓度、边框、中缝、翻页速度
    /// </summary>
    public void ApplyStyle(FontFamily fontFamily, double cornerRadius, double faceShade, bool showBorder, bool showSeam, double flipSpeed, double frameRate = 60)
    {
        _cornerRadius = Math.Clamp(cornerRadius, 0, 100);
        _flipSpeed = Math.Clamp(flipSpeed, 25, 300) / 100d;
        _frameRate = (int)Math.Round(Math.Clamp(frameRate, 10, 240));

        var alpha = (byte)Math.Round(Math.Clamp(faceShade, 0, 100) * 255d / 100d);
        var radius = new CornerRadius(_cornerRadius);

        FaceFill.CornerRadius = radius;
        FaceFill.Background = new SolidColorBrush(Color.FromArgb(alpha, 0x14, 0x14, 0x18));
        FaceBorder.CornerRadius = radius;
        FaceBorder.Visibility = showBorder ? Visibility.Visible : Visibility.Collapsed;
        SeamLine.Visibility = showSeam ? Visibility.Visible : Visibility.Collapsed;

        foreach (var digit in Digits)
        {
            digit.FontFamily = fontFamily;
        }

        // 字体换了，字面尺寸和垂直位置都要重算
        _aligned = false;
        AlignDigits();
    }

    /// <summary>
    /// 按字形包围盒计算字号与垂直位置：字形中心正好落在牌的中缝上，上下两块用同一基准所以必然拼合。
    /// </summary>
    private void AlignDigits()
    {
        if (_aligned)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new System.Windows.Media.Typeface(TopText.FontFamily, TopText.FontStyle, TopText.FontWeight, TopText.FontStretch);

        // 不同字体的字面大小差很多（Bebas 高瘦、Orbitron 又方又宽），
        // 先用 "8"（最宽的数字）量出字形尺寸，算一个"高度撑满、宽度不超"的字号
        var probe = new FormattedText("8", CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight,
            typeface, BaseFontSize, System.Windows.Media.Brushes.Black, dpi);
        var ink = probe.BuildGeometry(new System.Windows.Point(0, 0)).Bounds;
        var fontSize = BaseFontSize;
        if (ink.Width > 0.5 && ink.Height > 0.5)
        {
            var scale = Math.Min(GlyphTargetHeight / ink.Height, GlyphTargetWidth / ink.Width);
            fontSize = Math.Clamp(BaseFontSize * scale, 60d, 400d);
        }

        foreach (var digit in Digits)
        {
            digit.FontSize = fontSize;
        }

        var formatted = new FormattedText("0", CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight,
            typeface, fontSize, System.Windows.Media.Brushes.Black, dpi);
        var bounds = formatted.BuildGeometry(new System.Windows.Point(0, 0)).Bounds;
        var glyphCenter = bounds.Top + bounds.Height / 2d;
        var top = HalfHeight - glyphCenter;

        // 上半、上翻页层都位于 y=0，直接用 top；下半、下翻页层整体下移半块，要减去 HalfHeight
        Canvas.SetTop(TopText, top);
        Canvas.SetTop(BottomText, top - HalfHeight);
        Canvas.SetTop(FlipText, top);
        Canvas.SetTop(FlipBottomText, top - HalfHeight);

        _aligned = true;
    }

    /// <summary>
    /// 设置牌面数字
    /// </summary>
    /// <param name="value">要显示的字符</param>
    /// <param name="animate">是否播放翻页动画</param>
    public void SetDigit(string value, bool animate)
    {
        AlignDigits();

        if (_current == value)
        {
            return;
        }

        var old = _current;
        _current = value;

        // 首次赋值或明确要求不动画时，直接落位
        if (!animate || old.Length == 0)
        {
            SnapTo(value);
            return;
        }

        // 上一次翻页还没播完就来新值：先把上一次落到终态，再从终态继续翻
        if (_animating)
        {
            SnapTo(old);
        }

        var generation = ++_generation;
        _animating = true;

        // 起始态：静态上=新值（暂被裁掉），静态下=旧值，上翻页=旧值，下翻页=新值
        TopText.Text = value;
        BottomText.Text = old;
        FlipText.Text = old;
        FlipBottomText.Text = value;

        FlipPanel.Visibility = Visibility.Visible;
        FlipPanelBottom.Visibility = Visibility.Collapsed;
        FlipScale.ScaleY = 1;
        FlipScale.ScaleX = 1;
        FlipScaleBottom.ScaleY = 0;
        FlipScaleBottom.ScaleX = 1;
        FlipShade.Opacity = 0;

        // 新值上半一开始完全被翻页面挡住
        _revealClip.BeginAnimation(RectangleGeometry.RectProperty, null);
        _revealClip.Rect = EmptyTop;

        var easeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn };

        var fall = LimitFps(new DoubleAnimation(1, 0, FallTime) { EasingFunction = easeIn });
        // 翻落时轻微横向拉伸，模拟透视
        var fallWiden = LimitFps(new DoubleAnimation(1, 1.06, FallTime) { EasingFunction = easeIn });
        var fallShade = LimitFps(new DoubleAnimation(0, 1, FallTime) { EasingFunction = easeIn });

        // 裁剪区跟着翻页面上沿一起下移：翻页面压扁多少，新值就露出多少
        var reveal = LimitFps(new RectAnimation(EmptyTop, FullTop, FallTime)
        {
            EasingFunction = easeIn,
            FillBehavior = FillBehavior.HoldEnd
        });

        fall.Completed += (_, _) =>
        {
            if (generation != _generation)
            {
                return;
            }

            // 上半已经翻落：撤掉翻页面，新值上半完整露出
            FlipPanel.Visibility = Visibility.Collapsed;
            FlipScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            FlipScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            FlipShade.BeginAnimation(OpacityProperty, null);
            FlipScale.ScaleY = 1;
            FlipScale.ScaleX = 1;
            FlipShade.Opacity = 0;

            // 接着把下半的新值翻上来：静态下半只保留翻页面以下的部分
            FlipPanelBottom.Visibility = Visibility.Visible;
            FlipScaleBottom.ScaleY = 0;
            FlipScaleBottom.ScaleX = 1.06;
            FlipShadeBottom.Opacity = 1;

            _riseClip.BeginAnimation(RectangleGeometry.RectProperty, null);
            _riseClip.Rect = FullTop;

            var easeOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            var rise = LimitFps(new DoubleAnimation(0, 1, RiseTime) { EasingFunction = easeOut });
            var riseWiden = LimitFps(new DoubleAnimation(1.06, 1, RiseTime) { EasingFunction = easeOut });
            var riseShade = LimitFps(new DoubleAnimation(1, 0, RiseTime) { EasingFunction = easeOut });
            var hide = LimitFps(new RectAnimation(FullTop, EmptyBottom, RiseTime)
            {
                EasingFunction = easeOut,
                FillBehavior = FillBehavior.HoldEnd
            });

            rise.Completed += (_, _) =>
            {
                if (generation != _generation)
                {
                    return;
                }

                // 下半静态层换成新值，否则会停在上新下旧的错位状态
                SnapTo(value);
            };

            FlipScaleBottom.BeginAnimation(ScaleTransform.ScaleYProperty, rise);
            FlipScaleBottom.BeginAnimation(ScaleTransform.ScaleXProperty, riseWiden);
            FlipShadeBottom.BeginAnimation(OpacityProperty, riseShade);
            _riseClip.BeginAnimation(RectangleGeometry.RectProperty, hide);
        };

        FlipScale.BeginAnimation(ScaleTransform.ScaleYProperty, fall);
        FlipScale.BeginAnimation(ScaleTransform.ScaleXProperty, fallWiden);
        FlipShade.BeginAnimation(OpacityProperty, fallShade);
        _revealClip.BeginAnimation(RectangleGeometry.RectProperty, reveal);
    }

    /// <summary>
    /// 立即显示（不播放动画）
    /// </summary>
    public void SetDigitImmediate(string value)
    {
        AlignDigits();
        _current = value;
        SnapTo(value);
    }

    /// <summary>
    /// 直接落到某个值的静止态：清掉所有动画与翻页层
    /// </summary>
    private void SnapTo(string value)
    {
        _generation++;
        _animating = false;

        FlipScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        FlipScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        FlipShade.BeginAnimation(OpacityProperty, null);
        FlipScaleBottom.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        FlipScaleBottom.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        FlipShadeBottom.BeginAnimation(OpacityProperty, null);
        _revealClip.BeginAnimation(RectangleGeometry.RectProperty, null);
        _riseClip.BeginAnimation(RectangleGeometry.RectProperty, null);

        TopText.Text = value;
        BottomText.Text = value;
        FlipText.Text = value;
        FlipBottomText.Text = value;

        FlipPanel.Visibility = Visibility.Collapsed;
        FlipPanelBottom.Visibility = Visibility.Collapsed;
        FlipScale.ScaleY = 1;
        FlipScale.ScaleX = 1;
        FlipScaleBottom.ScaleY = 0;
        FlipScaleBottom.ScaleX = 1;
        FlipShade.Opacity = 0;
        FlipShadeBottom.Opacity = 0;
        _revealClip.Rect = FullTop;
        _riseClip.Rect = FullTop;
    }
}
