using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Pty.Net;

namespace AddyScript.Gui;

public partial class TerminalSession
{
    private readonly CancellationToken timeoutToken = new CancellationTokenSource(int.MaxValue).Token;
    private readonly Encoding textEncoding = new UTF8Encoding(false);
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
                if (read > 0) DataReceived?.Invoke(buffer[..read]);
            }
        });
    }
    
    public int ExitCode => ptyConnection.ExitCode;

    public string GetString(byte[] bytes, int index, int count)
    {
        var text = textEncoding.GetString(bytes, index, count).Replace("\r", string.Empty);
        return GetAnsiCharRegex().Replace(text, string.Empty);
    }

    public string GetString(byte[] bytes) => GetString(bytes, 0, bytes.Length);

    public void Send(byte[] bytes)
    {
        ptyConnection.WriterStream.Write(bytes, 0, bytes.Length);
        ptyConnection.WriterStream.Flush();
    }

    public void Send(string text) => Send(textEncoding.GetBytes(text));

    public void Resize(int rows, int cols) => ptyConnection.Resize(rows, cols);

    public void Close() => ptyConnection.Dispose();

    public event Action<byte[]> DataReceived;

    public event Action<int> ProcessExited;

    [GeneratedRegex(@"[\u001B\u009B][[\]()#;?]*(?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|(?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~]))")]
    private static partial Regex GetAnsiCharRegex();
}