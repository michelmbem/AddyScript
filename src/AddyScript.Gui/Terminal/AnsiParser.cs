using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Media;

namespace AddyScript.Gui.Terminal;

public record ColoredSpan(int StartOffset, int Length, Color Foreground, Color Background);

public partial class AnsiParser(Color fg, Color bg)
{
    private static readonly Regex AnsiRegex = GetAnsiRegex();
    private Color currentFg = fg;
    private Color currentBg = bg;

    public List<ColoredSpan> Spans { get; } = [];

    public string Parse(string input, int baseOffset)
    {
        var sb = new StringBuilder();
        int logicalPos = 0;

        foreach (Match match in AnsiRegex.Matches(input))
        {
            sb.Append(input.AsSpan(logicalPos, match.Index - logicalPos));
            logicalPos = match.Index + match.Length;

            //string[] codes = match.Groups["code"].Value.Split(';');
            string[] codes = match.Value.Split(';');
            ApplyCodes(codes);
        }

        // Add remaining text
        sb.Append(input.AsSpan(logicalPos));

        string cleaned = sb.ToString();

        // Record spans
        if (cleaned.Length > 0)
            Spans.Add(new ColoredSpan(baseOffset, cleaned.Length, currentFg, currentBg));

        return cleaned;
    }

    private void ApplyCodes(string[] codes)
    {
        using var log = File.AppendText("ansi.log");

        foreach (var c in codes)
        {
            log.WriteLine(c);

            if (c == "0") // reset
            {
                currentFg = fg;
                currentBg = fg;
            }
            else if (int.TryParse(c, out int code))
            {
                if (code >= 30 && code <= 37)
                    currentFg = BasicAnsiColor(code - 30);

                if (code >= 40 && code <= 47)
                    currentBg = BasicAnsiColor(code - 40);
            }
        }
    }

    private Color BasicAnsiColor(int i) => i switch
    {
        0 => Colors.Black,
        1 => Colors.Red,
        2 => Colors.Green,
        3 => Colors.Yellow,
        4 => Colors.Blue,
        5 => Colors.Magenta,
        6 => Colors.Cyan,
        7 => Colors.White,
        _ => Colors.White
    };
    
    [GeneratedRegex(@"[\u001B\u009B][[\]()#;?]*(?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|(?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~]))")]
    private static partial Regex GetAnsiRegex();
}