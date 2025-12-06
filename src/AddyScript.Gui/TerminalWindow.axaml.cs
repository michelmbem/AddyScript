using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AddyScript.Gui;

public partial class TerminalWindow : Window
{
    private TerminalSession session;
    
    public TerminalWindow()
    {
        InitializeComponent();
        
        Title = $"{AssemblyInfo.Title} Terminal";

        session = new TerminalSession();
        session.DataReceived += TerminalDataReceived;
    }

    private static string ConvertKey(Key key, bool shift)
    {
        return key switch
        {
            Key.Escape => "\u001b",
            Key.Tab => "\t",
            Key.Space => " ",
            Key.Back => "\b",
            Key.Enter => "\n",
            Key.Left => shift ? "\u001b[1;2D" : "\u001b[D",
            Key.Right => shift ? "\u001b[1;2C" : "\u001b[C",
            Key.Up => shift ? "\u001b[1;2A" : "\u001b[A",
            Key.Down => shift ? "\u001b[1;2B" : "\u001b[B",
            >= Key.A and <= Key.Z =>
                shift ? ((char)('A' + (key - Key.A))).ToString() : ((char)('a' + (key - Key.A))).ToString(),
            >= Key.D0 and <= Key.D9 =>
                ((char)('0' + (key - Key.D0))).ToString(),
            _ => string.Empty
        };
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        TerminalBox.Focus();
    }

    private void TerminalBoxKeyUp(object sender, KeyEventArgs e)
    {
        Console.WriteLine($"Key pressed: {e.Key}");
        session.Send(ConvertKey(e.Key, e.KeyModifiers.HasFlag(KeyModifiers.Shift)));
        e.Handled = true;
    }

    private void TerminalDataReceived(string data)
    {
        Console.WriteLine($"Terminal data: {data}");
        Dispatcher.UIThread.Post(() =>
        {
            TerminalBox.Text += data;
            TerminalBox.CaretIndex = TerminalBox.Text.Length;
        });
    }
}