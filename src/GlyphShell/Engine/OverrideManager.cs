using System.IO;
using GlyphShell.Engine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GlyphShell.Engine;

/// <summary>
/// Manages persistent user icon and color overrides stored in YAML.
/// Thread-safe singleton — call <see cref="Initialize"/> at startup.
/// </summary>
public static class OverrideManager
{
    private static readonly object _lock = new();
    private static bool _initialized;

    private static readonly string _configDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "GlyphShell");

    private static readonly string _filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "GlyphShell", "overrides.yaml");

    // Deserialized override entries: key → (iconName, colorHex)
    private static Dictionary<string, OverrideEntry> _extensions = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, OverrideEntry> _directories = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, OverrideEntry> _wellknown = new(StringComparer.OrdinalIgnoreCase);

    // Pre-resolved ANSI color sequences for fast lookup
    private static Dictionary<string, string?> _extensionColors = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, string?> _directoryColors = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, string?> _wellknownColors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes the override system by loading from disk.
    /// Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;
            Load();
            _initialized = true;
        }
    }

    /// <summary>
    /// Checks whether a user override exists for the given file system entry.
    /// </summary>
    public static bool TryResolveIcon(FileSystemInfo fileInfo, out string? iconName, out string? colorSeq)
    {
        iconName = null;
        colorSeq = null;

        lock (_lock)
        {
            bool isDirectory = fileInfo is DirectoryInfo;

            if (isDirectory)
            {
                if (_directories.TryGetValue(fileInfo.Name, out var entry))
                {
                    iconName = entry.Icon;
                    _directoryColors.TryGetValue(fileInfo.Name, out colorSeq);
                    return true;
                }
                return false;
            }

            // File: check well-known by exact name first
            if (_wellknown.TryGetValue(fileInfo.Name, out var wkEntry))
            {
                iconName = wkEntry.Icon;
                _wellknownColors.TryGetValue(fileInfo.Name, out colorSeq);
                return true;
            }

            // Then check by extension
            var ext = fileInfo.Extension;
            if (!string.IsNullOrEmpty(ext) && _extensions.TryGetValue(ext, out var extEntry))
            {
                iconName = extEntry.Icon;
                _extensionColors.TryGetValue(ext, out colorSeq);
                return true;
            }

            return false;
        }
    }

    /// <summary>Adds or updates an override entry and saves to disk.</summary>
    /// <param name="type">One of "extension", "directory", or "wellknown".</param>
    /// <param name="key">The extension (e.g. ".xyz"), directory name, or filename.</param>
    /// <param name="iconName">Nerd Font glyph name (e.g. "nf-md-file_code").</param>
    /// <param name="colorHex">Optional hex color (e.g. "#FF6600").</param>
    public static void AddOverride(string type, string key, string? iconName, string? colorHex)
    {
        lock (_lock)
        {
            var entry = new OverrideEntry { Icon = iconName, Color = colorHex };
            string? resolvedColor = null;
            if (!string.IsNullOrWhiteSpace(colorHex))
            {
                try { resolvedColor = ColorEngine.FromHex(colorHex); }
                catch { /* ignore invalid hex — store raw but no ANSI */ }
            }

            switch (type.ToLowerInvariant())
            {
                case "extension":
                    _extensions[key] = entry;
                    _extensionColors[key] = resolvedColor;
                    break;
                case "directory":
                    _directories[key] = entry;
                    _directoryColors[key] = resolvedColor;
                    break;
                case "wellknown":
                    _wellknown[key] = entry;
                    _wellknownColors[key] = resolvedColor;
                    break;
            }

            Save();
        }
    }

    /// <summary>Removes an override entry and saves to disk.</summary>
    public static void RemoveOverride(string type, string key)
    {
        lock (_lock)
        {
            switch (type.ToLowerInvariant())
            {
                case "extension":
                    _extensions.Remove(key);
                    _extensionColors.Remove(key);
                    break;
                case "directory":
                    _directories.Remove(key);
                    _directoryColors.Remove(key);
                    break;
                case "wellknown":
                    _wellknown.Remove(key);
                    _wellknownColors.Remove(key);
                    break;
            }

            Save();
        }
    }

    /// <summary>Returns all override entries grouped by type for display.</summary>
    public static IReadOnlyList<(string Type, string Key, string? Icon, string? Color)> GetAllOverrides()
    {
        lock (_lock)
        {
            var result = new List<(string, string, string?, string?)>();

            foreach (var (key, entry) in _extensions)
                result.Add(("Extension", key, entry.Icon, entry.Color));
            foreach (var (key, entry) in _directories)
                result.Add(("Directory", key, entry.Icon, entry.Color));
            foreach (var (key, entry) in _wellknown)
                result.Add(("WellKnown", key, entry.Icon, entry.Color));

            return result;
        }
    }

    // ───────────────────────── Persistence ─────────────────────────

    /// <summary>Saves current overrides to YAML on disk.</summary>
    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(_configDir);

            var model = new YamlOverrideFile
            {
                Extensions = _extensions.Count > 0 ? new Dictionary<string, OverrideEntry>(_extensions, StringComparer.OrdinalIgnoreCase) : null,
                Directories = _directories.Count > 0 ? new Dictionary<string, OverrideEntry>(_directories, StringComparer.OrdinalIgnoreCase) : null,
                Wellknown = _wellknown.Count > 0 ? new Dictionary<string, OverrideEntry>(_wellknown, StringComparer.OrdinalIgnoreCase) : null,
            };

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();

            File.WriteAllText(_filePath, serializer.Serialize(model));
        }
        catch
        {
            // Never crash the formatter — silently ignore I/O errors
        }
    }

    /// <summary>Loads overrides from YAML on disk. Tolerates missing or corrupt files.</summary>
    private static void Load()
    {
        _extensions = new(StringComparer.OrdinalIgnoreCase);
        _directories = new(StringComparer.OrdinalIgnoreCase);
        _wellknown = new(StringComparer.OrdinalIgnoreCase);
        _extensionColors = new(StringComparer.OrdinalIgnoreCase);
        _directoryColors = new(StringComparer.OrdinalIgnoreCase);
        _wellknownColors = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!File.Exists(_filePath)) return;

            var yaml = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(yaml)) return;

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var model = deserializer.Deserialize<YamlOverrideFile>(yaml);
            if (model is null) return;

            if (model.Extensions is not null)
            {
                foreach (var (key, entry) in model.Extensions)
                {
                    _extensions[key] = entry;
                    _extensionColors[key] = ResolveColor(entry.Color);
                }
            }

            if (model.Directories is not null)
            {
                foreach (var (key, entry) in model.Directories)
                {
                    _directories[key] = entry;
                    _directoryColors[key] = ResolveColor(entry.Color);
                }
            }

            if (model.Wellknown is not null)
            {
                foreach (var (key, entry) in model.Wellknown)
                {
                    _wellknown[key] = entry;
                    _wellknownColors[key] = ResolveColor(entry.Color);
                }
            }
        }
        catch
        {
            // Corrupt file — start fresh, never crash
        }
    }

    private static string? ResolveColor(string? colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex)) return null;
        try { return ColorEngine.FromHex(colorHex); }
        catch { return null; }
    }

    // ───────────────────────── YAML model ─────────────────────────

    private sealed class YamlOverrideFile
    {
        public Dictionary<string, OverrideEntry>? Extensions { get; set; }
        public Dictionary<string, OverrideEntry>? Directories { get; set; }
        public Dictionary<string, OverrideEntry>? Wellknown { get; set; }
    }

    /// <summary>Single override entry stored in YAML.</summary>
    public sealed class OverrideEntry
    {
        public string? Icon { get; set; }
        public string? Color { get; set; }
    }
}
