namespace ComputerLock.Enums;

/// <summary>
/// 时钟背景板的填充方式
/// </summary>
public enum ClockBackgroundMode
{
    /// <summary>
    /// 不显示背景，时钟直接浮在屏幕内容上
    /// </summary>
    None = 0,

    /// <summary>
    /// 单色背景
    /// </summary>
    Solid = 1,

    /// <summary>
    /// 双色渐变背景
    /// </summary>
    Gradient = 2
}
