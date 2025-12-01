using System;

namespace AddyScript.Gui.Extensions;

public static class StringExtensions
{
    public static string EscapeAsCmdLineArg(this string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    
    public static string IndentLines(this string value, string indentation, bool skipFirstLine = false)
    {
        var lines = value.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        
        for (var i = 0; i < lines.Length; ++i)
        {
            if (skipFirstLine && i == 0) continue;
            lines[i] = indentation + lines[i];
        }
        
        return string.Join(Environment.NewLine, lines);
    }
}