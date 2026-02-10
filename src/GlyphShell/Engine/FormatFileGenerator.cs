using System.IO;
using System.Text;

namespace GlyphShell.Engine;

public static class FormatFileGenerator
{
    private static string? _lastFormatPath;

    public static string Generate()
    {
        _lastFormatPath ??= Path.Combine(Path.GetTempPath(), "GlyphShell.format.dynamic.ps1xml");
        File.WriteAllText(_lastFormatPath, BuildXml(), Encoding.UTF8);
        return _lastFormatPath;
    }

    private static string BuildXml()
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            <?xml version="1.0" encoding="utf-8" ?>
            <Configuration>
                <SelectionSets>
                    <SelectionSet>
                        <Name>FileSystemTypes</Name>
                        <Types>
                            <TypeName>System.IO.DirectoryInfo</TypeName>
                            <TypeName>System.IO.FileInfo</TypeName>
                        </Types>
                    </SelectionSet>
                </SelectionSets>

                <Controls>
                    <Control>
                        <Name>FileSystemTypes-GroupingFormat</Name>
                        <CustomControl>
                            <CustomEntries>
                                <CustomEntry>
                                    <CustomItem>
                                        <Frame>
                                            <LeftIndent>4</LeftIndent>
                                            <CustomItem>
                                                <Text AssemblyName="System.Management.Automation" BaseName="FileSystemProviderStrings" ResourceId="DirectoryDisplayGrouping"/>
                                                <ExpressionBinding>
                                                    <ScriptBlock>
                                                        $_.PSParentPath.Replace("Microsoft.PowerShell.Core\FileSystem::", "")
                                                    </ScriptBlock>
                                                </ExpressionBinding>
                                                <NewLine/>
                                            </CustomItem>
                                        </Frame>
                                    </CustomItem>
                                </CustomEntry>
                            </CustomEntries>
                        </CustomControl>
                    </Control>
                </Controls>

                <ViewDefinitions>
                    <View>
                        <Name>children</Name>
                        <ViewSelectedBy>
                            <SelectionSetName>FileSystemTypes</SelectionSetName>
                        </ViewSelectedBy>
                        <GroupBy>
                            <PropertyName>PSParentPath</PropertyName>
                            <CustomControlName>FileSystemTypes-GroupingFormat</CustomControlName>
                        </GroupBy>
                        <TableControl>
                            <TableHeaders>
                                <TableColumnHeader>
                                    <Label>Mode</Label>
                                    <Width>7</Width>
                                    <Alignment>left</Alignment>
                                </TableColumnHeader>
            """);

        if (GlyphShellSettings.GitStatusEnabled)
        {
            sb.AppendLine("""
                                <TableColumnHeader>
                                    <Label>Git</Label>
                                    <Width>3</Width>
                                    <Alignment>center</Alignment>
                                </TableColumnHeader>
            """);
        }

        sb.AppendLine("""
                                <TableColumnHeader>
                                    <Label>LastWriteTime</Label>
                                    <Width>25</Width>
                                    <Alignment>right</Alignment>
                                </TableColumnHeader>
                                <TableColumnHeader>
                                    <Label>Length</Label>
                                    <Width>14</Width>
                                    <Alignment>right</Alignment>
                                </TableColumnHeader>
                                <TableColumnHeader>
                                    <Label>&#xF016;</Label>
                                    <Width>2</Width>
                                    <Alignment>left</Alignment>
                                </TableColumnHeader>
                                <TableColumnHeader>
                                    <Label>Name</Label>
                                </TableColumnHeader>
                            </TableHeaders>
                            <TableRowEntries>
                                <TableRowEntry>
                                    <Wrap/>
                                    <TableColumnItems>
                                        <TableColumnItem>
                                            <PropertyName>GlyphMode</PropertyName>
                                        </TableColumnItem>
        """);

        if (GlyphShellSettings.GitStatusEnabled)
        {
            sb.AppendLine("""
                                        <TableColumnItem>
                                            <PropertyName>GlyphGit</PropertyName>
                                        </TableColumnItem>
            """);
        }

        sb.AppendLine("""
                                        <TableColumnItem>
                                            <PropertyName>GlyphDate</PropertyName>
                                        </TableColumnItem>
                                        <TableColumnItem>
                                            <PropertyName>GlyphSize</PropertyName>
                                        </TableColumnItem>
                                        <TableColumnItem>
                                            <PropertyName>Icon</PropertyName>
                                        </TableColumnItem>
                                        <TableColumnItem>
                                            <PropertyName>GlyphName</PropertyName>
                                        </TableColumnItem>
                                    </TableColumnItems>
                                </TableRowEntry>
                            </TableRowEntries>
                        </TableControl>
                    </View>
                </ViewDefinitions>
            </Configuration>
        """);

        return sb.ToString();
    }
}
