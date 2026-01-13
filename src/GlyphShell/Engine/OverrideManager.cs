namespace GlyphShell.Engine;

public static class OverrideManager
{
    private static bool _initialized;

    public static void Initialize()
    {
        _initialized = true;
    }

    public static bool TryResolveIcon(System.IO.FileSystemInfo fileInfo, out string? iconName, out string? colorSeq)
    {
        iconName = null;
        colorSeq = null;
        return false;
    }
}
