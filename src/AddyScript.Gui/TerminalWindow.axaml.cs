using System;
using System.Linq;
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
    private static readonly byte[] BackSpaceSequence = [0x1B, 0x5B, 0x36, 0x6E];
    private TerminalSession session;

    public TerminalWindow()
    {
        InitializeComponent();

        TextArea textArea = TerminalView.TextArea;
        textArea.KeyDown += TerminalViewKeyDown;
        textArea.KeyUp += TerminalViewKeyUp;
    }
    
    public PtyOptions PtyOptions { get; init; }
    
    public int ExitCode => session?.ExitCode ?? -1;

    private static bool ArraysEqual(byte[] a1, byte[] a2)
    {
        if (a1.Length != a2.Length) return false;
        return !a1.Where((t, i) => t != a2[i]).Any();
    }
    
    #region Window Events

    private void WindowActivated(object sender, EventArgs e)
    {   
        TerminalView.TextArea.Focus();
        
        if (PtyOptions == null) return;
        
        session = new Terminal.TerminalSession(PtyOptions);
        session.DataReceived += TerminalDataReceived;
        session.ProcessExited += TerminalProcessExited;
    }
    
    #endregion
    
    #region Key Handling

    private void TerminalViewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = e.Key == Key.Tab; // Prevent further processing of the Tab key
    }

    private void TerminalViewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyModifiers is KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.A:
                    session.Send([0x01]); // Ctrl+A
                    break;
                case Key.B:
                    session.Send([0x02]); // Ctrl+B
                    break;
                case Key.C:
                    session.Send([0x03]); // Ctrl+C
                    break;
                case Key.D:
                    session.Send([0x04]); // Ctrl+D
                    break;
                case Key.E:
                    session.Send([0x05]); // Ctrl+E
                    break;
                case Key.F:
                    session.Send([0x06]); // Ctrl+F
                    break;
                case Key.G:
                    session.Send([0x07]); // Ctrl+G
                    break;
                case Key.H:
                    session.Send([0x08]); // Ctrl+H
                    break;
                case Key.I:
                    session.Send([0x09]); // Ctrl+I (Tab)
                    break;
                case Key.J:
                    session.Send([0x0A]); // Ctrl+J (Line Feed)
                    break;
                case Key.K:
                    session.Send([0x0B]); // Ctrl+K
                    break;
                case Key.L:
                    session.Send([0x0C]); // Ctrl+L
                    break;
                case Key.M:
                    session.Send([0x0D]); // Ctrl+M (Carriage Return)
                    break;
                case Key.N:
                    session.Send([0x0E]); // Ctrl+N
                    break;
                case Key.O:
                    session.Send([0x0F]); // Ctrl+O
                    break;
                case Key.P:
                    session.Send([0x10]); // Ctrl+P
                    break;
                case Key.Q:
                    session.Send([0x11]); // Ctrl+Q
                    break;
                case Key.R:
                    session.Send([0x12]); // Ctrl+R
                    break;
                case Key.S:
                    session.Send([0x13]); // Ctrl+S
                    break;
                case Key.T:
                    session.Send([0x14]); // Ctrl+T
                    break;
                case Key.U:
                    session.Send([0x15]); // Ctrl+U
                    break;
                case Key.V:
                    session.Send([0x16]); // Ctrl+V
                    break;
                case Key.W:
                    session.Send([0x17]); // Ctrl+W
                    break;
                case Key.X:
                    session.Send([0x18]); // Ctrl+X
                    break;
                case Key.Y:
                    session.Send([0x19]); // Ctrl+Y
                    break;
                case Key.Z:
                    session.Send([0x1A]); // Ctrl+Z
                    break;
                case Key.D1: // Ctrl+1
                    session.Send([0x31]); // ASCII '1'
                    break;
                case Key.D2: // Ctrl+2
                    session.Send([0x32]); // ASCII '2'
                    break;
                case Key.D3: // Ctrl+3
                    session.Send([0x33]); // ASCII '3'
                    break;
                case Key.D4: // Ctrl+4
                    session.Send([0x34]); // ASCII '4'
                    break;
                case Key.D5: // Ctrl+5
                    session.Send([0x35]); // ASCII '5'
                    break;
                case Key.D6: // Ctrl+6
                    session.Send([0x36]); // ASCII '6'
                    break;
                case Key.D7: // Ctrl+7
                    session.Send([0x37]); // ASCII '7'
                    break;
                case Key.D8: // Ctrl+8
                    session.Send([0x38]); // ASCII '8'
                    break;
                case Key.D9: // Ctrl+9
                    session.Send([0x39]); // ASCII '9'
                    break;
                case Key.D0: // Ctrl+0
                    session.Send([0x30]); // ASCII '0'
                    break;
                case Key.OemOpenBrackets: // Ctrl+[
                    session.Send([0x1B]);
                    break;
                case Key.OemBackslash: // Ctrl+\
                    session.Send([0x1C]);
                    break;
                case Key.OemCloseBrackets: // Ctrl+]
                    session.Send([0x1D]);
                    break;
                case Key.Space: // Ctrl+Space
                    session.Send([0x00]);
                    break;
                case Key.OemMinus: // Ctrl+_
                    session.Send([0x1F]);
                    break;
                default:
                    if (!string.IsNullOrEmpty(e.KeySymbol))
                        session.Send(e.KeySymbol);
                    break;
            }
        }

        if (e.KeyModifiers is KeyModifiers.Alt)
        {
            session.Send([0x1B]);
            if (!string.IsNullOrEmpty(e.KeySymbol))
                session.Send(e.KeySymbol);
        }
        else
        {
            switch (e.Key)
            {
                case Key.Escape:
                    session.Send([0x1b]);
                    break;
                case Key.Space:
                    session.Send([0x20]);
                    break;
                case Key.Delete:
                    session.Send(EscapeSequences.CmdDelKey);
                    break;
                case Key.Back:
                    session.Send(EscapeSequences.CmdDel);
                    break;
                case Key.Up:
                    session.Send(EscapeSequences.MoveUpNormal);
                    break;
                case Key.Down:
                    session.Send(EscapeSequences.MoveDownNormal);
                    break;
                case Key.Left:
                    session.Send(EscapeSequences.MoveLeftNormal);
                    break;
                case Key.Right:
                    session.Send(EscapeSequences.MoveRightNormal);
                    break;
                case Key.PageUp:
                    session.Send(EscapeSequences.CmdPageUp);
                    break;
                case Key.PageDown:
                    session.Send(EscapeSequences.CmdPageDown);
                    break;
                case Key.Home:
                    session.Send(EscapeSequences.MoveHomeNormal);
                    break;
                case Key.End:
                    session.Send(EscapeSequences.MoveEndNormal);
                    break;
                case Key.Insert:
                    break;
                case Key.F1:
                    session.Send(EscapeSequences.CmdF[0]);
                    break;
                case Key.F2:
                    session.Send(EscapeSequences.CmdF[1]);
                    break;
                case Key.F3:
                    session.Send(EscapeSequences.CmdF[2]);
                    break;
                case Key.F4:
                    session.Send(EscapeSequences.CmdF[3]);
                    break;
                case Key.F5:
                    session.Send(EscapeSequences.CmdF[4]);
                    break;
                case Key.F6:
                    session.Send(EscapeSequences.CmdF[5]);
                    break;
                case Key.F7:
                    session.Send(EscapeSequences.CmdF[6]);
                    break;
                case Key.F8:
                    session.Send(EscapeSequences.CmdF[7]);
                    break;
                case Key.F9:
                    session.Send(EscapeSequences.CmdF[8]);
                    break;
                case Key.F10:
                    session.Send(EscapeSequences.CmdF[9]);
                    break;
                case Key.OemBackTab:
                    session.Send(EscapeSequences.CmdBackTab);
                    break;
                case Key.Tab:
                    session.Send(EscapeSequences.CmdTab);
                    break;
                default:
                    if (!string.IsNullOrEmpty(e.KeySymbol))
                        session.Send(e.KeySymbol);
                    break;
            }
        }

        e.Handled = true;
    }
    
    #endregion
    
    #region Terminal Events

    private void TerminalDataReceived(byte[] data)
    {
        Console.Write("Received: ");
        foreach (var b in data) Console.Write($"{b:X2} ");
        Console.WriteLine();

        Dispatcher.UIThread.Post(() =>
        {
            TextArea textArea = TerminalView.TextArea;
            TextDocument document = textArea.Document;
            Caret caret = textArea.Caret;
            
            if (ArraysEqual(data, BackSpaceSequence) || ArraysEqual(data, EscapeSequences.CmdDel))
            {
                var prevIndex = caret.Offset - 1;
                if (prevIndex < 0) return;
                document.Remove(prevIndex, 1);
                caret.Offset = prevIndex;
            }
            else
            {
                document.Insert(document.TextLength, TerminalSession.GetString(data));
                caret.Offset = document.TextLength;
            }
            
            textArea.ScrollToLine(caret.Line);
        });
    }

    private void TerminalProcessExited(int exitCode)
    {
        Dispatcher.UIThread.Post(Close);
    }
    
    #endregion
}