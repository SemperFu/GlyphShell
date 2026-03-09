# Non-BMP Icon Replacements

Glyphs above U+FFFF (supplementary plane) are encoded as surrogate pairs in UTF-16.
PowerShell terminals render them as 2 cells but count them as 1 character, causing
column alignment issues. All `nf-md-*` (Material Design) icons are in this range.

These replacements use BMP-only glyphs (U+0000–U+FFFF) as a workaround.
When PowerShell fixes supplementary plane width counting, revert these to the
original `nf-md-*` icons for better visual fidelity.

**Tracking:** https://github.com/PowerShell/PowerShell/issues — search for surrogate pair / supplementary plane width

## Directory Name Icons (`DefaultIconTheme.cs`)

| Directory | Current (BMP) | Original (non-BMP) |
|-----------|--------------|---------------------|
| .azure | `nf-cod-azure` (U+EBD8) | `nf-md-microsoft_azure` (U+F0805) |
| .cache | `nf-fa-database` (U+F1C0) | `nf-md-cached` (U+F00E8) |
| .kube | `nf-oct-container` (U+F4B7) | `nf-md-ship_wheel` (U+F0833) |
| applications | `nf-oct-apps` (U+F40E) | `nf-md-apps` (U+F003B) |
| apps | `nf-oct-apps` (U+F40E) | `nf-md-apps` (U+F003B) |
| benchmark | `nf-oct-stopwatch` (U+F520) | `nf-md-timer` (U+F13AB) |
| contacts | `nf-fa-address_book` (U+F2B9) | `nf-md-contacts` (U+F06CB) |
| desktop | `nf-oct-device_desktop` (U+F4A9) | `nf-md-desktop_classic` (U+F07C0) |
| downloads | `nf-oct-download` (U+F409) | `nf-md-folder_download` (U+F024D) |
| favorites | `nf-fa-star` (U+F005) | `nf-md-folder_star` (U+F069D) |
| images | `nf-fa-images` (U+F00F) | `nf-md-folder_image` (U+F024F) |
| movies | `nf-oct-video` (U+F52C) | `nf-md-movie` (U+F0381) |
| music | `nf-fa-music` (U+F001) | `nf-md-music_box_multiple` (U+F0333) |
| photos | `nf-fa-images` (U+F00F) | `nf-md-folder_image` (U+F024F) |
| pictures | `nf-fa-images` (U+F00F) | `nf-md-folder_image` (U+F024F) |
| songs | `nf-fa-music` (U+F001) | `nf-md-music_box_multiple` (U+F0333) |
| tests | `nf-cod-beaker` (U+EA79) | `nf-md-test_tube` (U+F0668) |
| umbraco | `nf-fa-umbrella` (U+F0E9) | `nf-md-umbraco` (U+F0549) |
| videos | `nf-oct-video` (U+F52C) | `nf-md-movie` (U+F0381) |

## YAML Theme Directory Icons

All three built-in themes (`onedark`, `dracula`, `catppuccin`):

| Directory | Current (BMP) | Original (non-BMP) |
|-----------|--------------|---------------------|
| tests | `nf-cod-beaker` (U+EA79) | `nf-md-test_tube` (U+F0668) |

## Column Header Icons (`FormatFileGenerator.cs`, `format.ps1xml`)

| Header | Current (BMP) | Original (non-BMP) |
|--------|--------------|---------------------|
| Icon | `nf-fa-file_o` (U+F016) | `⚡` (U+26A1, emoji-capable) |
| Badge | `nf-fa-tag` (U+F02B) | `◆` (U+25C6, emoji-capable) |

## Remaining Non-BMP Icons

~99 file extension mappings in `DefaultIconTheme.cs` and ~15 entries in YAML theme
## File Extension Icons (`DefaultIconTheme.cs` FileExtensions)

| Extension | Current (BMP) | Original (non-BMP) |
|-----------|--------------|---------------------|
| .appx, .AppxBundle, .deb, .msi, .msix, .msixbundle, .rpm | `nf-cod-package` | `nf-md-package_variant` |
| .astro | `nf-fa-rocket` | `nf-md-rocket` |
| .c | `nf-custom-c` | `nf-md-language_c` |
| .chm | `nf-fa-question_circle` | `nf-md-help_box` |
| .cpp | `nf-custom-cpp` | `nf-md-language_cpp` |
| .cs, .csx | `nf-dev-dotnet` | `nf-md-language_csharp` |
| .csv, .tsv, .xls, .xlsx | `nf-fa-file_excel_o` | `nf-md-file_excel` |
| .doc, .docx, .rtf | `nf-fa-file_word_o` | `nf-md-file_word` |
| .dtd, .iml, .manifest, .plist, .project, .resx, .tmLanguage, .xml, .xquery, .xsd, .xsl, .xslt | `nf-seti-xml` | `nf-md-xml` |
| .env | `nf-fa-cog` | `nf-md-file_cog` |
| .exe | `nf-fa-window_maximize` | `nf-md-application` |
| .gradle | `nf-dev-java` | `nf-md-elephant` |
| .iLogicVb | `nf-fa-code` | `nf-md-alpha_i` |
| .ipynb | `nf-fa-book` | `nf-md-notebook` |
| .lrc, .srt, .txt | `nf-fa-file_text_o` | `nf-md-file_document` |
| .pkl | `nf-fa-cog` | `nf-md-cog` |
| .potm–.pptx (all PowerPoint) | `nf-fa-file_powerpoint_o` | `nf-md-file_powerpoint` |
| .ps1, .ps1xml, .psc1, .psd1, .psm1, .pssc | `nf-cod-terminal_powershell` | `nf-md-console_line` |
| .R, .Rmd, .Rproj | `nf-seti-r` | `nf-md-language_r` |
| .svg | `nf-seti-svg` | `nf-md-svg` |
| .vhd, .vhdx, .vmdk | `nf-fa-hdd_o` | `nf-md-harddisk` |
| .vue | `nf-seti-vue` | `nf-md-vuejs` |
| .wasm | `nf-oct-package` | `nf-md-hexagon` |
| .xaml | `nf-fa-file_code_o` | `nf-md-language_xaml` |
| .yaml, .yml | `nf-fa-align_left` | `nf-md-format_align_left` |

## Well-Known File Icons (`DefaultIconTheme.cs` WellKnownFiles)

| Filename | Current (BMP) | Original (non-BMP) |
|----------|--------------|---------------------|
| .azure-pipelines.yml | `nf-cod-azure` | `nf-md-microsoft_azure` |
| .htaccess | `nf-seti-xml` | `nf-md-xml` |
| gradlew, pom.xml, build.gradle(.kts), settings.gradle(.kts) | `nf-dev-java` | `nf-md-elephant` |
| LICENSE | `nf-fa-certificate` | `nf-md-certificate` |
| README, README.md, README.txt | `nf-fa-file_text_o` | `nf-md-text_box_multiple` |
| vue.config.js, vue.config.ts | `nf-seti-vue` | `nf-md-vuejs` |
| go.mod, go.sum | `nf-dev-go` | `nf-md-language_go` |
| pyproject.toml, setup.py/cfg, requirements.txt, Pipfile(.lock), poetry.lock | `nf-dev-python` | `nf-md-language_python` |
| composer.json | `nf-dev-php` | `nf-md-language_php` |
| Package.swift | `nf-dev-swift` | `nf-md-language_swift` |
| global.json, nuget.config | `nf-dev-visualstudio` | `nf-md-microsoft_visual_studio` |

## YAML Theme File Extension/WellKnown Replacements

All three built-in themes (`onedark`, `dracula`, `catppuccin`):

| Key | Current (BMP) | Original (non-BMP) |
|-----|--------------|---------------------|
| .c | `nf-custom-c` | `nf-md-language_c` |
| .cpp | `nf-custom-cpp` | `nf-md-language_cpp` |
| .cs | `nf-dev-dotnet` | `nf-md-language_csharp` |
| .csv | `nf-fa-file_excel_o` | `nf-md-file_excel` |
| .doc, .docx | `nf-fa-file_word_o` | `nf-md-file_word` |
| .env | `nf-fa-cog` | `nf-md-file_cog` |
| .exe | `nf-fa-window_maximize` | `nf-md-application` |
| .ps1 | `nf-cod-terminal_powershell` | `nf-md-console_line` |
| .svg | `nf-seti-svg` | `nf-md-svg` |
| .txt | `nf-fa-file_text_o` | `nf-md-file_document` |
| .xml | `nf-seti-xml` | `nf-md-xml` |
| .yaml, .yml | `nf-fa-align_left` | `nf-md-format_align_left` |
| LICENSE | `nf-fa-certificate` | `nf-md-certificate` |
| README.md | `nf-fa-file_text_o` | `nf-md-text_box_multiple` |
