using System.Windows;
using System.Windows.Media.Animation;
using ComputerLock.Models;
using UserControl = System.Windows.Controls.UserControl;
using Canvas = System.Windows.Controls.Canvas;
using Ellipse = System.Windows.Shapes.Ellipse;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Brushes = System.Windows.Media.Brushes;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using RadialGradientBrush = System.Windows.Media.RadialGradientBrush;
using GradientStop = System.Windows.Media.GradientStop;
using TranslateTransform = System.Windows.Media.TranslateTransform;
using Point = System.Windows.Point;
using SineEase = System.Windows.Media.Animation.SineEase;

namespace ComputerLock.Controls;

/// <summary>
/// 锁屏画面层：黑色遮罩 / 翻页时钟。
/// WindowLockScreen（密码解锁）和 WindowBlankScreen（快捷键解锁）共用同一份实现。
/// </summary>
public partial class LockSurface : UserControl
{
    /// <summary>
    /// 自定义两色时的默认漂移周期（秒）
    /// </summary>
    private const double CustomFlowPeriod = 36d;

    /// <summary>
    /// 色块数量
    /// </summary>
    private const int BlobCount = 7;

    /// <summary>
    /// 背景色块漂移的帧率上限（比钟面再低一档，漂得很慢，看不出来）
    /// </summary>
    private int _driftFrameRate = 20;

    private double _clockBaseOpacity = 1d;
    private bool _isClockOn;

    private AppSettings? _meshSettings;
    private double _clockScale = 100;
    private double _clockOffsetY;
    private double _meshPeriod;

    public LockSurface()
    {
        InitializeComponent();
        SizeChanged += (_, _) =>
        {
            RebuildMesh();
            AdjustClockSize();
        };
    }

    /// <summary>
    /// 按设置切换锁屏画面
    /// </summary>
    public void Apply(AppSettings settings, ILogger? logger = null)
    {
        MaskLayer.Visibility = Visibility.Collapsed;
        ClockLayer.Visibility = Visibility.Collapsed;
        ClockBackground.Visibility = Visibility.Collapsed;
        ClockHost.Visibility = Visibility.Collapsed;
        ClearMesh();
        LockClock.Stop();
        _isClockOn = false;

        switch (settings.LockStyle)
        {
            case LockScreenStyle.BlackMask:
                var maskOpacity = Math.Clamp(settings.LockMaskOpacity, 0, 100) / 100d;
                logger?.Info($"锁屏画面 -> 黑色遮罩，不透明度：{maskOpacity:P0}");
                MaskLayer.Opacity = maskOpacity;
                MaskLayer.Visibility = Visibility.Visible;
                break;

            case LockScreenStyle.Clock:
                var clockOpacity = Math.Clamp(settings.ClockOpacity, 0, 100) / 100d;
                var backgroundOpacity = Math.Clamp(settings.ClockBackgroundOpacity, 0, 100) / 100d;
                logger?.Info($"锁屏画面 -> 翻页时钟，时钟不透明度：{clockOpacity:P0}，背景不透明度：{backgroundOpacity:P0}，背景：{settings.ClockBackgroundMode}，预设：{settings.ClockBackgroundPreset}");
                LockClock.SetShowSeconds(settings.ClockShowSeconds);
                _clockScale = settings.ClockScale;
                _clockOffsetY = settings.ClockOffsetY;

                // 帧率要在建背景之前算好：色块漂移的动画是在 ApplyClockBackground 里起的
                var frameRate = Math.Clamp(settings.AnimationFrameRate, 10, 240);
                // 背景色块比钟面再低一档：它漂得很慢，帧率低完全看不出来
                _driftFrameRate = Math.Max(10, (int)Math.Round(frameRate * 0.8));
                ApplyClockBackground(settings, logger);

                // 整层不做透明度（解锁时的压暗动画用它），数字与背景各自调
                ClockLayer.BeginAnimation(OpacityProperty, null);
                ClockLayer.Opacity = 1;
                ClockHost.Opacity = clockOpacity;
                ClockBackground.Opacity = backgroundOpacity;
                ClockLayer.Visibility = Visibility.Visible;
                ClockHost.Visibility = Visibility.Visible;

                var font = ClockFonts.Resolve(settings.ClockFontId, settings.ClockCustomFontPath,
                    settings.ClockCustomFontName, out var fontName);
                logger?.Info($"时钟外观 -> 字体：{fontName}，圆角：{settings.ClockCardRadius}，牌面浓度：{settings.ClockCardShade}%，翻页速度：{settings.ClockFlipSpeed}%，帧率：{frameRate}fps");
                LockClock.ApplyStyle(font, settings.ClockCardRadius, settings.ClockCardShade,
                    settings.ClockCardBorder, settings.ClockSeamLine, settings.ClockFlipSpeed, frameRate);

                _isClockOn = true;
                LockClock.Start();
                AdjustClockSize();
                break;

            default:
                logger?.Info("锁屏画面 -> 完全透明");
                break;
        }
    }

    /// <summary>
    /// 密码框出现时把时钟退到背景
    /// </summary>
    public void SetClockDim(bool dim)
    {
        if (!_isClockOn || ClockLayer.Visibility != Visibility.Visible)
        {
            return;
        }

        var target = dim ? _clockBaseOpacity * 0.22 : _clockBaseOpacity;
        var animation = new DoubleAnimation(target, TimeSpan.FromMilliseconds(180))
        {
            FillBehavior = FillBehavior.HoldEnd
        };
        ClockLayer.BeginAnimation(OpacityProperty, animation);
    }

    public void StopClock()
    {
        ClearMesh();
        LockClock.Stop();
    }

    /// <summary>
    /// 时钟背景：不显示 / 单色 / 网格渐变
    /// </summary>
    private void ApplyClockBackground(AppSettings settings, ILogger? logger)
    {
        switch (settings.ClockBackgroundMode)
        {
            case ClockBackgroundMode.Solid:
                var presetSolid = ColorPresets.Find(ColorPresets.Background, settings.ClockBackgroundPreset);
                var solidStops = GradientFactory.GetStops(presetSolid, settings.ClockBackgroundColor1, settings.ClockBackgroundColor2);
                ClockBackground.Background = new SolidColorBrush(solidStops[0].Color);
                ClockBackground.Visibility = Visibility.Visible;
                MeshCanvas.Visibility = Visibility.Collapsed;
                break;

            case ClockBackgroundMode.Gradient:
                ClockBackground.Visibility = Visibility.Visible;
                MeshCanvas.Visibility = Visibility.Visible;
                _meshSettings = settings;
                RebuildMesh();
                logger?.Info($"锁屏画面 -> 网格渐变，色块 {BlobCount} 个，漂移周期 {_meshPeriod:0.0}s");
                break;

            default:
                ClockBackground.Background = Brushes.Transparent;
                ClockBackground.Visibility = Visibility.Collapsed;
                break;
        }
    }

    /// <summary>
    /// 用若干径向色块叠出背景。径向渐变本身从中心向外淡出，
    /// 色块之间没有边界，也就不存在线性渐变那种「到头断一下」的接缝。
    /// </summary>
    private void RebuildMesh()
    {
        if (_meshSettings == null || ClockBackground.Visibility != Visibility.Visible)
        {
            return;
        }

        var width = ActualWidth > 0 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
        var height = ActualHeight > 0 ? ActualHeight : SystemParameters.PrimaryScreenHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        MeshCanvas.Children.Clear();

        var settings = _meshSettings;
        var preset = ColorPresets.Find(ColorPresets.Background, settings.ClockBackgroundPreset);
        var stops = GradientFactory.GetStops(preset, settings.ClockBackgroundColor1, settings.ClockBackgroundColor2)
            .Select(s => (Offset: s.Offset, Color: GradientFactory.Saturate(s.Color, 1.38)))
            .ToArray();

        // 底色取调色板里最深的一档再压暗，色块淡出后落在同色系深色上，既有纵深也不会出色差台阶
        var deepest = stops.OrderBy(s => s.Color.R + s.Color.G + s.Color.B).First().Color;
        var baseColor = GradientFactory.Scale(deepest, 0.52);
        ClockBackground.Background = new SolidColorBrush(baseColor);

        var animated = settings.ClockBackgroundAnimated;
        var basePeriod = preset?.PeriodSeconds > 0 ? preset.PeriodSeconds : CustomFlowPeriod;
        _meshPeriod = animated ? GradientFactory.ToPeriod(basePeriod, settings.ClockBackgroundSpeed) : 0;

        // 固定种子：同一套配置每次重建的构图一致，不会闪一下换个样子
        var random = new Random(StableSeed(
            settings.ClockBackgroundPreset + settings.ClockBackgroundColor1 + settings.ClockBackgroundColor2));

        // 色块按网格铺开，保证每种颜色都能占到画面，不会被随机位置挤到屏幕外
        (double X, double Y)[] anchors =
        [
            (0.06, 0.12), (0.50, -0.04), (0.94, 0.18),
            (-0.02, 0.62), (0.42, 0.52), (0.92, 0.66), (0.28, 0.96)
        ];

        var radius = Math.Max(width, height) * 1.15;

        for (var i = 0; i < BlobCount; i++)
        {
            var t = BlobCount == 1 ? 0d : i / (double)(BlobCount - 1);
            var color = GradientFactory.SampleAt(stops, t);

            var size = radius * (0.62 + random.NextDouble() * 0.55);
            var blob = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = CreateBlobBrush(color),
                IsHitTestVisible = false
            };

            // 锚点 + 小抖动：构图稳定但不会呆板，也允许略微超出画面避免边缘空档
            var anchor = anchors[i % anchors.Length];
            var jitterX = (random.NextDouble() - 0.5) * 0.18;
            var jitterY = (random.NextDouble() - 0.5) * 0.18;
            Canvas.SetLeft(blob, width * (anchor.X + jitterX) - size / 2);
            Canvas.SetTop(blob, height * (anchor.Y + jitterY) - size / 2);
            MeshCanvas.Children.Add(blob);

            if (_meshPeriod <= 0)
            {
                continue;
            }

            var drift = new TranslateTransform();
            blob.RenderTransform = drift;

            var dx = width * (0.26 + random.NextDouble() * 0.26) * (random.Next(2) == 0 ? 1 : -1);
            var dy = height * (0.22 + random.NextDouble() * 0.22) * (random.Next(2) == 0 ? 1 : -1);
            var seconds = _meshPeriod * (0.7 + random.NextDouble() * 0.9);
            var begin = -_meshPeriod * random.NextDouble();

            // 色块漂得很慢，20fps 肉眼无差别，但整屏渐变每帧都要重新合成，帧率上限能省下大部分开销
            var driftX = new DoubleAnimation(0, dx, TimeSpan.FromSeconds(seconds))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(begin),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            var driftY = new DoubleAnimation(0, dy, TimeSpan.FromSeconds(seconds * 1.37))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(begin * 0.7),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Timeline.SetDesiredFrameRate(driftX, _driftFrameRate);
            Timeline.SetDesiredFrameRate(driftY, _driftFrameRate);

            drift.BeginAnimation(TranslateTransform.XProperty, driftX);
            drift.BeginAnimation(TranslateTransform.YProperty, driftY);
        }
    }

    /// <summary>
    /// 单个色块：从中心实色向外淡出，只改透明度不改色相，过渡才自然
    /// </summary>
    private static RadialGradientBrush CreateBlobBrush(Color color)
    {
        var brush = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5
        };
        brush.GradientStops.Add(new GradientStop(WithAlpha(color, 228), 0d));
        brush.GradientStops.Add(new GradientStop(WithAlpha(color, 160), 0.35));
        brush.GradientStops.Add(new GradientStop(WithAlpha(color, 72), 0.68));
        brush.GradientStops.Add(new GradientStop(WithAlpha(color, 0), 1d));
        return brush;
    }

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    /// <summary>
    /// 与进程无关的稳定哈希（string.GetHashCode 每次启动结果不同）
    /// </summary>
    private static int StableSeed(string text)
    {
        unchecked
        {
            var hash = 2166136261;
            foreach (var c in text)
            {
                hash = (hash ^ c) * 16777619;
            }
            return (int)(hash & 0x7FFFFFFF);
        }
    }

    private void ClearMesh()
    {
        _meshSettings = null;
        _meshPeriod = 0;
        MeshCanvas.Children.Clear();
    }

    private void AdjustClockSize()
    {
        if (ClockHost.Visibility != Visibility.Visible)
        {
            return;
        }
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        // 显示秒时牌更多、更长，占宽比例要相应放大
        var ratio = LockClock.AspectRatio;
        var scale = Math.Clamp(_clockScale, 50, 150) / 100d;
        var byWidth = ActualWidth * (LockClock.ShowSeconds ? 0.64 : 0.52);
        var byHeight = ActualHeight * 0.78 * ratio;
        ClockHost.Width = Math.Min(byWidth, byHeight) * scale;

        var offsetY = Math.Clamp(_clockOffsetY, -40, 40) / 100d * ActualHeight;
        ClockHost.RenderTransform = new TranslateTransform(0, offsetY);
    }
}
