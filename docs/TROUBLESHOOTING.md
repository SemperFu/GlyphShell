# Troubleshooting & Known Issues

## Troubleshooting

### Icons or colors missing (names show, but plain text)

The resolver is silently failing. Enable diagnostics to see the error:

```powershell
Set-GlyphShellOption -Diagnostics
Get-ChildItem  # warnings will show the exception
Set-GlyphShellOption -Diagnostics:$false
```

Common causes:
- **Missing DLLs** — `YamlDotNet.dll` or `LibGit2Sharp.dll` not alongside `GlyphShell.dll`. Rebuild with `dotnet build -c Release`.
- **Old DLL loaded** — The net9.0 DLL is locked by the current terminal. Close all PowerShell windows and reopen before rebuilding.

### Names blank (no text at all)

The `Format-GlyphShell` cmdlet is throwing and the catch block is also failing. Check `$Error[0]` for the actual exception.

### "command not found" for new cmdlets after update

The old DLL is still loaded. Close the terminal and reopen — PowerShell reimports the module fresh on each session.

### Module loads with "unapproved verbs" warning

Safe to ignore. This is a warning from `Import-Module` about cmdlet naming conventions and doesn't affect functionality.

## Known PowerShell Issues

### Column misalignment with supplementary plane glyphs (nf-md-*)

**Known PowerShell bug** ([#23861](https://github.com/PowerShell/PowerShell/issues/23861)): PowerShell's column width calculation counts .NET `char` units, not Unicode codepoints. Nerd Font glyphs in the **supplementary plane** (U+10000 and above) are stored as surrogate pairs — two `char`s for one visible character. PowerShell counts them as width 2 but the terminal renders them as width 1, causing all subsequent columns to shift left by one cell.

**Which glyphs are affected?** Most `nf-md-*` (Material Design) icons live in the supplementary plane. You can identify them by their Unicode escape: `\U000Fxxxx` (8-digit) vs BMP glyphs which use `\uXXXX` (4-digit).

**What to use instead:** Stick to BMP-range glyph families that don't trigger the bug:

| Family | Prefix | Example |
| --- | --- | --- |
| Devicons | `nf-dev-*` | `nf-dev-python`, `nf-dev-docker` |
| Seti-UI | `nf-seti-*` | `nf-seti-typescript`, `nf-seti-c_sharp` |
| Font Awesome | `nf-fa-*` | `nf-fa-music`, `nf-fa-gamepad` |
| Codicons | `nf-cod-*` | `nf-cod-file`, `nf-cod-folder` |
| Octicons | `nf-oct-*` | `nf-oct-repo`, `nf-oct-git_branch` |

GlyphShell's built-in icons already use BMP glyphs exclusively. This only matters if you add custom icons via `Add-GlyphShellIcon` or register plugins that return icon names. The cmdlet will warn you if you pick a supplementary plane glyph.

A fix is in progress upstream ([PowerShell PR #26567](https://github.com/PowerShell/PowerShell/pull/26567)) but has not shipped yet.
