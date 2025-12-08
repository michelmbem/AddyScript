using System;
using System.IO;
using System.Text;
using AddyScript.Gui.Terminal;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Pty.Net;

namespace AddyScript.Gui;

public partial class TerminalWindow : Window
{
    private readonly AnsiParser ansiParser;
    private readonly TerminalColorizer colorizer;
    
    private TerminalSession session;
    private int inputStartOffset;
    private char lastChar;

    public TerminalWindow()
    {
        InitializeComponent();
        
        ansiParser = new AnsiParser(TerminalView.Foreground, TerminalView.Background);
        colorizer = new TerminalColorizer();

        TextArea textArea = TerminalView.TextArea;
        textArea.TextView.LineTransformers.Add(colorizer);
        textArea.Caret.PositionChanged += TerminalViewCaretPositionChanged;
        textArea.KeyUp += TerminalViewKeyUp;
    }

    public PtyOptions PtyOptions { get; init; }
    
    public int ExitCode => session?.ExitCode ?? -1;

    private void WindowActivated(object sender, EventArgs e)
    {   
        TerminalView.TextArea.Focus();
        
        if (PtyOptions == null) return;
        
        session = new TerminalSession(PtyOptions);
        session.DataReceived += TerminalDataReceived;
        session.ProcessExited += TerminalProcessExited;
    }

    private void TerminalViewCaretPositionChanged(object sender, EventArgs e)
    {
        TerminalView.IsReadOnly = TerminalView.TextArea.Caret.Offset < inputStartOffset;
    }

    private void TerminalViewKeyUp(object sender, KeyEventArgs e)
    {
        TextArea textArea = TerminalView.TextArea;
        TextDocument document = textArea.Document;
        Caret caret = textArea.Caret;
            
        if (e.Key == Key.Enter)
        {
            var inputLength = document.TextLength - inputStartOffset;
            if (inputLength <= 0) return;

            var inputText = document.GetText(inputStartOffset, inputLength).TrimEnd('\r', '\n');
            document.Remove(inputStartOffset, inputLength);
            session.Send(inputText + "\n");
        }
        else if (e.Key == Key.Back && caret.Offset < inputStartOffset)
        {
            document.Insert(caret.Offset, lastChar.ToString());
            caret.Offset = inputStartOffset;
        }
    }

    private void TerminalDataReceived(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            TextArea textArea = TerminalView.TextArea;
            TextDocument document = textArea.Document;
            Caret caret = textArea.Caret;
            
            string cleanText = ansiParser.Parse(text, inputStartOffset);
            document.Insert(inputStartOffset, cleanText);
            colorizer.Spans.AddRange(ansiParser.Spans);
            
            int docLength = document.TextLength;
            inputStartOffset = caret.Offset = docLength;
            lastChar = docLength > 0 ? document.GetCharAt(docLength - 1) : '\0';
            caret.BringCaretToView();
        });
    }

    private void TerminalProcessExited(int exitCode)
    {
        Dispatcher.UIThread.Post(Close);
    }
}