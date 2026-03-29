using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AddyScript.Gui.Extensions;
using Avalonia.Controls;
using Pty.Net;

namespace AddyScript.Gui.Terminal;

internal static class TerminalLauncher
{
    public static async Task<int> LaunchTerminal(Window owner, string title, string command, string[] args) =>
        App.Options.UseEmulatedTerminal
            ? await LaunchEmulatedTerminal(owner, title, command, args)
            : await LaunchNativeTerminal(command, args);

    public static async Task<int> LaunchEmulatedTerminal(Window owner, string title, string command, string[] args)
    {
        var options = new PtyOptions
        {
            Name = title,
            Rows = 30,
            Cols = 120,
            App = command,
            CommandLine = args,
            Cwd = Environment.CurrentDirectory,
        };

        var terminalWindow = new TerminalWindow
        {
            Title = options.Name,
            Options = options
        };

        await terminalWindow.ShowDialog(owner);
        return terminalWindow.ExitCode;
    }

    public static async Task<int> LaunchNativeTerminal(string command, string[] args) => OperatingSystem.IsMacOS()
        ? await LaunchMacOSTerminal(command, args)
        : await LaunchWindowsOrLinuxTerminal(command, args);

    private static async Task<int> LaunchWindowsOrLinuxTerminal(string command, string[] args)
    {
        var process = Process.Start(new ProcessStartInfo(command, args));
        if (process == null) return -1;

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static async Task<int> LaunchMacOSTerminal(string command, string[] args)
    {
        var doneFile = $"/tmp/asis_{Guid.NewGuid():N}";
        var joinedArgs = string.Join(' ', args.Select(arg => arg.EscapeAsCmdLineArg()));
        var fullCommand = $"{command} {joinedArgs}; echo done > {doneFile}; exit";
        var script = GetMacOSTerminalScript(fullCommand);
        
        var psi = new ProcessStartInfo
        {
            FileName = "osascript",
            Arguments = $"-e \"{script.EscapeForAppleScript()}\"",
            UseShellExecute = false
        };

        var terminal = Process.Start(psi);
        if (terminal == null) return -1;
        
        while (!File.Exists(doneFile))
            await Task.Delay(250);
        
        File.Delete(doneFile);
        return terminal.ExitCode;
    }

    private static string GetMacOSTerminalScript(string command)
    {
        string terminalApp;
        
        if (Directory.Exists("/Applications/iTerm.app"))
            terminalApp = "iTerm";
        else if (Directory.Exists("/Applications/iTerm2.app"))
            terminalApp = "iTerm2";
        else
            terminalApp = "Terminal";
        
        return terminalApp.StartsWith("iTerm", StringComparison.Ordinal)
            ? $"""
               tell application "{terminalApp}"
                   create window with default profile
                   tell current session of current window
                       write text "{command.EscapeForAppleScript()}"
                   end tell
               end tell
               """
            : $"""
               tell application "{terminalApp}" to do script "{command.EscapeForAppleScript()}"
               """;
    }
}