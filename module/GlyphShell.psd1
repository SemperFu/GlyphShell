@{
    RootModule        = '../src/GlyphShell/bin/Release/net9.0/GlyphShell.dll'
    ModuleVersion     = '0.1.0'
    GUID              = 'ee06b1dc-1a8b-4c3f-82d3-fe74c4b7b962'
    Author            = 'SemperFu'
    CompanyName       = 'SemperFu'
    Copyright         = '(c) 2026 SemperFu. All rights reserved.'
    Description       = 'Hooks into Get-ChildItem to add Nerd Font icons, colors, and extra columns to your directory listings. Built in C#, no configuration required. Inspired by Terminal-Icons and eza.'
    PowerShellVersion = '7.5'
    FunctionsToExport = @()
    CmdletsToExport   = @(
        'Format-GlyphShell',
        'Format-GlyphShellIcon',
        'Format-GlyphShellBadge',
        'Format-GlyphShellGit',
        'Format-GlyphShellSize',
        'Format-GlyphShellMode',
        'Format-GlyphShellDate',
        'Show-GlyphShell',
        'Show-GlyphShellTree',
        'Show-GlyphShellGrid',
        'Get-GlyphShellTheme',
        'Set-GlyphShellTheme',
        'Show-GlyphShellTheme',
        'Add-GlyphShellIconTheme',
        'Add-GlyphShellColorTheme',
        'Remove-GlyphShellTheme',
        'Export-GlyphShellTheme',
        'Import-GlyphShellLegacyThemes',
        'New-GlyphShellTheme',
        'Add-GlyphShellIcon',
        'Remove-GlyphShellIcon',
        'Get-GlyphShellOverrides',
        'Set-GlyphShellOption',
        'Register-GlyphShellPlugin',
        'Unregister-GlyphShellPlugin',
        'Get-GlyphShellPlugins',
        'Enable-GlyphShellPlugin',
        'Disable-GlyphShellPlugin',
        'Select-GlyphShell'
    )
    VariablesToExport = @()
    AliasesToExport   = @('gstree', 'gsgrid')
    FormatsToProcess  = @('GlyphShell.format.ps1xml')
    TypesToProcess    = @('GlyphShell.types.ps1xml')
    PrivateData       = @{
        PSData = @{
            Tags       = @('Terminal', 'Icons', 'NerdFonts', 'Glyphs', 'Color', 'Git', 'DirectoryListing', 'PowerShell', 'ls', 'dir', 'Get-ChildItem', 'eza', 'Terminal-Icons')
            ProjectUri = 'https://github.com/SemperFu/GlyphShell'
            LicenseUri = 'https://github.com/SemperFu/GlyphShell/blob/master/LICENSE'
            IconUri    = 'https://raw.githubusercontent.com/SemperFu/GlyphShell/master/docs/demo/glyph-preview.png'
            ReleaseNotes = 'Initial public release. Still early, some things are rough.

Works well: file/folder icons for 900+ types, colored dates/sizes/mode, per-extension overrides, gstree and gsgrid.

Preview/WIP: theme system (8 themes included but switching is hit or miss), git status, plugin API.

Project type detection is on by default. Disable with Set-GlyphShellOption -ProjectDetection:$false if it causes slowdowns.

https://github.com/SemperFu/GlyphShell/releases/tag/v0.1.0'
        }
    }
}
