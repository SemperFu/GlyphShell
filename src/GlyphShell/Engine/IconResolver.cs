using System.Collections.Frozen;
using System.IO;
using GlyphShell.Data;
using GlyphShell.Themes;

namespace GlyphShell.Engine;

public record struct ResolvedIcon(string? Glyph, string? ColorSequence, string? Target, string? Suffix = null);

public class IconResolver
{
    private readonly FrozenDictionary<string, string> _glyphs;

    public IconResolver()
    {
        _glyphs = BuiltInGlyphs.All;
        ThemeManager.Initialize();
        OverrideManager.Initialize();
    }

    public ResolvedIcon Resolve(FileSystemInfo fileInfo)
    {
        string? iconName = null;
        string? colorSeq = null;
        string? target = null;

        var iconTheme = ThemeManager.CurrentIconTheme;
        var colorTheme = ThemeManager.CurrentColorTheme;

        bool isDirectory = fileInfo is DirectoryInfo;

        // 0. User overrides
        if (OverrideManager.TryResolveIcon(fileInfo, out var overrideIconName, out var overrideColorSeq))
        {
            iconName = overrideIconName;
            colorSeq = overrideColorSeq;
            iconName ??= isDirectory ? iconTheme.DefaultDirectoryIcon : iconTheme.DefaultFileIcon;
            colorSeq ??= isDirectory ? colorTheme.DefaultDirectoryColor : colorTheme.DefaultFileColor;
            string? overrideGlyph = _glyphs.GetValueOrDefault(iconName) ?? iconName;
            return new ResolvedIcon(overrideGlyph, colorSeq, null);
        }

        // 1. Handle symlinks/junctions
        if (fileInfo.LinkTarget is not null)
        {
            iconName = isDirectory ? iconTheme.DirSymlinkIcon : iconTheme.FileSymlinkIcon;
            colorSeq = colorTheme.SymlinkColor;
            target = " \u2192 " + fileInfo.LinkTarget;
        }

        if (iconName is null)
        {
            if (isDirectory)
            {
                iconTheme.DirectoryNames.TryGetValue(fileInfo.Name, out iconName);
                colorTheme.DirectoryNames.TryGetValue(fileInfo.Name, out colorSeq);
            }
            else
            {
                if (iconTheme.WellKnownFiles.TryGetValue(fileInfo.Name, out iconName))
                {
                    colorTheme.WellKnownFiles.TryGetValue(fileInfo.Name, out colorSeq);
                }
                else
                {
                    var ext = fileInfo.Extension;
                    if (!string.IsNullOrEmpty(ext))
                    {
                        iconTheme.FileExtensions.TryGetValue(ext, out iconName);
                        colorTheme.FileExtensions.TryGetValue(ext, out colorSeq);
                    }

                    if (iconName is null)
                    {
                        var name = fileInfo.Name.AsSpan();
                        var firstDot = name.IndexOf('.');
                        if (firstDot >= 0 && name[(firstDot + 1)..].IndexOf('.') >= 0)
                        {
                            var fullExt = fileInfo.Name[firstDot..];
                            iconTheme.FileExtensions.TryGetValue(fullExt, out iconName);
                            colorTheme.FileExtensions.TryGetValue(fullExt, out colorSeq);
                        }
                    }
                }
            }
        }

        if (colorSeq is null && !isDirectory)
            colorSeq = FileTypeClassifier.GetCategoryColor(fileInfo.Extension);

        iconName ??= isDirectory ? iconTheme.DefaultDirectoryIcon : iconTheme.DefaultFileIcon;
        colorSeq ??= isDirectory ? colorTheme.DefaultDirectoryColor : colorTheme.DefaultFileColor;

        string? glyph = _glyphs.GetValueOrDefault(iconName);
        return new ResolvedIcon(glyph, colorSeq, target);
    }
}
