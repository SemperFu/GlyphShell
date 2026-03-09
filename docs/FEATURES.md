# GlyphShell Features

Detailed reference for every GlyphShell feature. For a quick overview, see the [README](../README.md).

---

## Git Status Integration

GlyphShell can show git status directly in your directory listing via LibGit2Sharp (no shelling out to `git.exe`). **Git status is off by default** for performance. When enabled, a **Git** column appears between Mode and LastWriteTime:

```powershell
Set-GlyphShellOption -GitStatus           # enable (adds Git column)
Set-GlyphShellOption -GitStatus:$false    # disable (removes Git column)
```

| Indicator | Meaning | Color |
| --- | --- | --- |
| `M` | Modified (working tree) | Red |
| `A` | Added / Staged | Green |
| `?` | Untracked | Gray |
| `R` | Renamed | Blue |
| `D` | Deleted | Dark red |
| `I` | Ignored | Dark gray |
| `C` | Merge conflict | Yellow |
| `S` | Staged (index) | Green |

For directories, the indicator shows the worst child status (e.g., if any file inside is modified, the directory shows `M`).

Performance: one `RetrieveStatus()` call per listing, not per file. Results are cached per repo root. Outside a git repo, the column is empty with zero overhead.

## Tree View

Render directory trees with icons, colors, and Unicode box-drawing characters:

```powershell
# Show tree of current directory (default depth: 3)
Show-GlyphShellTree

# Custom path and depth
Show-GlyphShellTree .\src -Depth 5

# Include hidden files
Show-GlyphShellTree -ShowHidden

# Files before directories
Show-GlyphShellTree -FilesFirst

# Show file and folder sizes (folders show recursive total)
Show-GlyphShellTree -Size
```

Alias: `gstree`

The `-Size` switch computes sizes for every entry: files show their individual size, folders show the recursive total of all files inside. Output streams line-by-line as sizes are computed — the root line appears instantly, with no upfront full-tree scan. The colored summary footer shows directory count, file count, and total size.

## Grid View

Display directory contents in a responsive grid layout, like `eza --grid`:

```powershell
# Grid view of current directory
Show-GlyphShellGrid

# With directories first
Show-GlyphShellGrid -DirectoriesFirst

# Include hidden files
Show-GlyphShellGrid -ShowHidden
```

Alias: `gsgrid`

The grid automatically adjusts column count based on your terminal width.

## Themes

GlyphShell supports customizable icon and color themes in YAML format, with backward compatibility for Terminal-Icons `.psd1` themes:

```powershell
# See current theme
Get-GlyphShellTheme

# Switch themes
Set-GlyphShellTheme -IconTheme dracula -ColorTheme dracula

# Preview a theme
Show-GlyphShellTheme

# Add a custom YAML theme
Add-GlyphShellIconTheme -Path .\my-icons.yaml
Add-GlyphShellColorTheme -Path .\my-colors.yaml

# Import existing Terminal-Icons themes
Import-GlyphShellLegacyThemes
```

YAML theme format:

```yaml
name: my-theme
type: icon
extensions:
  .cs: nf-dev-dotnet
  .js: nf-dev-javascript
directories:
  .git: nf-custom-folder_git
wellknown:
  Dockerfile: nf-dev-docker
defaults:
  file: nf-fa-file
  directory: nf-oct-file_directory
```

### Theme Export

Export your current theme configuration to shareable YAML files:

```powershell
# Export both icon and color themes
Export-GlyphShellTheme -Path .\my-theme.yaml

# Export icon theme only
Export-GlyphShellTheme -IconOnly

# Export color theme only
Export-GlyphShellTheme -ColorOnly
```

GlyphShell ships with 3 built-in color themes (plus the default):

```powershell
Set-GlyphShellTheme -ColorTheme dracula
Set-GlyphShellTheme -ColorTheme catppuccin
Set-GlyphShellTheme -ColorTheme onedark
```

## Custom Icons (User Overrides)

One of the biggest frustrations with Terminal-Icons is that it's been abandoned since late 2023. If your file type isn't supported, your only option is to open a pull request and hope the maintainer merges it. They won't. There are PRs sitting untouched for years, and users are stuck with missing icons for `.astro`, `.svelte`, `.prisma`, and dozens of other modern file types.

GlyphShell takes a different approach: **you're in control.** One command adds any icon you want, instantly, and it persists across sessions:

```powershell
# Add a custom icon for any file extension
Add-GlyphShellIcon -Extension .xyz -Icon nf-fa-file_code_o -Color "#FF6600"

# Add a custom directory icon
Add-GlyphShellIcon -Directory my-special-dir -Icon nf-oct-star -Color "#FFD700"

# Add a well-known filename icon
Add-GlyphShellIcon -WellKnown .myconfig -Icon nf-fa-cog -Color "#888888"

# See all your overrides
Get-GlyphShellOverrides

# Remove one
Remove-GlyphShellIcon -Extension .xyz
```

Overrides are saved to `~/.config/GlyphShell/overrides.yaml` and stack on top of any active theme. Switch themes, update GlyphShell, your overrides stay. If a file type is popular enough, we'll add it to the built-in defaults in a future release, but you never have to wait for that.

## File Type Categories

GlyphShell classifies files into 16 eza-style categories, each with a distinct default color. When a file extension doesn't have a specific color in the active theme, the category color is used as a fallback instead of plain white:

| Category | Example extensions | Color |
| --- | --- | --- |
| Source | .cs, .js, .ts, .py, .go, .rs, .java | Cyan |
| Markup | .html, .jsx, .tsx, .vue, .svelte | Orange |
| Style | .css, .scss, .sass, .less | Pink |
| Data | .json, .yaml, .toml, .env | Yellow |
| Document | .md, .txt, .pdf, .doc | White |
| Image | .png, .jpg, .gif, .svg, .webp | Magenta |
| Video | .mp4, .mkv, .mov, .avi | Purple |
| Audio | .mp3, .wav, .flac, .ogg | Light purple |
| Archive | .zip, .tar, .gz, .7z, .rar | Red |
| Binary | .exe, .dll, .wasm, .so | Dark red |
| Build | .sln, .csproj, .cmake, .targets | Gold |
| Crypto | .pem, .key, .crt, .pfx | Orange-red |
| Font | .ttf, .otf, .woff, .woff2 | Teal |
| Database | .sql, .db, .sqlite | Blue |
| Config | .gitignore, .editorconfig | Gray |
| Temp | .tmp, .bak, .log, .cache | Dark gray |

## Colored Display

The default `Get-ChildItem` output now includes colored file sizes, mode flags, and timestamps with no extra commands needed:

**File sizes** scale from green (tiny) through yellow (medium) to red (huge), with human-readable units (1.2K, 3.4M, 2.1G).

**Mode flags** are individually colored: `d` (directory) is blue, `a` (archive) is green, `r` (read-only) is yellow, `h` (hidden) is gray, `s` (system) is red, and `-` (unset) is dark gray.

**Timestamps** are colored by age: bright green for files modified in the last hour, fading through green → yellow → dim → gray for older files. This makes it easy to spot recently changed files at a glance.

To switch to a flat date color (like eza's blue):

```powershell
Set-GlyphShellOption -DateAge:$false          # flat blue (default)
Set-GlyphShellOption -DateColor '#FF8800'     # custom flat color
Set-GlyphShellOption -DateAge                 # re-enable age gradient
```

## Project-Type Detection

**This is the feature that started the whole GlyphShell project.** When enabled, directories show a project-specific icon based on marker files inside them. A folder containing `Cargo.toml` shows the Rust crab in orange, a folder with `package.json` shows the Node.js icon in green, and so on — no configuration needed.

By default, **BadgeMerge** is enabled: the project icon **replaces** the folder icon directly in the Icon column — you see a single Rust crab icon instead of a generic folder icon plus a separate badge. This keeps the display clean with one icon per row. To get the original separate Badge column (next to the Icon column), disable BadgeMerge:

```powershell
Set-GlyphShellOption -BadgeMerge:$false   # separate Badge column (original behavior)
Set-GlyphShellOption -BadgeMerge          # merge back into Icon column (default)
```

The Badge column only appears when project detection is enabled AND BadgeMerge is disabled.

| Marker Files | Project Type | Color |
| --- | --- | --- |
| `*.csproj`, `*.fsproj`, `*.sln`, `*.slnx` | .NET/C# | Purple |
| `tsconfig.json` | TypeScript | Blue |
| `package.json` | Node.js | Green |
| `go.mod` | Go | Cyan |
| `Cargo.toml` | Rust | Orange |
| `pyproject.toml`, `requirements.txt`, `setup.py`, `Pipfile` | Python | Yellow |
| `Gemfile` | Ruby | Red |
| `pom.xml`, `build.gradle`, `build.gradle.kts` | Java/Kotlin | Orange |
| `composer.json` | PHP | Indigo |
| `pubspec.yaml` | Dart/Flutter | Blue |
| `Package.swift` | Swift | Orange |
| `CMakeLists.txt`, `Makefile` | C/C++ | Blue |
| `Dockerfile`, `docker-compose.yml` | Docker | Blue |
| `ProjectSettings/` directory | Unity | Gray |
| `*.uproject` | Unreal Engine | Gray |

When multiple markers are present (e.g. `package.json` + `tsconfig.json`), the more specific type wins (TypeScript beats Node.js).

Detection is on by default. Toggling these settings regenerates the format file dynamically:

```powershell
Set-GlyphShellOption -ProjectDetection:$false  # disable project detection entirely
Set-GlyphShellOption -ProjectDetection         # re-enable project detection
Set-GlyphShellOption -BadgeMerge:$false        # show badges in a separate Badge column
Set-GlyphShellOption -BadgeMerge               # merge project icons into the Icon column (default)
```

The Badge column requires both `-ProjectDetection` enabled AND `-BadgeMerge:$false`.

## Content-Type Directory Detection

Beyond programming projects, GlyphShell also detects **content-type directories** — folders that primarily contain media or files of a specific category. These badges also appear in the Badge column:

| Content Type | Well-Known Names | Detected By Extensions |
| --- | --- | --- |
| 🎵 Music | Music, Songs, Audio, Podcasts, Soundtracks, Playlists | .mp3, .flac, .wav, .ogg, .aac, .wma, .m4a, .opus |
| 📷 Photos | Photos, Pictures, Images, Screenshots, Wallpapers, DCIM | .jpg, .jpeg, .png, .bmp, .raw, .tiff, .webp, .gif, .heic |
| 🎬 Video | Videos, Movies, Films, Clips, Recordings, Screencasts | .mp4, .avi, .mkv, .mov, .wmv, .flv, .webm |
| 📄 Documents | Documents, Docs, Papers, PDFs, Reports, Invoices | .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx |
| 🔤 Fonts | Fonts, Typography | .ttf, .otf, .woff, .woff2, .eot |
| 🧊 3D Models | 3D, Models, Blender, CAD | .obj, .fbx, .stl, .blend, .gltf, .glb |
| 📚 Ebooks | Books, Ebooks, Library, Kindle | .epub, .mobi, .azw, .cbr, .cbz |
| 🎮 Games | Games, ROMs, ISOs, Emulators | (name-based only) |
| 🎨 Design | Design, Creative, Artwork, Art, Graphics, Mockups | .ai, .sketch, .fig, .xd, .indd |
| 🗃️ Data | Data, Datasets, Databases | .csv, .tsv, .sqlite, .db, .parquet |

Detection works three ways (in priority order):
1. **Programming markers** always win (e.g. `package.json` → Node.js, even if folder is named "Music")
2. **Well-known directory names** match instantly (e.g. a folder named "Photos")
3. **Content sniffing** — if ≥60% of files match one category (min 3 files, max 100 scanned)

## Plugin System

Extend GlyphShell with custom logic via ScriptBlock plugins.

**⚠️ Security Model:** The plugin system is **disabled by default**. Plugins execute code on every file listing, so enabling requires explicit confirmation:

```powershell
# Enable the plugin system (shows security warning, requires confirmation)
Set-GlyphShellOption -Plugins

# Disable and unregister all custom plugins
Set-GlyphShellOption -Plugins:$false
```

**Sandboxed execution:** Custom ScriptBlock plugins run in a constrained runspace that blocks:
- ❌ File writes (`Remove-Item`, `Set-Content`, `New-Item`, `Copy-Item`, etc.)
- ❌ Network access (`Invoke-WebRequest`, `Invoke-RestMethod`)
- ❌ Process execution (`Start-Process`, `Invoke-Expression`, `Invoke-Command`)
- ❌ Module loading (`Import-Module`, `Add-Type`)
- ✅ File reading, string operations, math — everything needed to inspect files

```powershell
# Register a custom plugin (requires plugin system enabled)
Register-GlyphShellPlugin -Name MyPlugin -Resolve {
    param($item)
    if ($item.Extension -eq '.custom') {
        @{ Icon = 'nf-fa-file_code_o'; Color = '#FF6600'; Suffix = ' (custom!)' }
    }
}

# List registered plugins
Get-GlyphShellPlugins

# Unregister
Unregister-GlyphShellPlugin -Name MyPlugin
```

Plugins run at step 0.5 in the resolve chain (after user overrides, before built-in resolution). The first plugin to return a non-null result wins.

**Built-in File Preview Plugin** (always available — compiled C#, not sandboxed):

```powershell
# Enable the built-in file preview plugin (no plugin system required)
Enable-GlyphShellPlugin -Name FilePreview

# Text files show line counts, images/archives show type tags
# Disable when done
Disable-GlyphShellPlugin -Name FilePreview
```


