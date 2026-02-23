using System.Collections.Frozen;
using System.IO;
using GlyphShell.Data;
using GlyphShell.Themes;

namespace GlyphShell.Engine;

/// <summary>Result of resolving an icon for a file system entry.</summary>
public record struct ResolvedIcon(string? Glyph, string? ColorSequence, string? Target, string? Suffix = null, string? Badge = null);

/// <summary>
/// Resolves the appropriate Nerd Font icon for a given file or directory.
/// Lookup chain: symlink/junction → well-known filename → extension → multi-extension fallback → default.
/// Reads from <see cref="ThemeManager"/> for theme-aware resolution.
/// </summary>
public class IconResolver
{
    private readonly FrozenDictionary<string, string> _glyphs;

    /// <summary>Initializes a new resolver with the glyph database.</summary>
    public IconResolver()
    {
        _glyphs = BuiltInGlyphs.All;
        ThemeManager.Initialize();
        OverrideManager.Initialize();
    }

    /// <summary>
    /// Resolves the icon glyph, color, and symlink target for the given file system entry.
    /// </summary>
    public ResolvedIcon Resolve(FileSystemInfo fileInfo)
    {
        string? iconName = null;
        string? colorSeq = null;
        string? target = null;

        var iconTheme = ThemeManager.CurrentIconTheme;
        var colorTheme = ThemeManager.CurrentColorTheme;

        bool isDirectory = fileInfo is DirectoryInfo;

        // 0. User overrides (highest priority — always checked first)
        if (OverrideManager.TryResolveIcon(fileInfo, out var overrideIconName, out var overrideColorSeq))
        {
            iconName = overrideIconName;
            colorSeq = overrideColorSeq;
            iconName ??= isDirectory ? iconTheme.DefaultDirectoryIcon : iconTheme.DefaultFileIcon;
            colorSeq ??= isDirectory ? colorTheme.DefaultDirectoryColor : colorTheme.DefaultFileColor;
            // Try glyph dictionary first (named glyph), fall back to raw string (direct Unicode char)
            string? overrideGlyph = _glyphs.GetValueOrDefault(iconName) ?? iconName;
            return new ResolvedIcon(overrideGlyph, colorSeq, null);
        }

        // 0.5. Plugin overrides (checked after user overrides, before built-in chain)
        string? pluginSuffix = null;
        if (PluginManager.TryResolve(fileInfo, out var pluginResult))
        {
            pluginSuffix = pluginResult!.Suffix;
            if (pluginResult.IconName is not null || pluginResult.ColorSequence is not null)
            {
                iconName = pluginResult.IconName;
                colorSeq = pluginResult.ColorSequence;
                iconName ??= isDirectory ? iconTheme.DefaultDirectoryIcon : iconTheme.DefaultFileIcon;
                colorSeq ??= isDirectory ? colorTheme.DefaultDirectoryColor : colorTheme.DefaultFileColor;
                string? pluginGlyph = _glyphs.GetValueOrDefault(iconName);
                return new ResolvedIcon(pluginGlyph, colorSeq, null, pluginSuffix);
            }
        }

        // 1. Handle symlinks/junctions
        if (fileInfo.LinkTarget is not null)
        {
            iconName = isDirectory
                ? iconTheme.DirSymlinkIcon
                : iconTheme.FileSymlinkIcon;
            colorSeq = colorTheme.SymlinkColor;
            target = " \u2192 " + fileInfo.LinkTarget;
        }

        string? projectBadge = null;

        if (iconName is null)
        {
            if (isDirectory)
            {
                // 2. Well-known directory name lookup
                iconTheme.DirectoryNames.TryGetValue(fileInfo.Name, out iconName);
                colorTheme.DirectoryNames.TryGetValue(fileInfo.Name, out colorSeq);

                // 2.5. Project-type detection
                if (iconName is null)
                {
                    var projectResult = ProjectTypeDetector.Detect((DirectoryInfo)fileInfo);
                    if (projectResult is not null)
                    {
                        if (GlyphShellSettings.BadgeMerge)
                        {
                            // Merge: project icon replaces the folder icon
                            iconName = projectResult.Value.IconName;
                        }
                        else
                        {
                            // Separate: project icon goes to badge column
                            projectBadge = _glyphs.GetValueOrDefault(projectResult.Value.IconName);
                        }
                        colorSeq = projectResult.Value.ColorSequence;
                    }
                }
            }
            else
            {
                // 2. Well-known filename first
                if (iconTheme.WellKnownFiles.TryGetValue(fileInfo.Name, out iconName))
                {
                    colorTheme.WellKnownFiles.TryGetValue(fileInfo.Name, out colorSeq);
                }
                else
                {
                    // 3. Extension lookup (Path.GetExtension returns the last extension, e.g. ".js")
                    var ext = fileInfo.Extension;
                    if (!string.IsNullOrEmpty(ext))
                    {
                        iconTheme.FileExtensions.TryGetValue(ext, out iconName);
                        colorTheme.FileExtensions.TryGetValue(ext, out colorSeq);
                    }

                    // 4. Multi-extension fallback (e.g. ".test.js" tries full compound extension)
                    //    Skip if only one dot — single extension was already tried in step 3
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

        // 4.5. File type category color fallback (eza-style)
        if (colorSeq is null && !isDirectory)
        {
            colorSeq = FileTypeClassifier.GetCategoryColor(fileInfo.Extension);
        }

        // 5. Fallbacks
        iconName ??= isDirectory ? iconTheme.DefaultDirectoryIcon : iconTheme.DefaultFileIcon;
        colorSeq ??= isDirectory ? colorTheme.DefaultDirectoryColor : colorTheme.DefaultFileColor;

        // Resolve glyph name to actual Unicode character
        string? glyph = _glyphs.GetValueOrDefault(iconName);

        return new ResolvedIcon(glyph, colorSeq, target, pluginSuffix, projectBadge);
    }
}