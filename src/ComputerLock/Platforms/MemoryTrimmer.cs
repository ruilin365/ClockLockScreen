using System.Runtime.InteropServices;

namespace ComputerLock.Platforms;

/// <summary>
/// 锁屏窗口关掉之后回收一次内存。
/// WPF 的全屏分层窗口本身要占几十 MB 工作集，窗口关掉后 .NET 也不见得马上把页还给系统，
/// 任务管理器里就长期挂着一百多 MB。这里在空闲时机强制回收一次，再把工作集修剪掉。
/// </summary>
internal static class MemoryTrimmer
{
    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    /// <summary>
    /// 后台延迟回收，避免和窗口关闭、动画收尾抢线程
    /// </summary>
    public static void TrimAsync()
    {
        Task.Run(() =>
        {
            try
            {
                // 等窗口真正销毁、终结器跑完，否则这次回收没意义
                Thread.Sleep(600);

                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);

                // 工作集修剪：页面转成待命状态，进程占用的物理内存立刻降下来，
                // 下次用到会从系统缓存读回，不影响使用
                EmptyWorkingSet(GetCurrentProcess());
            }
            catch
            {
                // 回收失败不影响功能
            }
        });
    }
}
