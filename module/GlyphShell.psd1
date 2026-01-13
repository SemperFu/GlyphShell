@{
    RootModule        = '../src/GlyphShell/bin/Release/net9.0/GlyphShell.dll'
    ModuleVersion     = '0.1.0'
    GUID              = 'ee06b1dc-1a8b-4c3f-82d3-fe74c4b7b962'
    Author            = 'SemperFu'
    CompanyName       = 'SemperFu'
    Copyright         = '(c) 2026 SemperFu. All rights reserved.'
    Description       = 'High-performance PowerShell module for file icons and rich directory listings. Built in C#, inspired by Terminal-Icons and eza.'
    PowerShellVersion = '7.5'
    FunctionsToExport = @()
    CmdletsToExport   = @(
        'Format-GlyphShell',
        'Format-GlyphShellIcon',
        'Format-GlyphShellSize',
        'Format-GlyphShellMode',
        'Format-GlyphShellDate',
        'Show-GlyphShell',
        'Set-GlyphShellOption'
    )
    VariablesToExport = @()
    AliasesToExport   = @()
    FormatsToProcess  = @('GlyphShell.format.ps1xml')
    TypesToProcess    = @('GlyphShell.types.ps1xml')
    PrivateData       = @{
        PSData = @{
            Tags       = @('Terminal', 'Icons', 'NerdFonts', 'Glyphs', 'Color')
            ProjectUri = 'https://github.com/SemperFu/GlyphShell'
        }
    }
}
