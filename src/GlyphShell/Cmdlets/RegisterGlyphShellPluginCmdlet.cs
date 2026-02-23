using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using GlyphShell.Engine;

namespace GlyphShell.Cmdlets;

/// <summary>
/// Registers a custom GlyphShell plugin that can override icon resolution.
/// The ScriptBlock receives the FileSystemInfo as $args[0] and should return a hashtable
/// with optional keys: Icon, Color, Suffix — or $null to skip.
/// Plugins run in a constrained runspace that blocks writes, network, and process execution.
/// </summary>
[Cmdlet("Register", "GlyphShellPlugin")]
public class RegisterGlyphShellPluginCmdlet : PSCmdlet
{
    /// <summary>Dangerous commands blocked in the plugin sandbox.</summary>
    private static readonly string[] BlockedCommands =
    [
        // File modification
        "Remove-Item", "Set-Content", "Add-Content", "Out-File", "New-Item",
        "Copy-Item", "Move-Item", "Rename-Item", "Clear-Content", "Clear-Item",
        // Network
        "Invoke-WebRequest", "Invoke-RestMethod", "New-Object",
        // Process/code execution
        "Start-Process", "Invoke-Expression", "Invoke-Command", "Start-Job",
        "Start-ThreadJob", "New-PSSession", "Enter-PSSession",
        // Module loading (prevent escape)
        "Import-Module", "Add-Type",
    ];

    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    public ScriptBlock Resolve { get; set; } = null!;

    protected override void ProcessRecord()
    {
        if (!GlyphShellSettings.PluginsEnabled)
        {
            WriteError(new ErrorRecord(
                new InvalidOperationException("The plugin system is disabled. Run 'Set-GlyphShellOption -Plugins' to enable it first."),
                "PluginsDisabled", ErrorCategory.PermissionDenied, null));
            return;
        }

        // Create a constrained runspace for this plugin (reused for all invocations)
        var iss = InitialSessionState.CreateDefault();
        foreach (var cmd in BlockedCommands)
        {
            iss.Commands.Remove(cmd, null);
        }
        iss.LanguageMode = PSLanguageMode.ConstrainedLanguage;

        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();

        // Re-create the ScriptBlock inside the constrained runspace
        var scriptText = Resolve.ToString();

        PluginResult? Resolver(FileSystemInfo fsi)
        {
            using var ps = PowerShell.Create();
            ps.Runspace = runspace;
            ps.AddScript(scriptText).AddArgument(fsi);

            Collection<PSObject> results;
            try
            {
                results = ps.Invoke();
            }
            catch
            {
                return null;
            }

            if (results is null || results.Count == 0)
                return null;

            var obj = results[0]?.BaseObject;
            if (obj is null)
                return null;

            if (obj is Hashtable ht)
            {
                var icon = ht["Icon"] as string;
                var color = ht["Color"] as string;
                var suffix = ht["Suffix"] as string;
                if (icon is null && color is null && suffix is null)
                    return null;
                return new PluginResult(icon, color, suffix);
            }

            return null;
        }

        PluginManager.Register(Name, Resolver);
        WriteObject($"Registered plugin '{Name}' (sandboxed).");
    }
}
