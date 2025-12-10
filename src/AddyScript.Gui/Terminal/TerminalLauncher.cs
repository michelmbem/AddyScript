using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Pty.Net;

namespace AddyScript.Gui.Terminal;

internal static class TerminalLauncher
{
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

    public static async Task<int> LaunchNativeTerminal(string command, string[] args)
    {
        var process = Process.Start(command, string.Join(" ", args));
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}