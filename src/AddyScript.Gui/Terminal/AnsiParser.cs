using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Media;

namespace AddyScript.Gui.Terminal;

internal record ColoredSpan(int StartOffset, int Length, IBrush Foreground, IBrush Background)
{
    public int EndOffset => StartOffset + Length;
}

internal record TerminalText(string Text, List<ColoredSpan> Spans);

internal partial class AnsiParser(IBrush defaultFg, IBrush defaultBg)
{
    private static readonly Regex AnsiRegex = GetAnsiRegex();
    private IBrush currentFg = defaultFg;
    private IBrush currentBg = defaultBg;

    public TerminalText Parse(string input, int baseOffset)
    {
        var sb = new StringBuilder();
        List<ColoredSpan> spans = [];
        int logicalPos = 0;
        
        foreach (Match match in AnsiRegex.Matches(input))
        {
            int spanLength = match.Index - logicalPos;
            if (spanLength > 0)
            {
                spans.Add(new (baseOffset + sb.Length, spanLength, currentFg, currentBg));
                sb.Append(input.AsSpan(logicalPos, spanLength));
            }
            
            logicalPos = match.Index + match.Length;
            
            if (SplitCodes(match.Value, out var codes))
                ApplyCodes(codes);
        }

        if (logicalPos < input.Length)
        {
            spans.Add(new (baseOffset + sb.Length, input.Length - logicalPos, currentFg, currentBg));
            sb.Append(input.AsSpan(logicalPos));
        }

        return new (sb.ToString(), spans);
    }
    
    [GeneratedRegex(@"[\u001B\u009B][[\]()#;?]*(?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|(?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~]))")]
    private static partial Regex GetAnsiRegex();

    private static bool SplitCodes(string codeString, out string[] codes)
    {
        codes = null;
        
        int lBraceIndex = codeString.IndexOf('[');
        if (lBraceIndex < 0) return false;
        
        int lowerMIndex = codeString.IndexOf('m', lBraceIndex);
        if (lowerMIndex <= lBraceIndex) return false;
        
        codes = codeString.Substring(lBraceIndex + 1, lowerMIndex - lBraceIndex - 1).Split(';');
        return true;
    }

    private static IBrush AnsiColor(int code) => code switch
    {
        0 => Brushes.Black,
        1 => Brushes.Red,
        2 => Brushes.Green,
        3 => Brushes.Yellow,
        4 => Brushes.Blue,
        5 => Brushes.Magenta,
        6 => Brushes.Cyan,
        7 => Brushes.White,
        _ => null
    };

    private void ApplyCodes(string[] codes)
    {
        if (!int.TryParse(codes[0], out var code)) return;

        switch (code)
        {
            case 0:
            case 39 when codes.Length > 1 && int.TryParse(codes[1], out var code1) && code1 == 49:
                currentFg = defaultFg;
                currentBg = defaultBg;
                break;
            case >= 30 and <= 37:
                currentFg = AnsiColor(code - 30) ?? defaultFg;
                break;
            case >= 40 and <= 47:
                currentBg = AnsiColor(code - 40) ?? defaultBg;
                break;
            case >= 90 and <= 97:
                currentFg = AnsiColor(code - 90) ?? defaultFg;
                break;
            case >= 100 and <= 107:
                currentBg = AnsiColor(code - 100) ?? defaultBg;
                break;
        }
    }
}