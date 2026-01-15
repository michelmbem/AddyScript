using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pty.Net;

namespace AddyScript.Gui.Terminal;

internal class TerminalSession
{
    private static readonly UTF8Encoding Encoding = new (false);
    private readonly CancellationTokenSource cts = new ();
    private IPtyConnection ptyConnection;

    private TerminalSession() {}

    public static async Task<TerminalSession> CreateAsync(PtyOptions options)
    {
        var session = new TerminalSession();
        await session.InitializeAsync(options);
        return session;
    }

    private async Task InitializeAsync(PtyOptions options)
    {
        ptyConnection = await PtyProvider.SpawnAsync(options, cts.Token);
        ptyConnection.ProcessExited += (_, e) =>
            ProcessExited?.Invoke(this, e.ExitCode);

        _ = Task.Run(ReadLoopAsync);
    }

    private async Task ReadLoopAsync()
    {
        var buffer = new byte[4096];

        try
        {
            while (!cts.IsCancellationRequested)
            {
                var read = await ptyConnection.ReaderStream.ReadAsync(buffer, cts.Token);
                if (read <= 0) break;
                
                var text = Encoding.GetString(buffer, 0, read);
                DataReceived?.Invoke(this, text);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }

    public void Send(string text)
    {
        var bytes = Encoding.GetBytes(text);
        ptyConnection.WriterStream.Write(bytes);
        ptyConnection.WriterStream.Flush();
    }

    public void Resize(int cols, int rows) => ptyConnection.Resize(cols, rows);

    public void Close()
    {
        cts.Cancel();
        cts.Dispose();
        ptyConnection?.Dispose();
    }

    public event EventHandler<string> DataReceived;
    
    public event EventHandler<int> ProcessExited;
}