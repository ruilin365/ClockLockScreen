using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ComputerLock.Configuration;
internal class AppBase
{
    /// <summary>
    /// 改名前的程序名（.NET Core 下 FriendlyName 是程序集简单名，不带 .exe）
    /// </summary>
    private const string LegacyFriendlyName = "ComputerLock";

    /// <summary>
    /// App路径（包含文件名）
    /// </summary>
    public static string ExecutablePath { get; } = Process.GetCurrentProcess().MainModule.FileName;

    public static string FriendlyName { get; } = AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    /// App Data文件夹路径
    /// </summary>
    private static readonly string DataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    static AppBase()
    {
        MigrateLegacyData();
    }

    /// <summary>
    /// 程序从 ComputerLock 改名为 ClockLockScreen 后，配置目录跟着变了。
    /// 新目录还没有配置时，把旧目录里的文件复制过来，用户之前的设置不会丢。
    /// </summary>
    private static void MigrateLegacyData()
    {
        if (string.Equals(FriendlyName, LegacyFriendlyName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var legacyDir = Path.Combine(DataPath, LegacyFriendlyName);
            var currentDir = Path.Combine(DataPath, FriendlyName);
            if (Directory.Exists(currentDir) || !Directory.Exists(legacyDir))
            {
                return;
            }

            Directory.CreateDirectory(currentDir);
            foreach (var file in Directory.GetFiles(legacyDir))
            {
                File.Copy(file, Path.Combine(currentDir, Path.GetFileName(file)), false);
            }
        }
        catch
        {
            // 迁移失败不影响启动，最多用默认配置
        }
    }
    /// <summary>
    /// 配置文件路径
    /// </summary>
    public static string ConfigPath { get; } = Path.Combine(DataPath, FriendlyName, "config.json");

    /// <summary>
    /// 版本文件路径
    /// </summary>
    public static string VersionFilePath { get; } = Path.Combine(DataPath, FriendlyName, "current_version");

    /// <summary>
    /// 版本号
    /// </summary>
    public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidOperationException("App Version");
    public static string VersionString { get; } = Version.ToString();
}