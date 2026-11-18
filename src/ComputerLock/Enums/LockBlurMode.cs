namespace ComputerLock.Enums;

/// <summary>
/// 锁屏模糊的实现方式
/// </summary>
public enum LockBlurMode
{
    /// <summary>
    /// 实时：走系统 DWM 材质，桌面怎么变都跟着变，模糊半径由系统固定
    /// </summary>
    Live = 0,

    /// <summary>
    /// 定格：锁屏瞬间截屏后自己模糊，半径可调，画面定格
    /// </summary>
    Frozen = 1
}
