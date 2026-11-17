namespace ComputerLock.Enums;

/// <summary>
/// 锁屏时屏幕的表现方式
/// </summary>
public enum LockScreenStyle
{
    /// <summary>
    /// 完全透明：屏幕内容原样可见，只拦截输入（原版行为）
    /// </summary>
    Transparent = 0,

    /// <summary>
    /// 黑色遮罩：全屏覆盖一层黑色，透明度可调
    /// </summary>
    BlackMask = 1,

    /// <summary>
    /// 翻页时钟：显示翻页时钟，透明度与背景可调
    /// </summary>
    Clock = 2
}
