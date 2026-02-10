using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;

namespace GlyphShell.Engine;

/// <summary>Git file status categories used for display indicators.</summary>
public enum GitFileStatus
{
    /// <summary>Not in a git repo, or clean (no changes).</summary>
    None,
    /// <summary>Working tree modified.</summary>
    Modified,
    /// <summary>Staged new file.</summary>
    Added,
    /// <summary>Not tracked by git.</summary>
    Untracked,
    /// <summary>Renamed in index or working tree.</summary>
    Renamed,
    /// <summary>Deleted from index or working tree.</summary>
    Deleted,
    /// <summary>Listed in .gitignore.</summary>
    Ignored,
    /// <summary>Merge conflict.</summary>
    Conflicted,
    /// <summary>Modified and staged for commit.</summary>
    Staged,
}

/// <summary>Aggregate git status counts for files inside a directory.</summary>
public record struct GitDirSummary(int Modified, int Added, int Untracked, int Deleted, int Conflicted);

/// <summary>
/// Provides git status information using LibGit2Sharp.
/// Caches a single <see cref="RepositoryStatus"/> per repo root + queried parent directory
/// so that one <c>Get-ChildItem</c> invocation triggers at most one <c>RetrieveStatus()</c> call.
/// </summary>
public sealed class GitStatusProvider : IDisposable
{
    private Repository? _repo;
    private string? _repoRoot;
    private RepositoryStatus? _cachedStatus;
    private string? _cachedParentPath;

    // O(1) lookup indexes built when status is cached
    private Dictionary<string, FileStatus>? _fileStatusLookup;
    private Dictionary<string, List<FileStatus>>? _dirStatusLookup;

    /// <summary>Returns <c>true</c> if <paramref name="path"/> is inside a git repository.</summary>
    public bool IsInGitRepo(string path)
    {
        try
        {
            EnsureRepo(path);
            return _repo is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Returns the git status indicator for a single file or directory.</summary>
    public GitFileStatus GetFileStatus(string fullPath)
    {
        try
        {
            EnsureRepo(fullPath);
            if (_repo is null || _repoRoot is null) return GitFileStatus.None;

            var status = GetOrRefreshStatus(fullPath);
            if (status is null) return GitFileStatus.None;

            var relativePath = MakeRelative(fullPath, _repoRoot);
            if (relativePath is null) return GitFileStatus.None;

            // Check if fullPath is a directory — aggregate child statuses
            if (Directory.Exists(fullPath))
                return AggregateDirectoryStatus(relativePath);

            return MapStatus(relativePath);
        }
        catch
        {
            return GitFileStatus.None;
        }
    }

    /// <summary>Returns aggregate git status counts for all files under <paramref name="dirPath"/>.</summary>
    public GitDirSummary GetDirectorySummary(string dirPath)
    {
        try
        {
            EnsureRepo(dirPath);
            if (_repo is null || _repoRoot is null)
                return default;

            var status = GetOrRefreshStatus(dirPath);
            if (status is null) return default;

            var relDir = MakeRelative(dirPath, _repoRoot);
            if (relDir is null) return default;

            // Ensure trailing slash for prefix matching
            var prefix = relDir.Length == 0 ? "" : relDir.TrimEnd('/') + "/";

            if (_dirStatusLookup is null || !_dirStatusLookup.TryGetValue(prefix, out var entries))
                return default;

            int modified = 0, added = 0, untracked = 0, deleted = 0, conflicted = 0;

            foreach (var s in entries)
            {
                if (s.HasFlag(FileStatus.Conflicted))
                    conflicted++;
                else if (s.HasFlag(FileStatus.DeletedFromIndex) || s.HasFlag(FileStatus.DeletedFromWorkdir))
                    deleted++;
                else if (s.HasFlag(FileStatus.NewInIndex) || s.HasFlag(FileStatus.NewInWorkdir))
                {
                    if (s.HasFlag(FileStatus.NewInWorkdir) && !s.HasFlag(FileStatus.NewInIndex))
                        untracked++;
                    else
                        added++;
                }
                else if (s.HasFlag(FileStatus.ModifiedInIndex) || s.HasFlag(FileStatus.ModifiedInWorkdir)
                      || s.HasFlag(FileStatus.RenamedInIndex) || s.HasFlag(FileStatus.RenamedInWorkdir)
                      || s.HasFlag(FileStatus.TypeChangeInIndex) || s.HasFlag(FileStatus.TypeChangeInWorkdir))
                    modified++;
            }

            return new GitDirSummary(modified, added, untracked, deleted, conflicted);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>Clears the cached status so the next call re-queries the repository.</summary>
    public void InvalidateCache()
    {
        _cachedStatus = null;
        _cachedParentPath = null;
        _fileStatusLookup = null;
        _dirStatusLookup = null;
    }

    public void Dispose()
    {
        _repo?.Dispose();
        _repo = null;
        _repoRoot = null;
        _cachedStatus = null;
        _cachedParentPath = null;
        _fileStatusLookup = null;
        _dirStatusLookup = null;
    }

    // ── Private helpers ─────────────────────────────────────────────────

    private void EnsureRepo(string path)
    {
        // If we already have a repo and the path is under the same root, keep it.
        if (_repo is not null && _repoRoot is not null
            && path.StartsWith(_repoRoot, StringComparison.OrdinalIgnoreCase))
            return;

        // Discover repo root by walking parent directories.
        var discovered = Repository.Discover(path);
        if (discovered is null)
        {
            _repo?.Dispose();
            _repo = null;
            _repoRoot = null;
            InvalidateCache();
            return;
        }

        var newRoot = new Repository(discovered).Info.WorkingDirectory;
        if (newRoot is null)
        {
            // Bare repo — no working tree statuses.
            _repo?.Dispose();
            _repo = null;
            _repoRoot = null;
            InvalidateCache();
            return;
        }

        // Normalize root path (remove trailing separator for consistent StartsWith checks)
        newRoot = newRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!string.Equals(_repoRoot, newRoot, StringComparison.OrdinalIgnoreCase))
        {
            _repo?.Dispose();
            _repo = new Repository(discovered);
            _repoRoot = newRoot;
            InvalidateCache();
        }
    }

    private RepositoryStatus? GetOrRefreshStatus(string queryPath)
    {
        if (_repo is null) return null;

        // Determine the parent directory being listed.
        var parentDir = Directory.Exists(queryPath)
            ? Path.GetDirectoryName(queryPath) ?? queryPath
            : Path.GetDirectoryName(queryPath);

        // Cache hit — same parent directory means same Get-ChildItem invocation.
        if (_cachedStatus is not null
            && string.Equals(_cachedParentPath, parentDir, StringComparison.OrdinalIgnoreCase))
            return _cachedStatus;

        _cachedStatus = _repo.RetrieveStatus(new StatusOptions
        {
            IncludeUntracked = true,
            RecurseUntrackedDirs = true,
            IncludeIgnored = false, // skip ignored for perf
        });
        _cachedParentPath = parentDir;
        BuildLookups(_cachedStatus);
        return _cachedStatus;
    }

    /// <summary>
    /// Builds O(1) lookup dictionaries from the cached RepositoryStatus.
    /// _fileStatusLookup: relative file path → FileStatus (for single-file lookups).
    /// _dirStatusLookup: directory prefix → list of FileStatus values for all entries under that directory.
    /// </summary>
    private void BuildLookups(RepositoryStatus status)
    {
        var fileLookup = new Dictionary<string, FileStatus>(StringComparer.OrdinalIgnoreCase);
        var dirLookup = new Dictionary<string, List<FileStatus>>(StringComparer.OrdinalIgnoreCase);

        // Root key "" collects all entries (for repo-root directory queries)
        var rootList = new List<FileStatus>();
        dirLookup[""] = rootList;

        foreach (var entry in status)
        {
            // Index by normalized file path (trimmed trailing slash)
            var filePath = entry.FilePath.TrimEnd('/');
            fileLookup[filePath] = entry.State;

            // Every entry belongs to the root
            rootList.Add(entry.State);

            // Add to each ancestor directory prefix
            var path = entry.FilePath;
            int idx = 0;
            while ((idx = path.IndexOf('/', idx)) >= 0)
            {
                var dirPrefix = path[..(idx + 1)]; // includes trailing '/'
                if (!dirLookup.TryGetValue(dirPrefix, out var list))
                {
                    list = new List<FileStatus>();
                    dirLookup[dirPrefix] = list;
                }
                list.Add(entry.State);
                idx++;
            }
        }

        _fileStatusLookup = fileLookup;
        _dirStatusLookup = dirLookup;
    }

    private GitFileStatus MapStatus(string relativePath)
    {
        if (_fileStatusLookup is null)
            return GitFileStatus.None;

        if (!_fileStatusLookup.TryGetValue(relativePath, out var state))
            return GitFileStatus.None;

        if (state == FileStatus.Unaltered || state == FileStatus.Nonexistent)
            return GitFileStatus.None;

        if (state.HasFlag(FileStatus.Conflicted))
            return GitFileStatus.Conflicted;
        if (state.HasFlag(FileStatus.RenamedInIndex) || state.HasFlag(FileStatus.RenamedInWorkdir))
            return GitFileStatus.Renamed;
        if (state.HasFlag(FileStatus.DeletedFromIndex) || state.HasFlag(FileStatus.DeletedFromWorkdir))
            return GitFileStatus.Deleted;
        if (state.HasFlag(FileStatus.NewInWorkdir))
            return GitFileStatus.Untracked;
        if (state.HasFlag(FileStatus.NewInIndex))
            return GitFileStatus.Added;
        if (state.HasFlag(FileStatus.ModifiedInIndex))
            return GitFileStatus.Staged;
        if (state.HasFlag(FileStatus.ModifiedInWorkdir))
            return GitFileStatus.Modified;

        return GitFileStatus.None;
    }

    private GitFileStatus AggregateDirectoryStatus(string relDir)
    {
        if (_dirStatusLookup is null)
            return GitFileStatus.None;

        var prefix = relDir.Length == 0 ? "" : relDir.TrimEnd('/') + "/";

        if (!_dirStatusLookup.TryGetValue(prefix, out var entries))
            return GitFileStatus.None;

        GitFileStatus worst = GitFileStatus.None;
        foreach (var state in entries)
        {
            var mapped = MapSingleState(state);
            if (mapped > worst) worst = mapped;
            if (worst == GitFileStatus.Conflicted) break; // can't get worse
        }

        return worst;
    }

    private static GitFileStatus MapSingleState(FileStatus state)
    {
        if (state.HasFlag(FileStatus.Conflicted)) return GitFileStatus.Conflicted;
        if (state.HasFlag(FileStatus.DeletedFromIndex) || state.HasFlag(FileStatus.DeletedFromWorkdir)) return GitFileStatus.Deleted;
        if (state.HasFlag(FileStatus.ModifiedInWorkdir)) return GitFileStatus.Modified;
        if (state.HasFlag(FileStatus.ModifiedInIndex)) return GitFileStatus.Staged;
        if (state.HasFlag(FileStatus.RenamedInIndex) || state.HasFlag(FileStatus.RenamedInWorkdir)) return GitFileStatus.Renamed;
        if (state.HasFlag(FileStatus.NewInWorkdir)) return GitFileStatus.Untracked;
        if (state.HasFlag(FileStatus.NewInIndex)) return GitFileStatus.Added;
        return GitFileStatus.None;
    }

    private static string? MakeRelative(string fullPath, string repoRoot)
    {
        // Normalize separators to forward slashes (git convention)
        var normalizedPath = fullPath.Replace('\\', '/').TrimEnd('/');
        var normalizedRoot = repoRoot.Replace('\\', '/').TrimEnd('/');

        if (normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return "";

        if (!normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
            return null;

        return normalizedPath[(normalizedRoot.Length + 1)..];
    }
}
