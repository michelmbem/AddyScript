using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Pty.Net;

namespace AddyScript.Gui;

public partial class TerminalSession
{
    private readonly CancellationToken timeoutToken = new CancellationTokenSource(300_000).Token;
    private readonly Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private IPtyConnection pty;

    public TerminalSession()
    {
        var options = new PtyOptions
        {
            Name = $"{AssemblyInfo.Title} Terminal",
            Rows = 30,
            Cols = 120,
            App = "./asis", // GetShell(),
            Cwd = Environment.CurrentDirectory,
            ForceWinPty = false,
            Environment = new Dictionary<string, string>(),
        };

        var ptyTask = PtyProvider.SpawnAsync(options, timeoutToken);
        ptyTask.Wait();
        pty = ptyTask.Result;

        _ = Task.Run(async () =>
        {
            var buffer = new byte[4096];
            var ansiRegex = GetAnsiCharRegex();

            while (!timeoutToken.IsCancellationRequested)
            {
                var read = await pty.ReaderStream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) continue;
                
                var output = encoding.GetString(buffer, 0, read);
                output = output.Replace("\r", string.Empty);
                output = ansiRegex.Replace(output, string.Empty);
                DataReceived?.Invoke(output);
            }
        });
    }

    private static string GetShell()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
             ? Path.Combine(Environment.SystemDirectory, "powershell.exe")
             : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                 ? "/bin/zsh"
                 : "/bin/bash";
    }

    public void Send(string data)
    {
        var bytes = encoding.GetBytes(data);
        pty.WriterStream.Write(bytes, 0, bytes.Length);
        pty.WriterStream.Flush();
    }

    public void Resize(int rows, int cols) => pty.Resize(rows, cols);
    
    public event Action<string> DataReceived;

    [GeneratedRegex(@"[\u001B\u009B][[\]()#;?]*(?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|(?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~]))")]
    private static partial Regex GetAnsiCharRegex();
}