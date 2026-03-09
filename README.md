# GlyphShell

[![Build](https://github.com/SemperFu/GlyphShell/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/SemperFu/GlyphShell/actions/workflows/build-and-test.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![PowerShell 7.5+](https://img.shields.io/badge/PowerShell-7.5%2B-blue.svg)](https://github.com/PowerShell/PowerShell)

**A high-performance PowerShell module for file icons, git status, and rich directory listings.**

Built in C# targeting .NET 9 and .NET 10. Inspired by [Terminal-Icons](https://github.com/devblackops/Terminal-Icons) and [eza](https://github.com/eza-community/eza).

<p align="center">
  <a href="https://semperfu.github.io/GlyphShell/">
    <img src="GlyphDemo.gif" alt="GlyphShell demo - click for interactive version" width="700">
  </a>
  <br>
  <sub>▶ Click the GIF for an <a href="https://semperfu.github.io/GlyphShell/">interactive demo</a></sub>
</p>

---

## Installation

```powershell
Install-Module GlyphShell -Scope CurrentUser
Import-Module GlyphShell

# Add to your profile so it loads every session
Add-Content $PROFILE "`nImport-Module GlyphShell"
```

**Requirements:** PowerShell 7.5+ and a [Nerd Font](https://www.nerdfonts.com/) (e.g., Cascadia Code NF).

> **Like how the terminal looks?** Check out [TerminalBootstrap](https://github.com/SemperFu/TerminalBootstrap) for a quick-start PowerShell profile that sets up Nerd Fonts, Oh My Posh, GlyphShell, and more.

> **Note:** GlyphShell requires .NET 9+ APIs (`FrozenDictionary`, `IModuleAssemblyInitializer`, etc.) that don't exist in .NET Framework 4.x, so PowerShell 5.1 is not supported.

## Quick Start

GlyphShell hooks into `Get-ChildItem` automatically - just run `dir` or `ls` and you'll see icons and colors:

```powershell
dir                                         # icons + colors, just like normal
gstree                                      # tree view with icons
gstree -Size                                # tree view with file/folder sizes
gsgrid                                      # grid view, auto-fit to terminal width

Set-GlyphShellOption -GitStatus             # enable per-file git indicators (M/A/?/R/D)
Set-GlyphShellOption -ProjectDetection      # detect project types (dotnet, node, rust...)

dir | Select Icon, GlyphName                # colored icon + colored name in pipeline
Select-GlyphShell -Path . -Recurse          # standalone listing with clean column headers
```

Objects stay in the pipeline as real .NET `FileInfo`/`DirectoryInfo` - icons are display-only.

## Features

> **Full details:** [docs/FEATURES.md](docs/FEATURES.md)

| Feature | Highlights |
| --- | --- |
| **900+ icon mappings** | Extensions, well-known files, directories - all compiled into a `FrozenDictionary` |
| **Git status** | Per-file indicators via LibGit2Sharp, cached per repo. Off by default - `Set-GlyphShellOption -GitStatus` |
| **Tree & grid views** | `gstree` / `gsgrid` - icons, colors, box-drawing, depth control, auto-fit columns, `-Size` for disk usage |
| **8 built-in themes** *(preview)* | Dracula, Catppuccin, One Dark, Gruvbox, Nord, Tokyo Night, Solarized Dark, Monokai - plus YAML export/import for custom themes. Theme switching is a preview feature and needs further work |
| **Custom icon overrides** | `Add-GlyphShellIcon` - persists to `~/.config/GlyphShell/overrides.yaml` |
| **16 file categories** | eza-style fallback colors (Source, Image, Archive, etc.) |
| **Colored display** | Sizes (green→red), mode flags (per-char), timestamps (age gradient) |
| **Project detection** | 15 project types + 10 content types - project icons merge into folder icon by default |
| **Plugin system** | Sandboxed ScriptBlock plugins, disabled by default, built-in FilePreview plugin |
| **Theme generator** *(preview)* | Create themes from ~20 category colors instead of 900+ individual mappings |
| **Theme preview** *(preview)* | `Select-GlyphShell -Theme dracula` to preview without switching |

## Benchmarks

Real-world results across different directory sizes (PowerShell 7.5.4, Windows 11):

| Metric | Terminal-Icons | GlyphShell | Speedup |
| --- | --- | --- | --- |
| Module import | ~580 ms | ~265 ms | **2.2×** |
| Per-file (84 files) | ~576 μs | ~107 μs | **5.4×** |
| Per-file (6,000 files) | ~334 μs | ~42 μs | **7.9×** |
| Per-file (100,000 files) | ~348 μs | ~42 μs | **8.4×** |
| 100k directory | 34.8 s | 4.2 s | **8.4×** |

Full pipeline = `Get-ChildItem | Format-Table` with all columns colored, the real user experience. GlyphShell uses CodeProperty-backed format columns that call C# directly, bypassing PowerShell ScriptBlock overhead. At scale (6k+ files), per-file cost drops to ~42µs as Format-Table's fixed overhead is amortized.

```powershell
.\Benchmark-GlyphShell.ps1            # GlyphShell only
.\Benchmark-GlyphShell.ps1 -Compare   # side-by-side with Terminal-Icons
```

## Why GlyphShell?

[Terminal-Icons](https://github.com/devblackops/Terminal-Icons) proved that PowerShell format hooks could deliver a great icon experience, but it was written entirely in PowerShell, loading a 571 KB glyph script on every import (2-4 seconds) and is no longer actively maintained. [eza](https://github.com/eza-community/eza) showed what a modern directory listing could look like (git status, tree views, color themes) but outputs plain text with no PowerShell pipeline integration.

GlyphShell combines both: Terminal-Icons' seamless `Get-ChildItem` integration with eza's feature set, rewritten in C# for native .NET performance.

| Feature | Terminal-Icons | eza | **GlyphShell** |
| --- | --- | --- | --- |
| Language | PowerShell | Rust | **C# (.NET 9/.NET 10)** |
| Icon mappings | ~300 | ~330 | **900+** |
| Import time | ~3 seconds | N/A (binary) | **< 100 ms** |
| Hooks into Get-ChildItem | ✅ | ❌ | **✅** |
| PowerShell pipeline | ✅ Objects | ❌ Text | **✅ Objects** |
| Git status | ❌ | ✅ | **✅** |
| Tree / grid views | ❌ | ✅ | **✅** |
| User-defined icons | ❌ | ❌ | **✅** |
| File type categories | ❌ | ✅ | **✅** |
| Colored sizes / mode / dates | ❌ | Partial | **✅** |
| Project-type detection | ❌ | ❌ | **✅** |
| Plugin system | ❌ | ❌ | **✅** |
| Theme export | ❌ | ❌ | **✅** |
| Built-in themes | 2 | 0 | **8** |
| Actively maintained | ❌ | ✅ | **✅** |

## Commands

28 cmdlets + 2 aliases. Run `Show-GlyphShell -Help` for a full categorized reference.

### Display

| Command | Description |
| --- | --- |
| `Show-GlyphShell` | Version, glyph count, runtime info. `-Help` for command reference |
| `Show-GlyphShellGrid` | Grid view (alias: `gsgrid`) |
| `Show-GlyphShellTree` | Tree view (alias: `gstree`). `-Size` shows file/folder sizes |
| `Select-GlyphShell` | Standalone listing with clean headers. `-Property` to pick columns, `-Theme` to preview a theme |

### Themes

| Command | Description |
| --- | --- |
| `Get-GlyphShellTheme` | Show active themes |
| `Set-GlyphShellTheme` | Switch themes |
| `Show-GlyphShellTheme` | Preview the current theme |
| `Add-GlyphShellIconTheme` | Register custom YAML icon theme |
| `Add-GlyphShellColorTheme` | Register custom YAML color theme |
| `Remove-GlyphShellTheme` | Unregister a theme |
| `Export-GlyphShellTheme` | Export to YAML (`-IconOnly`, `-ColorOnly`) |
| `New-GlyphShellTheme` | Generate a theme from category colors |

### Icons & Overrides

| Command | Description |
| --- | --- |
| `Add-GlyphShellIcon` | Add custom icon override (by extension, directory, or well-known name) |
| `Remove-GlyphShellIcon` | Remove override |
| `Get-GlyphShellOverrides` | List all overrides |

### Plugins

| Command | Description |
| --- | --- |
| `Register-GlyphShellPlugin` | Register sandboxed ScriptBlock plugin |
| `Unregister-GlyphShellPlugin` | Remove plugin |
| `Get-GlyphShellPlugins` | List plugins |
| `Enable-GlyphShellPlugin` | Enable built-in plugin (e.g. `FilePreview`) |
| `Disable-GlyphShellPlugin` | Disable plugin |

### Utilities

| Command | Description |
| --- | --- |
| `Set-GlyphShellOption` | Runtime flags: `-Diagnostics`, `-DateAge`, `-DateColor`, `-ProjectDetection`, `-GitStatus`, `-Plugins`, `-BadgeMerge` |

<details>
<summary>Format cmdlets (called automatically by Get-ChildItem)</summary>

These power the columns in `dir` output - you don't need to call them directly:

`Format-GlyphShell` · `Format-GlyphShellIcon` · `Format-GlyphShellBadge` · `Format-GlyphShellDate` · `Format-GlyphShellGit` · `Format-GlyphShellMode` · `Format-GlyphShellSize`

</details>

## Migrating from Terminal-Icons

```powershell
Remove-Module Terminal-Icons
Uninstall-Module Terminal-Icons
Install-Module GlyphShell -Scope CurrentUser
```

Update your `$PROFILE`: replace `Import-Module Terminal-Icons` with `Import-Module GlyphShell`.

To bring over your existing themes:

```powershell
Import-GlyphShellLegacyThemes   # auto-expands sparse themes to 900+ mappings
```

## How It Works

GlyphShell hooks into PowerShell's format system, the same approach Terminal-Icons pioneered, but backed by compiled C#:

- **Format interception** - at module load, a dynamically generated format file intercepts how `FileInfo`/`DirectoryInfo` objects are displayed
- **CodeProperty columns** - all columns (Mode, Date, Size, Icon, Name, Git, Badge) use CodeProperty-backed `PropertyName` references that call C# static methods directly, with no ScriptBlock compilation or cmdlet invocation overhead
- **Conditional columns** - Git status and Badge columns are included based on your settings
- **Compiled lookups** - 9,254 Nerd Font glyphs and 900+ icon mappings live in `FrozenDictionary` instances, no script parsing at runtime

The underlying objects are never modified and stay in the pipeline as real .NET objects.

## Troubleshooting

> **Full guide:** [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)

- **Icons missing?** → `Set-GlyphShellOption -Diagnostics` then run `dir` to see errors
- **Old commands after update?** → Close terminal and reopen (DLL is locked while loaded)
- **Column misalignment with custom icons?** → Use BMP glyphs (U+0000–U+FFFF: `nf-dev-*`, `nf-seti-*`, `nf-fa-*`), not `nf-md-*`. See [known PowerShell bug](docs/TROUBLESHOOTING.md#column-misalignment-with-supplementary-plane-glyphs-nf-md-)

## Contributing

> **Build instructions & roadmap:** [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md)

```powershell
git clone https://github.com/SemperFu/GlyphShell.git
cd GlyphShell
dotnet build && dotnet test
Import-Module .\module\GlyphShell.psd1 -Force
```

## Credits

| Project | Role |
| --- | --- |
| [Terminal-Icons](https://github.com/devblackops/Terminal-Icons) by Brandon Olin | The original inspiration |
| [eza](https://github.com/eza-community/eza) | Feature inspiration: git status, tree view, file categories, color themes |
| [Nerd Fonts](https://www.nerdfonts.com/) | The glyph ecosystem that makes this possible |
| [Oh My Posh](https://github.com/JanDeDobbeleer/oh-my-posh) | Architectural inspiration for high-performance shell tooling |

## License

MIT
