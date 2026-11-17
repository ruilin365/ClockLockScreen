using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ComputerLock.Platforms;

/// <summary>
/// 给主窗口套上 Windows 11 的系统材质（亚克力/云母），做出毛玻璃底。
/// 只在 Windows 11（build 22000+）生效，其余系统返回 false，由调用方保留不透明背景。
/// </summary>
public static class AcrylicHelper
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    /// <summary>
    /// 圆角偏好（Win11）
    /// </summary>
    private const int DwmwaWindowCornerPreference = 33;

    /// <summary>
    /// 系统背景材质（Win11 22H2+）
    /// </summary>
    private const int DwmwaSystemBackdropType = 38;

    private const int DwmsbtAuto = 0;
    private const int DwmsbtNone = 1;
    private const int DwmsbtMainWindow = 2;      // Mica
    private const int DwmsbtTransientWindow = 3; // Acrylic
    private const int DwmsbtTabbedWindow = 4;    // Mica Alt

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    /// <summary>
    /// 是否支持系统材质
    /// </summary>
    public static bool IsSupported => Environment.OSVersion.Version.Build >= 22000;

    /// <summary>
    /// 开启亚克力背景。成功返回 true，此时窗口自身背景必须是透明的，否则看不到效果。
    /// </summary>
    public static bool TryEnableAcrylic(Window window, bool isDark)
    {
        if (!IsSupported)
        {
            return false;
        }

        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            var dark = isDark ? 1 : 0;
            DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));

            var corner = 2; // 圆角
            DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref corner, sizeof(int));

            var backdrop = DwmsbtTransientWindow;
            var result = DwmSetWindowAttribute(handle, DwmwaSystemBackdropType, ref backdrop, sizeof(int));

            if (result != 0)
            {
                // 老版本 Win11 没有 DWMWA_SYSTEMBACKDROP_TYPE，退回 Win10 的写法
                return TryEnableBlurBehind(window);
            }

            // 让客户区延伸到整个窗口，材质才会在整块区域生效
            var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            DwmExtendFrameIntoClientArea(handle, ref margins);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Win10 的亚克力写法：SetWindowCompositionAttribute + ACCENT_ENABLE_ACRYLICBLURBEHIND
    /// </summary>
    private static bool TryEnableBlurBehind(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            var accent = new AccentPolicy
            {
                AccentState = AccentState.EnableAcrylicBlurBehind,
                AccentFlags = 2,
                GradientColor = unchecked((int)0x99202026) // ABGR
            };

            var size = Marshal.SizeOf<AccentPolicy>();
            var pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, pointer, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.AccentPolicy,
                Data = pointer,
                SizeOfData = size
            };

            var result = SetWindowCompositionAttribute(handle, ref data);
            Marshal.FreeHGlobal(pointer);
            return result != 0;
        }
        catch
        {
            return false;
        }
    }

    private enum AccentState
    {
        Disabled = 0,
        EnableBlurBehind = 3,
        EnableAcrylicBlurBehind = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    private enum WindowCompositionAttribute
    {
        AccentPolicy = 19
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
}
