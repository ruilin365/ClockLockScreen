using System.Diagnostics;
using System.IO;

namespace ComputerLock.Components;

public partial class About
{
    private string _version = "";

    [Inject]
    private IStringLocalizer<Lang> Lang { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _version = $"v{AppBase.VersionString[..AppBase.VersionString.LastIndexOf('.')]}";
    }

    /// <summary>
    /// 打开发布目录里的 LICENSE 文件
    /// </summary>
    private void OpenLicense()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "LICENSE");
        if (!File.Exists(path))
        {
            return;
        }
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
