using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Avalonia.Media;

using HarfBuzzSharp;

namespace AddyScript.Gui.Terminal;

internal record ColoredSpan(int StartOffset, int Length, IBrush Foreground, IBrush Background)
{
    public int EndOffset => StartOffset + Length;
}

internal record TerminalOutput(string Text, List<ColoredSpan> Spans, bool ClearScreen);

internal partial class AnsiParser(IBrush defaultFg, IBrush defaultBg)
{
    private static readonly Regex AnsiRegex = GetAnsiRegex();
    private IBrush currentFg = defaultFg;
    private IBrush currentBg = defaultBg;

    public TerminalOutput Parse(string input, int baseOffset)
    {
        var sb = new StringBuilder();
        List<ColoredSpan> spans = [];
        bool clearScreen = false;
        int logicalPos = 0;

        foreach (Match match in AnsiRegex.Matches(input))
        {
            int spanLength = match.Index - logicalPos;
            
            if (spanLength > 0)
            {
                spans.Add(new(baseOffset + sb.Length, spanLength, currentFg, currentBg));
                sb.Append(input.AsSpan(logicalPos, spanLength));
            }

            string tag = match.Groups["tag"].Value;

            if (tag is "[2J" or "]1047;\u0007")
                clearScreen = true;
            else if (tag.StartsWith('[') && tag.EndsWith('m'))
                ApplyCodes(tag[1..^1].Split(';'));

            logicalPos = match.Index + match.Length;
        }

        if (logicalPos < input.Length)
        {
            spans.Add(new (baseOffset + sb.Length, input.Length - logicalPos, currentFg, currentBg));
            sb.Append(input.AsSpan(logicalPos));
        }

        return new (sb.ToString(), spans, clearScreen);
    }

    [GeneratedRegex(
        """
        [\u001B\u009B](?<tag>\][^\u0007]*\u0007|[\[\]()#;?]*
        (?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|
        (?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~])))
        """,
        RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex GetAnsiRegex();

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
        if (!int.TryParse(codes[0], out var code))
            code = 0;

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
            case 38 when codes.Length > 2 && int.TryParse(codes[2], out var fg):
                currentFg = AnsiColor(fg % 8);
                break;
            case 48 when codes.Length > 2 && int.TryParse(codes[2], out var bg):
                currentBg = AnsiColor(bg % 8);
                break;
        }
    }
}