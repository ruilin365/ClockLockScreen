using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace ComputerLock.Components;

/// <summary>
/// 设置项滑块 + 可直接编辑的数值框。
/// 拖动时数值实时跟随；点数值可以手输，最多两位小数，超出范围自动夹紧。
/// </summary>
public partial class SettingSlider
{
    [Parameter]
    public string Label { get; set; } = "";

    [Parameter]
    public double Value { get; set; }

    [Parameter]
    public EventCallback<double> ValueChanged { get; set; }

    [Parameter]
    public double Min { get; set; }

    [Parameter]
    public double Max { get; set; } = 100d;

    [Parameter]
    public double Step { get; set; } = 1d;

    /// <summary>
    /// 数值后面的单位，例如 % / px / fps
    /// </summary>
    [Parameter]
    public string Suffix { get; set; } = "";

    [Parameter]
    public string Remark { get; set; } = "";

    /// <summary>
    /// 显示用：去掉无意义的小数位，最多两位
    /// </summary>
    private string Display => Value.ToString("0.##", CultureInfo.InvariantCulture);

    private Task OnSliderChanged(double value) => ValueChanged.InvokeAsync(Normalize(value));

    private Task OnInputChanged(ChangeEventArgs args)
    {
        var text = args.Value?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.CompletedTask;
        }

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            && !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
        {
            return Task.CompletedTask;
        }

        return ValueChanged.InvokeAsync(Normalize(parsed));
    }

    /// <summary>
    /// 夹到范围内并保留两位小数
    /// </summary>
    private double Normalize(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return Value;
        }

        return Math.Round(Math.Clamp(value, Min, Max), 2);
    }
}
