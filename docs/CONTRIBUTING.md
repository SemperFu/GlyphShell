# Contributing to GlyphShell

## Building from Source

```powershell
git clone https://github.com/SemperFu/GlyphShell.git
cd GlyphShell
dotnet build
dotnet test

# Load the module
Import-Module .\module\GlyphShell.psd1 -Force
Get-ChildItem  # Should show icons!
```

### Important Build Notes

- **Target frameworks:** `net9.0;net10.0` — PS 7.5 runs on .NET 9, PS 7.6 runs on .NET 10
- **DLL locking:** The net9.0 DLL is locked when the module is imported. You cannot rebuild while any PowerShell terminal has GlyphShell loaded. Close all terminals first.
- **Dependencies:** `CopyLocalLockFileAssemblies=true` in the csproj ensures YamlDotNet and LibGit2Sharp copy to the output directory

### Running Tests

```powershell
dotnet test src/GlyphShell.Tests/   # 102 xUnit tests
```

## Roadmap

Everything below has shipped. Future plans will be driven by community feedback and feature requests.

### Completed

- Compiled glyph database (`FrozenDictionary`, 9,253 Nerd Font glyphs)
- Icon resolution engine (10-step lookup chain)
- Format hook with dynamic column generation (Git/Badge columns toggle on/off)
- Color engine (ANSI RGB / 256-color)
- Theme system (YAML + legacy `.psd1` import, 3 built-in themes)
- Git status integration (LibGit2Sharp, opt-in)
- Tree view, grid view, sorting helpers
- User-defined icon overrides (persisted to YAML)
- File type classification (16 eza-style categories)
- Colored sizes, mode flags, and age-based date coloring
- Project-type detection (15 types) + content-type detection (10 types)
- Plugin system with constrained-language sandbox
- Built-in file preview plugin
- Theme export to YAML
- `Select-GlyphShell` for custom pipeline output with clean headers
- 102 unit tests + CI/CD pipeline

### Future

- `Select-GlyphShell` parity with `Get-ChildItem` — wildcards, `-Filter`, `-Include`, `-Exclude`, `-Directory`, `-File`, `-Hidden`, `-ReadOnly`, `-System`, `-Attributes`, `-LiteralPath`, `-Name`, `-FollowSymlink`, pipeline input, multiple paths
- PSGallery publishing
- Tab completion for glyph names
- Performance profiling and optimization
- Community-requested features (open an issue!)
