using System.IO;
using System.Management.Automation;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Sort field options for directory listings.
/// </summary>
public enum GlyphShellSortField
{
    Name,
    Size,
    Modified,
    Created,
    Extension,
    Type // directories vs files
}

/// <summary>
/// Sorts FileSystemInfo pipeline objects by specified criteria.
/// Usage: Get-ChildItem | Sort-GlyphShell -By Size -DirectoriesFirst -Descending
/// </summary>
[Cmdlet("Sort", "GlyphShell")]
[OutputType(typeof(FileSystemInfo))]
public class SortGlyphShellCmdlet : PSCmdlet
{
    private readonly List<FileSystemInfo> _items = new();

    /// <summary>Items to sort (from pipeline).</summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public FileSystemInfo InputObject { get; set; } = null!;

    /// <summary>Sort field. Defaults to Name.</summary>
    [Parameter(Position = 0)]
    public GlyphShellSortField By { get; set; } = GlyphShellSortField.Name;

    /// <summary>Place directories before files.</summary>
    [Parameter]
    public SwitchParameter DirectoriesFirst { get; set; }

    /// <summary>Reverse sort order.</summary>
    [Parameter]
    public SwitchParameter Descending { get; set; }

    protected override void ProcessRecord()
    {
        if (InputObject is not null)
            _items.Add(InputObject);
    }

    protected override void EndProcessing()
    {
        _items.Sort((a, b) =>
        {
            // Directories-first grouping
            if (DirectoriesFirst)
            {
                bool aDir = a is DirectoryInfo;
                bool bDir = b is DirectoryInfo;
                if (aDir != bDir) return bDir.CompareTo(aDir);
            }

            int cmp = By switch
            {
                GlyphShellSortField.Name => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase),
                GlyphShellSortField.Size => CompareSize(a, b),
                GlyphShellSortField.Modified => a.LastWriteTime.CompareTo(b.LastWriteTime),
                GlyphShellSortField.Created => a.CreationTime.CompareTo(b.CreationTime),
                GlyphShellSortField.Extension => string.Compare(a.Extension, b.Extension, StringComparison.OrdinalIgnoreCase),
                GlyphShellSortField.Type => CompareType(a, b),
                _ => 0,
            };

            return Descending ? -cmp : cmp;
        });

        foreach (var item in _items)
            WriteObject(item);
    }

    private static int CompareSize(FileSystemInfo a, FileSystemInfo b)
    {
        long sizeA = a is FileInfo fa ? fa.Length : 0;
        long sizeB = b is FileInfo fb ? fb.Length : 0;
        return sizeA.CompareTo(sizeB);
    }

    private static int CompareType(FileSystemInfo a, FileSystemInfo b)
    {
        bool aDir = a is DirectoryInfo;
        bool bDir = b is DirectoryInfo;
        if (aDir != bDir) return bDir.CompareTo(aDir);
        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
    }
}
