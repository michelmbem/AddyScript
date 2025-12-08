using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pty.Net;

namespace AddyScript.Gui.Terminal;

public class TerminalSession
{
    private static readonly UTF8Encoding Encoding = new (false);
    private readonly CancellationToken timeoutToken = new CancellationTokenSource(-1).Token;
    private readonly IPtyConnection ptyConnection;

    public TerminalSession(PtyOptions options)
    {
        var ptyConnectionTask = PtyProvider.SpawnAsync(options, timeoutToken);
        ptyConnectionTask.Wait();
        ptyConnection = ptyConnectionTask.Result;
        ptyConnection.ProcessExited += (_, e) => ProcessExited?.Invoke(e.ExitCode);

        _ = Task.Run(async () =>
        {
            var buffer = new byte[4096];

            while (!timeoutToken.IsCancellationRequested)
            {
                var read = await ptyConnection.ReaderStream.ReadAsync(buffer);
                if (read <= 0) continue;

                var text = Encoding.GetString(buffer, 0, read);
                DataReceived?.Invoke(text);
            }
        });
    }

    public int ExitCode => ptyConnection.ExitCode;

    public void Send(string text)
    {
        var bytes = Encoding.GetBytes(text);
        ptyConnection.WriterStream.Write(bytes);
        ptyConnection.WriterStream.Flush();
    }

    public void Resize(int cols, int rows) => ptyConnection.Resize(cols, rows);
    
    public void Close() => ptyConnection.Dispose();

    public event Action<string> DataReceived;

    public event Action<int> ProcessExited;
}