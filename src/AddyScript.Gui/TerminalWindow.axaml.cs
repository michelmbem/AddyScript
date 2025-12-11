using System;

using AddyScript.Gui.Terminal;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

using Pty.Net;

namespace AddyScript.Gui;

public partial class TerminalWindow : Window
{
    private readonly AnsiParser parser;
    private readonly TerminalColorizer colorizer;

    private TerminalSession terminal;
    private int inputOffset;
    private char trailingChar;

    public TerminalWindow()
    {
        InitializeComponent();

        parser = new AnsiParser(TerminalView.Foreground, TerminalView.Background);
        colorizer = new TerminalColorizer();

        TextArea textArea = TerminalView.TextArea;
        textArea.TextView.LineTransformers.Add(colorizer);
        textArea.Caret.PositionChanged += TerminalViewCaretMoved;
        textArea.KeyUp += TerminalViewKeyUp;
    }

    public PtyOptions Options { get; init; }

    public int ExitCode => terminal?.ExitCode ?? -1;

    private void WindowActivated(object sender, EventArgs e)
    {
        TerminalView.TextArea.Focus();

        if (Options == null) return;

        terminal = new TerminalSession(Options);
        terminal.DataReceived += TerminalDataReceived;
        terminal.ProcessExited += TerminalProcessExited;
    }

    private void WindowClosing(object sender, WindowClosingEventArgs e)
    {
        try
        {
            terminal?.Close();
        }
        catch
        {
            // Ignore
        }
    }

    private void TerminalViewCaretMoved(object sender, EventArgs e)
    {
        TerminalView.IsReadOnly = TerminalView.TextArea.Caret.Offset < inputOffset;
    }

    private void TerminalViewKeyUp(object sender, KeyEventArgs e)
    {
        TextArea textArea = TerminalView.TextArea;
        TextDocument document = textArea.Document;
        Caret caret = textArea.Caret;

        switch (e.Key)
        {
            case Key.Enter:
            {
                var inputLength = document.TextLength - inputOffset;
                if (inputLength <= 0) return;

                var inputText = document.GetText(inputOffset, inputLength).TrimEnd('\r', '\n');
                document.Remove(inputOffset, inputLength);
                terminal.Send(inputText + Environment.NewLine);
                break;
            }
            case Key.Back when caret.Offset == inputOffset - 1:
                document.Insert(caret.Offset, trailingChar.ToString());
                caret.Offset = inputOffset;
                break;
        }
    }

    private void TerminalDataReceived(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            TextArea textArea = TerminalView.TextArea;
            TextDocument document = textArea.Document;
            Caret caret = textArea.Caret;

            TerminalText tt = parser.Parse(text, inputOffset);
            document.Insert(inputOffset, tt.Text);
            colorizer.Spans.AddRange(tt.Spans);

            inputOffset = caret.Offset = document.TextLength;
            trailingChar = inputOffset > 0 ? document.GetCharAt(inputOffset - 1) : '\0';
            caret.BringCaretToView();
        });
    }

    private void TerminalProcessExited(int exitCode)
    {
        terminal = null;
        Dispatcher.UIThread.Post(Close);
    }
}