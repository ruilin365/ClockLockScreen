using ComputerLock.Interfaces;
using ComputerLock.Models;
using ComputerLock.Services;
using Microsoft.AspNetCore.Components.Web;
using MudExtensions;

namespace ComputerLock.Components.Settings;

public partial class LockSettings : IDisposable
{
    [Inject]
    private AppSettings AppSettings { get; set; } = null!;

    [Inject]
    private AppSettingsProvider AppSettingsProvider { get; set; } = null!;

    [Inject]
    private IStringLocalizer<Lang> Lang { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;


    [Inject]
    private IGlobalLockService GlobalLockService { get; set; } = null!;

    [Inject]
    private HotkeyHook HotkeyHook { get; set; } = null!;

    [Inject]
    private ILogger Logger { get; set; } = null!;

    [Inject]
    private PowerManager PowerManager { get; set; } = null!;

    [Inject]
    private LockPreviewService LockPreview { get; set; } = null!;

    private bool _isPreviewing;
    private DateTime _lastPreviewRefresh = DateTime.MinValue;
    private CancellationTokenSource? _persistCts;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _isPreviewing = LockPreview.IsRunning;
        LockPreview.Stopped += OnPreviewStopped;
    }

    /// <summary>
    /// 预览被服务自己关掉时，把界面上的开关也拨回来
    /// </summary>
    private void OnPreviewStopped()
    {
        _ = InvokeAsync(() =>
        {
            _isPreviewing = false;
            StateHasChanged();
        });
    }

    private MudTextFieldExtended<string>? _textTipsMessageRef;
    private bool _lockTipsMessageEdit;
    public string LockTipsMessage
    {
        get => AppSettings.LockTipsMessage.IsEmpty()
            ? Lang["LockTipsValue"] // 显示默认文案
            : AppSettings.LockTipsMessage;
        set => AppSettings.LockTipsMessage = value;
    }
    private async Task EnableTipsMessageEditing()
    {
        _lockTipsMessageEdit = true;
        // 等待渲染后再聚焦，否则焦点可能丢失
        await Task.Delay(1);
        _textTipsMessageRef?.FocusAsync();
    }

    private void TipsMessageBlur(FocusEventArgs args)
    {
        TipsMessageFinishEditing();
    }
    private void TipsMessageKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            TipsMessageFinishEditing();
        }
    }
    private void TipsMessageFinishEditing()
    {
        if (!_lockTipsMessageEdit)
        {
            return;
        }
        _lockTipsMessageEdit = false;
    }

    private void SaveSettings()
    {
        AppSettingsProvider.SaveSettings(AppSettings);
        // 预览开着的时候，改一项就立刻反映到画面上
        LockPreview.Refresh(AppSettings);
        _lastPreviewRefresh = DateTime.UtcNow;
    }

    /// <summary>
    /// 拖动滑块时用：数值实时更新，但预览限流刷新、落盘防抖，
    /// 否则每一像素都重建一次渐变背景，画面反而卡。
    /// </summary>
    private void SaveSettingsLive()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastPreviewRefresh).TotalMilliseconds >= 50)
        {
            _lastPreviewRefresh = now;
            LockPreview.Refresh(AppSettings);
        }

        _persistCts?.Cancel();
        _persistCts = new CancellationTokenSource();
        _ = PersistLaterAsync(_persistCts.Token);
    }

    private async Task PersistLaterAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(300, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        await InvokeAsync(() => AppSettingsProvider.SaveSettings(AppSettings));
    }

    private void TogglePreview(bool isOn)
    {
        _isPreviewing = isOn;
        if (isOn)
        {
            LockPreview.Start(AppSettings, Logger);
        }
        else
        {
            LockPreview.Stop();
        }
    }

    public void Dispose()
    {
        LockPreview.Stopped -= OnPreviewStopped;

        // 还在防抖窗口里就离开页面的话，补一次落盘，别把最后一次拖动丢了
        if (_persistCts is { IsCancellationRequested: false })
        {
            _persistCts.Cancel();
            AppSettingsProvider.SaveSettings(AppSettings);
        }

        // 离开设置页就把预览关掉，避免留一层看不见的全屏窗口
        if (_isPreviewing)
        {
            _isPreviewing = false;
            LockPreview.Stop();
        }
    }

    /// <summary>
    /// 把锁屏画面外观恢复成默认（不动密码、快捷键、自动锁定）
    /// </summary>
    private void ResetAppearanceToDefault()
    {
        AppSettings.ResetAppearance();
        SaveSettings();
        StateHasChanged();
        Snackbar.Add(Lang["ResetAppearanceDone"], Severity.Success);
    }

    /// <summary>
    /// 模糊方式：定格（截一张）或实时（跟着屏幕刷新）
    /// </summary>
    private void BlurModeChanged(LockBlurMode mode)
    {
        AppSettings.LockBlurMode = mode;
        SaveSettings();
    }

    /// <summary>
    /// 是否已经导入过自定义字体
    /// </summary>
    private bool HasCustomFont =>
        !string.IsNullOrWhiteSpace(AppSettings.ClockCustomFontPath)
        && System.IO.File.Exists(AppSettings.ClockCustomFontPath);

    private string CustomFontCaption =>
        string.IsNullOrWhiteSpace(AppSettings.ClockCustomFontName)
            ? System.IO.Path.GetFileNameWithoutExtension(AppSettings.ClockCustomFontPath)
            : AppSettings.ClockCustomFontName;

    /// <summary>
    /// 让 WinForms 对话框认 WPF 窗口做宿主
    /// </summary>
    private sealed class WindowHandle(IntPtr handle) : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }

    private void ClockFontChanged(string fontId)
    {
        AppSettings.ClockFontId = fontId;
        SaveSettings();
    }

    /// <summary>
    /// 选一个本地 ttf/otf，复制到程序目录后立即启用
    /// </summary>
    private void ImportFont()
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = Lang["ClockFontImport"],
            Filter = "字体文件 (*.ttf;*.otf)|*.ttf;*.otf|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        // 设置窗口可能是 Topmost，给对话框指定宿主，避免它跑到窗口后面
        var owner = System.Windows.Application.Current?.MainWindow;
        var handle = owner == null ? IntPtr.Zero : new System.Windows.Interop.WindowInteropHelper(owner).Handle;
        var result = handle == IntPtr.Zero
            ? dialog.ShowDialog()
            : dialog.ShowDialog(new WindowHandle(handle));

        if (result != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        try
        {
            var family = ClockFonts.ReadFamilyName(dialog.FileName);
            if (string.IsNullOrWhiteSpace(family))
            {
                Snackbar.Add(Lang["ClockFontImportFailed"], Severity.Error);
                return;
            }

            var saved = ClockFonts.Import(dialog.FileName);
            AppSettings.ClockCustomFontPath = saved;
            AppSettings.ClockCustomFontName = family;
            AppSettings.ClockFontId = ClockFonts.CustomId;
            SaveSettings();
            Snackbar.Add($"{Lang["ClockFontImportOk"]}：{family}", Severity.Success);
        }
        catch (Exception ex)
        {
            Logger.Error("导入字体失败", ex);
            Snackbar.Add(Lang["ClockFontImportFailed"], Severity.Error);
        }
    }

    private void MaskFillModeChanged(ClockBackgroundMode mode)
    {
        AppSettings.MaskFillMode = mode;
        // 刚切到渐变时，如果还没选过预设，给一套默认配色
        if (mode == ClockBackgroundMode.Gradient
            && string.IsNullOrEmpty(AppSettings.MaskPreset)
            && string.IsNullOrWhiteSpace(AppSettings.MaskColor1))
        {
            ApplyMaskPreset(ColorPresets.Find(ColorPresets.Background, "bg-aurora") ?? ColorPresets.Background[0]);
            return;
        }
        SaveSettings();
    }

    #region 遮罩配色

    private void ApplyMaskPreset(ColorPreset preset)
    {
        AppSettings.MaskPreset = preset.Id;
        // 同时把首尾色写进自定义槽，切到自定义时有个合理的起点
        AppSettings.MaskColor1 = preset.Stops[0].Hex;
        AppSettings.MaskColor2 = preset.Stops[^1].Hex;
        SaveSettings();
    }

    private void MaskColor1Changed(ChangeEventArgs args)
    {
        if (args.Value?.ToString() is not { Length: > 0 } value)
        {
            return;
        }
        AppSettings.MaskColor1 = NormalizeColor(value);
        AppSettings.MaskPreset = "";   // 手改过颜色就不再算预设
        SaveSettings();
    }

    private void MaskColor2Changed(ChangeEventArgs args)
    {
        if (args.Value?.ToString() is not { Length: > 0 } value)
        {
            return;
        }
        AppSettings.MaskColor2 = NormalizeColor(value);
        AppSettings.MaskPreset = "";
        SaveSettings();
    }

    private void SwapMaskColors()
    {
        (AppSettings.MaskColor1, AppSettings.MaskColor2) =
            (AppSettings.MaskColor2, AppSettings.MaskColor1);
        AppSettings.MaskPreset = "";
        SaveSettings();
    }

    #endregion

    #region 状态提示配色

    private void ApplyBreathingPreset(ColorPreset preset)
    {
        AppSettings.BreathingPreset = preset.Id;
        AppSettings.BreathingColor1 = preset.Stops[0].Hex;
        AppSettings.BreathingColor2 = preset.Stops[^1].Hex;
        SaveSettings();
    }

    private void BreathingColor1Changed(ChangeEventArgs args)
    {
        if (args.Value?.ToString() is not { Length: > 0 } value)
        {
            return;
        }
        AppSettings.BreathingColor1 = NormalizeColor(value);
        AppSettings.BreathingPreset = "";
        SaveSettings();
    }

    private void BreathingColor2Changed(ChangeEventArgs args)
    {
        if (args.Value?.ToString() is not { Length: > 0 } value)
        {
            return;
        }
        AppSettings.BreathingColor2 = NormalizeColor(value);
        AppSettings.BreathingPreset = "";
        SaveSettings();
    }

    private void SwapBreathingColors()
    {
        (AppSettings.BreathingColor1, AppSettings.BreathingColor2) =
            (AppSettings.BreathingColor2, AppSettings.BreathingColor1);
        AppSettings.BreathingPreset = "";
        SaveSettings();
    }

    #endregion

    private static string NormalizeColor(string input)
    {
        var color = input.Trim();
        return color.StartsWith('#') ? color : $"#{color}";
    }

    private void LockStatusDisplayChanged(LockStatusDisplay lockStatusDisplay)
    {
        AppSettings.LockStatusDisplay = lockStatusDisplay;
        SaveSettings();
    }

    private void AutoLockChanged(int autoLockMinute)
    {
        AppSettings.AutoLockSecond = autoLockMinute * 60;
        SaveSettings();
        GlobalLockService.UpdateAutoLockSettings();
    }
    private Task SetLockHotkey(string hotkey)
    {
        // 检查快捷键是否与解锁快捷键相同
        if (hotkey.IsNotEmpty() && hotkey == AppSettings.UnlockHotkeyString)
        {
            Snackbar.Add(Lang["HotkeyDuplicateError"], Severity.Error);
            return Task.CompletedTask;
        }

        AppSettings.LockHotkeyString = hotkey;
        SaveSettings();
        RegisterLockHotkey();
        return Task.CompletedTask;
    }
    private Task ClearLockHotkey()
    {
        AppSettings.LockHotkeyString = "";
        SaveSettings();
        UnregisterLockHotkey();
        return Task.CompletedTask;
    }


    public void RegisterLockHotkey()
    {
        try
        {
            if (AppSettings.LockHotkey != null)
            {
                Logger.Info("注册锁屏热键");
                HotkeyHook.Register((int)HotkeyType.Lock, AppSettings.LockHotkey);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"绑定锁屏热键失败", ex);
            Snackbar.Add($"{Lang["ExRegistFailed"]}{ex.Message}", Severity.Error);
        }
    }
    public void UnregisterLockHotkey()
    {
        try
        {
            Logger.Info("释放锁屏热键");
            HotkeyHook.Unregister((int)HotkeyType.Lock);
        }
        catch (Exception ex)
        {
            Logger.Error($"释放锁屏热键失败", ex);
            //MessageBoxUtils.ShowError($"取消快捷键失败：{ex.Message}");
        }
    }

    private void AutoPowerChanged(int autoPowerMinute)
    {
        AppSettings.AutoPowerSecond = autoPowerMinute * 60;
        SaveSettings();
    }

    private void PowerActionTypeChanged(PowerActionType powerActionType)
    {
        if (powerActionType == PowerActionType.Hibernate && !PowerManager.IsHibernateSupported())
        {
            Snackbar.Add("系统可能未启用休眠功能，该功能可能无效。", Severity.Error);
            return;
        }
        AppSettings.AutoPowerActionType = powerActionType;
        SaveSettings();
    }
}