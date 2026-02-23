using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GlyphShell.Engine;

/// <summary>Result returned by a plugin resolver.</summary>
public record PluginResult(string? IconName, string? ColorSequence, string? Suffix);

/// <summary>
/// Manages registered plugins that can override icon resolution.
/// Thread-safe; plugins are evaluated in registration order.
/// </summary>
public static class PluginManager
{
    private static readonly object _lock = new();
    private static readonly List<(string Name, Func<FileSystemInfo, PluginResult?> Resolve)> _plugins = new();
    private static Func<FileSystemInfo, PluginResult?>[]? _snapshot;

    /// <summary>Register a plugin with the given name and resolver function.</summary>
    public static void Register(string name, Func<FileSystemInfo, PluginResult?> resolver)
    {
        lock (_lock)
        {
            // Replace if same name already registered
            _plugins.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            _plugins.Add((name, resolver));
            _snapshot = null;
        }
    }

    /// <summary>Unregister a plugin by name.</summary>
    public static void Unregister(string name)
    {
        lock (_lock)
        {
            _plugins.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            _snapshot = null;
        }
    }

    /// <summary>Returns the names of all registered plugins in registration order.</summary>
    public static string[] GetAll()
    {
        lock (_lock)
        {
            return _plugins.Select(p => p.Name).ToArray();
        }
    }

    /// <summary>
    /// Iterates all plugins in registration order; returns the first non-null result.
    /// </summary>
    public static bool TryResolve(FileSystemInfo fileInfo, out PluginResult? result)
    {
        // Fast path: skip locking and allocation when no plugins registered
        if (_plugins.Count == 0) { result = null; return false; }

        // Use cached snapshot; rebuild only when invalidated by Register/Unregister
        var snapshot = _snapshot;
        if (snapshot is null)
        {
            lock (_lock)
            {
                snapshot = _snapshot;
                if (snapshot is null)
                {
                    snapshot = _plugins.Select(p => p.Resolve).ToArray();
                    _snapshot = snapshot;
                }
            }
        }

        foreach (var resolve in snapshot)
        {
            try
            {
                var r = resolve(fileInfo);
                if (r is not null)
                {
                    result = r;
                    return true;
                }
            }
            catch
            {
                // Plugin threw — skip it silently
            }
        }

        result = null;
        return false;
    }
}
