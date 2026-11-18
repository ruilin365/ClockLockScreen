using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SdBitmap = System.Drawing.Bitmap;
using SdLockMode = System.Drawing.Imaging.ImageLockMode;
using SdPixelFormat = System.Drawing.Imaging.PixelFormat;
using SdGraphics = System.Drawing.Graphics;
using SdRect = System.Drawing.Rectangle;
using ThreadTimer = System.Threading.Timer;

namespace ComputerLock.Platforms;

/// <summary>
/// 屏幕取景 + 模糊，两种模式共用一套模糊算法：
/// <list type="bullet">
/// <item>定格：锁屏那一刻截一张，整场锁屏都用它，半径可调、画面不动。</item>
/// <item>实时：按固定频率重截，背后有东西在动（比如 AI 在滚日志）能隐约看出来。</item>
/// </list>
/// 取景都走「全屏缩到 1/4 的小图」，先把数据和内存降下来，模糊本身在小图上做，
/// 最后由 Image 放大显示，放大时的插值又算一层柔化。
/// </summary>
internal static class DesktopSnapshot
{
    /// <summary>
    /// 取景降采样倍数。模糊会把细节抹平，先缩到 1/4 再做半径模糊，开销降到 1/16，观感差别很小。
    /// </summary>
    private const int DownsampleFactor = 4;

    /// <summary>
    /// 强度 100 对应的模糊半径（作用在降采样后的图上）。
    /// 小图放大显示，实际观感约等于原图的 3 倍半径，100 已经糊到看不出内容。
    /// </summary>
    private const int MaxRadius = 16;

    /// <summary>
    /// 盒式模糊迭代次数，3 次足够接近高斯
    /// </summary>
    private const int BlurPasses = 3;

    /// <summary>
    /// 实时取景的最低帧率，低于这个就没必要了
    /// </summary>
    private const int MinLiveFps = 1;

    /// <summary>
    /// 实时取景的帧率上限。取一次屏要 20~30ms，再高就纯粹在烧 CPU 了
    /// </summary>
    private const int MaxLiveFps = 10;

    #region 定格

    private static BitmapSource? _frozenFull;
    private static SdRect _frozenBounds;
    private static long _frozenStamp;
    private static readonly Dictionary<string, BitmapSource> FrozenCache = [];

    /// <summary>
    /// 截取整个虚拟屏幕。定格模式必须在锁屏窗口显示之前调用，
    /// 否则会把锁屏窗口自己截进去（实时模式靠 WDA_EXCLUDEFROMCAPTURE 规避，不需要这个顺序）。
    /// </summary>
    public static void Capture()
    {
        try
        {
            var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            using var bitmap = new SdBitmap(bounds.Width, bounds.Height, SdPixelFormat.Format32bppArgb);
            using (var graphics = SdGraphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bitmap.Size,
                    System.Drawing.CopyPixelOperation.SourceCopy);
            }

            var data = bitmap.LockBits(
                new SdRect(0, 0, bounds.Width, bounds.Height),
                SdLockMode.ReadOnly,
                SdPixelFormat.Format32bppArgb);

            var source = BitmapSource.Create(
                bounds.Width, bounds.Height, 96, 96,
                PixelFormats.Bgra32, null,
                data.Scan0, data.Stride * bounds.Height, data.Stride);

            bitmap.UnlockBits(data);
            source.Freeze();

            _frozenFull = source;
            _frozenBounds = bounds;
            _frozenStamp++;
            FrozenCache.Clear();
        }
        catch
        {
            // 截屏失败（远程会话、权限异常等）就当没有快照，锁屏窗口保持原样
            _frozenFull = null;
        }
    }

    /// <summary>
    /// 按当前设置决定要不要在锁屏前截一张定格底图。
    /// 必须是「窗口盖住屏幕之前」，否则截到的是锁屏画面自己。
    /// </summary>
    public static void PrepareFrozenIfNeeded(Configuration.AppSettings settings)
    {
        if (settings.LockBlurEnabled && settings.LockBlurMode == Enums.LockBlurMode.Frozen)
        {
            Capture();
        }
    }

    /// <summary>
    /// 有没有可用的定格快照
    /// </summary>
    public static bool HasSnapshot => _frozenFull != null;

    /// <summary>
    /// 丢掉定格快照（解锁后调用，免得一直占着几 MB 内存）
    /// </summary>
    public static void ClearFrozen()
    {
        _frozenFull = null;
        FrozenCache.Clear();
    }

    /// <summary>
    /// 取某个屏幕区域对应的定格模糊图（物理像素坐标，来自 Screen.Bounds）
    /// </summary>
    public static BitmapSource? For(SdRect screenBounds, double strength)
    {
        if (_frozenFull == null)
        {
            return null;
        }

        var frame = FrozenFrame(strength);
        return frame == null ? null : CropRegion(frame, _frozenBounds, screenBounds);
    }

    /// <summary>
    /// 整屏定格图按强度模糊一次，多屏共用同一张，避免每块屏各算一遍
    /// </summary>
    private static BitmapSource? FrozenFrame(double strength)
    {
        if (_frozenFull == null)
        {
            return null;
        }

        var percent = Math.Clamp(strength, 0, 100);
        var key = $"{percent:0.0}|{_frozenStamp}";
        if (FrozenCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var frame = BlurWhole(_frozenFull, percent);

        // 按屏幕裁切的旧条目没用了，换壁纸/改强度时会不断产生新键
        if (FrozenCache.Count > 8)
        {
            FrozenCache.Clear();
        }
        FrozenCache[key] = frame;
        return frame;
    }

    #endregion

    #region 实时

    private static ThreadTimer? _liveTimer;
    private static BitmapSource? _liveFrame;
    private static SdRect _liveBounds;
    private static double _liveStrength = 45;
    private static int _liveBusy;
    private static DibSurface? _liveSurface;

    /// <summary>
    /// 有新的实时帧了。事件在后台线程触发，订阅方自己切回 UI 线程。
    /// </summary>
    public static event EventHandler? LiveFrameReady;

    /// <summary>
    /// 实时取景是否在跑
    /// </summary>
    public static bool IsLiveRunning => _liveTimer != null;

    /// <summary>
    /// 正在用实时取景的窗口数。多屏时每块屏一个窗口，只有全部退出才真正停掉取景
    /// </summary>
    private static int _liveRefCount;

    /// <summary>
    /// 启动（或改频率）实时取景。每调用一次要与 StopLive 配对一次。
    /// </summary>
    public static void StartLive(double strength, int fps)
    {
        _liveStrength = Math.Clamp(strength, 0, 100);

        var interval = (int)Math.Round(1000d / Math.Clamp(fps, MinLiveFps, MaxLiveFps));
        interval = Math.Clamp(interval, 60, 1000);

        Interlocked.Increment(ref _liveRefCount);

        if (_liveTimer != null)
        {
            _liveTimer.Change(0, interval);
            return;
        }

        _liveTimer = new ThreadTimer(LiveTick, null, 0, interval);
    }

    /// <summary>
    /// 只改模糊强度，不重启计时器（拖滑块时每帧都会调）
    /// </summary>
    public static void UpdateLiveStrength(double strength)
        => _liveStrength = Math.Clamp(strength, 0, 100);

    public static void StopLive()
    {
        if (Interlocked.Decrement(ref _liveRefCount) > 0)
        {
            // 还有别的屏幕在用，留着计时器
            return;
        }

        Interlocked.Exchange(ref _liveRefCount, 0);
        _liveTimer?.Dispose();
        _liveTimer = null;
        _liveFrame = null;
        _liveSurface?.Dispose();
        _liveSurface = null;
    }

    /// <summary>
    /// 取当前实时帧里某块屏幕的部分
    /// </summary>
    public static BitmapSource? LiveRegion(SdRect screenBounds)
    {
        var frame = _liveFrame;
        return frame == null ? null : CropRegion(frame, _liveBounds, screenBounds, DownsampleFactor);
    }

    private static void LiveTick(object? state)
    {
        // 上一次还没取完（机器卡的时候）就跳过这一帧，别让任务堆积
        if (Interlocked.Exchange(ref _liveBusy, 1) == 1)
        {
            return;
        }

        try
        {
            var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var width = Math.Max(1, bounds.Width / DownsampleFactor);
            var height = Math.Max(1, bounds.Height / DownsampleFactor);

            // 分辨率变了（换显示器、改缩放）就重建取景面
            if (_liveSurface == null || _liveSurface.Width != width || _liveSurface.Height != height)
            {
                _liveSurface?.Dispose();
                _liveSurface = new DibSurface(width, height);
            }

            _liveSurface.Capture(bounds);
            var pixels = _liveSurface.ToPixels();

            var radius = (int)Math.Round(_liveStrength / 100d * MaxRadius);
            BlurPixels(pixels, width, height, radius);

            var frame = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null,
                pixels, width * 4);
            frame.Freeze();

            _liveBounds = bounds;
            _liveFrame = frame;

            LiveFrameReady?.Invoke(null, EventArgs.Empty);
        }
        catch
        {
            // 取景失败（全屏独占程序、锁屏界面、远程会话）就当这帧没取到
        }
        finally
        {
            Volatile.Write(ref _liveBusy, 0);
        }
    }

    #endregion

    #region 模糊与裁切

    /// <summary>
    /// 整屏图降采样 → 盒式模糊。结果由 Image 放大显示，放大本身的插值也算一层柔化。
    /// </summary>
    private static BitmapSource? BlurWhole(BitmapSource source, double percent)
    {
        var width = Math.Max(1, source.PixelWidth / DownsampleFactor);
        var height = Math.Max(1, source.PixelHeight / DownsampleFactor);

        var scaled = new TransformedBitmap(
            source,
            new ScaleTransform((double)width / source.PixelWidth, (double)height / source.PixelHeight));
        var converted = new FormatConvertedBitmap(scaled, PixelFormats.Bgra32, null, 0);

        var stride = width * 4;
        var buffer = new byte[stride * height];
        converted.CopyPixels(buffer, stride, 0);

        var radius = (int)Math.Round(Math.Clamp(percent, 0, 100) / 100d * MaxRadius);
        BlurPixels(buffer, width, height, radius);

        var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, buffer, stride);
        result.Freeze();
        return result;
    }

    /// <summary>
    /// 从整屏小图里裁出某块屏幕对应的部分
    /// </summary>
    private static BitmapSource? CropRegion(BitmapSource frame, SdRect frameBounds, SdRect screenBounds,
        int downsample = DownsampleFactor)
    {
        var x = (screenBounds.X - frameBounds.X) / downsample;
        var y = (screenBounds.Y - frameBounds.Y) / downsample;
        var width = screenBounds.Width / downsample;
        var height = screenBounds.Height / downsample;

        x = Math.Clamp(x, 0, Math.Max(0, frame.PixelWidth - 1));
        y = Math.Clamp(y, 0, Math.Max(0, frame.PixelHeight - 1));
        width = Math.Clamp(width, 1, frame.PixelWidth - x);
        height = Math.Clamp(height, 1, frame.PixelHeight - y);

        try
        {
            var cropped = new CroppedBitmap(frame, new Int32Rect(x, y, width, height));
            cropped.Freeze();
            return cropped;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 多次双向盒式模糊，近似高斯
    /// </summary>
    private static void BlurPixels(byte[] buffer, int width, int height, int radius)
    {
        for (var i = 0; i < BlurPasses && radius > 0; i++)
        {
            BoxBlurHorizontal(buffer, width, height, radius);
            BoxBlurVertical(buffer, width, height, radius);
        }
    }

    /// <summary>
    /// 水平方向盒式模糊，滑动窗口累加，每像素 O(1)
    /// </summary>
    private static void BoxBlurHorizontal(byte[] buffer, int width, int height, int radius)
    {
        var temp = new byte[buffer.Length];
        var window = radius * 2 + 1;

        for (var y = 0; y < height; y++)
        {
            var row = y * width * 4;
            int b = 0, g = 0, r = 0, a = 0;

            for (var i = -radius; i <= radius; i++)
            {
                var x = Math.Clamp(i, 0, width - 1);
                var p = row + x * 4;
                b += buffer[p];
                g += buffer[p + 1];
                r += buffer[p + 2];
                a += buffer[p + 3];
            }

            for (var x = 0; x < width; x++)
            {
                var o = row + x * 4;
                temp[o] = (byte)(b / window);
                temp[o + 1] = (byte)(g / window);
                temp[o + 2] = (byte)(r / window);
                temp[o + 3] = (byte)(a / window);

                var outX = Math.Clamp(x - radius, 0, width - 1);
                var inX = Math.Clamp(x + radius + 1, 0, width - 1);
                var po = row + outX * 4;
                var pi = row + inX * 4;
                b += buffer[pi] - buffer[po];
                g += buffer[pi + 1] - buffer[po + 1];
                r += buffer[pi + 2] - buffer[po + 2];
                a += buffer[pi + 3] - buffer[po + 3];
            }
        }

        Buffer.BlockCopy(temp, 0, buffer, 0, buffer.Length);
    }

    /// <summary>
    /// 垂直方向盒式模糊
    /// </summary>
    private static void BoxBlurVertical(byte[] buffer, int width, int height, int radius)
    {
        var temp = new byte[buffer.Length];
        var window = radius * 2 + 1;
        var stride = width * 4;

        for (var x = 0; x < width; x++)
        {
            var column = x * 4;
            int b = 0, g = 0, r = 0, a = 0;

            for (var i = -radius; i <= radius; i++)
            {
                var y = Math.Clamp(i, 0, height - 1);
                var p = y * stride + column;
                b += buffer[p];
                g += buffer[p + 1];
                r += buffer[p + 2];
                a += buffer[p + 3];
            }

            for (var y = 0; y < height; y++)
            {
                var o = y * stride + column;
                temp[o] = (byte)(b / window);
                temp[o + 1] = (byte)(g / window);
                temp[o + 2] = (byte)(r / window);
                temp[o + 3] = (byte)(a / window);

                var outY = Math.Clamp(y - radius, 0, height - 1);
                var inY = Math.Clamp(y + radius + 1, 0, height - 1);
                var po = outY * stride + column;
                var pi = inY * stride + column;
                b += buffer[pi] - buffer[po];
                g += buffer[pi + 1] - buffer[po + 1];
                r += buffer[pi + 2] - buffer[po + 2];
                a += buffer[pi + 3] - buffer[po + 3];
            }
        }

        Buffer.BlockCopy(temp, 0, buffer, 0, buffer.Length);
    }

    #endregion

    #region 窗口捕获排除

    private const uint WdaExcludeFromCapture = 0x00000011;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    /// <summary>
    /// 恢复窗口参与截屏（预览结束时要还原，别让设置窗口一直截不到）
    /// </summary>
    public static bool IncludeInCapture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return SetWindowDisplayAffinity(hwnd, 0x00000000);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 把窗口标记成「不参与截屏」。实时取景时屏幕上是自己的锁屏窗口，
    /// 不做这个标记就会把自己截进去，越截越模糊（画面自我嵌套）。
    /// </summary>
    public static bool ExcludeFromCapture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return SetWindowDisplayAffinity(hwnd, WdaExcludeFromCapture);
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 屏幕回读

    /// <summary>
    /// 一块复用的 DIB 内存。StretchBlt 直接把屏幕缩进这块内存，
    /// 不分配全分辨率位图，也不走 GDI+ 的 Image 转换，省掉两次整屏拷贝。
    /// </summary>
    private sealed class DibSurface : IDisposable
    {
        private const int Halftone = 4;
        private const int SrcCopy = 0x00CC0020;

        private readonly IntPtr _screenDc;
        private readonly IntPtr _memDc;
        private readonly IntPtr _bitmap;
        private readonly IntPtr _previous;
        private readonly IntPtr _bits;
        private byte[] _buffer;

        public int Width { get; }
        public int Height { get; }

        public DibSurface(int width, int height)
        {
            Width = width;
            Height = height;
            _buffer = new byte[width * height * 4];

            var info = new BitmapInfoHeader
            {
                biSize = 40,
                biWidth = width,
                // 负数 = 自上而下，行序与 WPF 的 BGRA 一致，省一次翻转
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0
            };

            _screenDc = GetDC(IntPtr.Zero);
            _memDc = CreateCompatibleDC(_screenDc);
            _bitmap = CreateDIBSection(_memDc, ref info, 0, out _bits, IntPtr.Zero, 0);
            _previous = SelectObject(_memDc, _bitmap);

            // HALFTONE 是软件高质量缩放，比丢弃像素的模式慢一倍多；
            // 反正后面还要重模糊，用 COLORONCOLOR 直接丢像素看不出来
            SetStretchBltMode(_memDc, ColorOnColor);
        }

        public void Capture(SdRect bounds)
        {
            StretchBlt(_memDc, 0, 0, Width, Height,
                _screenDc, bounds.Left, bounds.Top, bounds.Width, bounds.Height, SrcCopy);
        }

        public byte[] ToPixels()
        {
            Marshal.Copy(_bits, _buffer, 0, _buffer.Length);
            return _buffer;
        }

        public void Dispose()
        {
            SelectObject(_memDc, _previous);
            if (_bitmap != IntPtr.Zero)
            {
                DeleteObject(_bitmap);
            }
            DeleteDC(_memDc);
            ReleaseDC(IntPtr.Zero, _screenDc);
        }

        private const int ColorOnColor = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfoHeader
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BitmapInfoHeader info, uint usage,
            out IntPtr bits, IntPtr section, uint offset);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool StretchBlt(IntPtr destDc, int xDest, int yDest, int wDest, int hDest,
            IntPtr srcDc, int xSrc, int ySrc, int wSrc, int hSrc, int rop);

        [DllImport("gdi32.dll")]
        private static extern int SetStretchBltMode(IntPtr hdc, int mode);
    }

    #endregion
}
