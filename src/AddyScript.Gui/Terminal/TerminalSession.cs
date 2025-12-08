using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pty.Net;

namespace AddyScript.Gui.Terminal;

public class TerminalSession
{
    private static readonly Encoding TextEncoding = new UTF8Encoding(false);
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
                var read = await ptyConnection.ReaderStream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) continue;

                var text = TextEncoding.GetString(buffer, 0, read);
                DataReceived?.Invoke(text);
            }
        });
    }

    public int ExitCode => ptyConnection.ExitCode;

    public void Send(string text)
    {
        byte[] bytes = TextEncoding.GetBytes(text);
        ptyConnection.WriterStream.Write(bytes, 0, bytes.Length);
        ptyConnection.WriterStream.Flush();
    }

    public void Resize(int rows, int cols) => ptyConnection.Resize(rows, cols);
    
    public void Close() => ptyConnection.Dispose();

    public event Action<string> DataReceived;

    public event Action<int> ProcessExited;
}