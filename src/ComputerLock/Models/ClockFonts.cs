using System.IO;
using System.Windows.Media;
using FontFamily = System.Windows.Media.FontFamily;

namespace ComputerLock.Models;

/// <summary>
/// 一个时钟字体预设
/// </summary>
public sealed class ClockFontPreset
{
    public required string Id { get; init; }

    /// <summary>
    /// 界面上显示的名字
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 字体文件里登记的家族名
    /// </summary>
    public required string FamilyName { get; init; }

    /// <summary>
    /// 随程序发布的字体文件名（在 fonts/ 目录下）
    /// </summary>
    public required string FileName { get; init; }

    public override string ToString() => DisplayName;
}

/// <summary>
/// 翻页时钟可用字体：内置几套 SIL OFL 授权字体，外加用户自己导入的 ttf/otf
/// </summary>
public static class ClockFonts
{
    public const string CustomId = "custom";

    public const string DefaultId = "bebas";

    /// <summary>
    /// 内置字体，全部为 SIL Open Font License 1.1，可随程序一起分发
    /// </summary>
    public static IReadOnlyList<ClockFontPreset> Embedded { get; } =
    [
        new() { Id = "barlow", DisplayName = "Barlow Condensed", FamilyName = "Barlow Condensed", FileName = "BarlowCondensed-Regular.ttf" },
        new() { Id = "oswald", DisplayName = "Oswald", FamilyName = "Oswald", FileName = "Oswald.ttf" },
        new() { Id = "archivo", DisplayName = "Archivo Narrow", FamilyName = "Archivo Narrow", FileName = "ArchivoNarrow.ttf" },
        new() { Id = "bebas", DisplayName = "Bebas Neue", FamilyName = "Bebas Neue", FileName = "BebasNeue-Regular.ttf" },
        new() { Id = "anton", DisplayName = "Anton", FamilyName = "Anton", FileName = "Anton-Regular.ttf" },
        new() { Id = "orbitron", DisplayName = "Orbitron", FamilyName = "Orbitron", FileName = "Orbitron.ttf" },
        new() { Id = "sharetech", DisplayName = "Share Tech Mono", FamilyName = "Share Tech Mono", FileName = "ShareTechMono-Regular.ttf" }
    ];

    /// <summary>
    /// 随程序发布的字体目录
    /// </summary>
    public static string BundledFontDirectory => Path.Combine(AppContext.BaseDirectory, "fonts");

    /// <summary>
    /// 自定义字体存放目录（用户导入的字体复制到这里，卸载程序也不会丢）
    /// </summary>
    public static string CustomFontDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClockLockScreen", "fonts");

    private static readonly Dictionary<string, FontFamily?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ClockFontPreset? Find(string? id) =>
        Embedded.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 解析出 WPF 能用的 FontFamily。自定义字体不可用时回落到默认内置字体。
    /// </summary>
    public static FontFamily Resolve(string? id, string? customPath, string? customFamily, out string displayName)
    {
        if (string.Equals(id, CustomId, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(customPath))
        {
            var resolved = TryResolveFile(customPath, customFamily);
            if (resolved != null)
            {
                displayName = string.IsNullOrWhiteSpace(customFamily)
                    ? Path.GetFileNameWithoutExtension(customPath)
                    : customFamily;
                return resolved;
            }
        }

        var preset = Find(id) ?? Find(DefaultId) ?? Embedded[0];
        displayName = preset.DisplayName;

        var bundled = LoadFromFile(Path.Combine(BundledFontDirectory, preset.FileName), preset.FamilyName);
        return bundled ?? new FontFamily("Segoe UI");
    }

    /// <summary>
    /// 从字体文件读家族名，用于导入时自动识别
    /// </summary>
    public static string? ReadFamilyName(string path)
    {
        try
        {
            var glyph = new GlyphTypeface(new Uri(path));
            return glyph.FamilyNames.Values.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 把用户选的字体文件复制到自定义字体目录，返回新路径
    /// </summary>
    public static string Import(string sourcePath)
    {
        var directory = CustomFontDirectory;
        Directory.CreateDirectory(directory);

        var name = Path.GetFileName(sourcePath);
        var target = Path.Combine(directory, name);
        var index = 1;
        while (File.Exists(target) && !string.Equals(target, sourcePath, StringComparison.OrdinalIgnoreCase))
        {
            target = Path.Combine(directory,
                $"{Path.GetFileNameWithoutExtension(name)}_{index++}{Path.GetExtension(name)}");
        }

        File.Copy(sourcePath, target, true);
        return target;
    }

    /// <summary>
    /// 从字体文件加载家族（WPF 只有 Fonts.GetFontFamilies 这条路能稳定拿到带文件的家族）
    /// </summary>
    private static FontFamily? LoadFromFile(string path, string? family)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var key = path + "#" + family;
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var families = Fonts.GetFontFamilies(path).ToList();
            var match = families.FirstOrDefault(f => !string.IsNullOrWhiteSpace(family)
                                                     && f.Source.Contains("#" + family, StringComparison.OrdinalIgnoreCase))
                        ?? families.FirstOrDefault();

            Cache[key] = match;
            return match;
        }
        catch
        {
            return null;
        }
    }

    private static FontFamily? TryResolveFile(string path, string? family)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var name = string.IsNullOrWhiteSpace(family) ? ReadFamilyName(path) : family;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return LoadFromFile(path, name);
        }
        catch
        {
            return null;
        }
    }
}
