using System;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// A set of extension methods for the <b>string</b> type.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Escapes a string to be used as a command line argument.
    /// </summary>
    /// <param name="value">The string to escape</param>
    /// <returns>A new string that is properly escaped to be used as a command line argument.</returns>
    public static string EscapeAsCmdLineArg(this string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    /// <summary>
    /// Indents all lines in the string with the specified indentation.
    /// </summary>
    /// <param name="value">The string whose lines are to be indented</param>
    /// <param name="indentation">The indentation to add at the beginning of each line</param>
    /// <param name="skipFirstLine">Determines whether the first line should be indented or not</param>
    /// <returns>A new string with all lines indented with <paramref name="indentation"/>.</returns>
    public static string IndentLines(this string value, string indentation, bool skipFirstLine = false)
    {
        var lines = value.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        
        for (var i = 0; i < lines.Length; ++i)
        {
            if (skipFirstLine && i == 0) continue;
            lines[i] = indentation + lines[i].TrimStart(' ', '\t');
        }
        
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Indents the line following the first occurrence of <paramref name="subString"/>
    /// with the specified <paramref name="indentation"/>.
    /// </summary>
    /// <param name="value">The string in which to indent the next line after <paramref name="subString"/></param>
    /// <param name="subString">The substring after which the next line should be indented</param>
    /// <param name="indentation">The indentation to add at the beginning of the line following <paramref name="subString"/></param>
    /// <returns>A new string with the line following the first occurrence of <paramref name="subString"/> indented</returns>
    public static string IndentNextLine(this string value, string subString, string indentation)
    {
        int offset = value.IndexOf(subString);
        if (offset < 0) return value;

        offset = value.IndexOf('\n', offset + subString.Length);
        if (offset < 0) return value;

        return value[..(offset + 1)] + indentation + value[(offset + 1)..];
    }

    /// <summary>
    /// Gets the leading whitespace (spaces and tabs) before the first occurrence of
    /// <paramref name="subString"/> in <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The string from which to extract the leading whitespace</param>
    /// <param name="subString">The substring before which to extract the leading whitespace</param>
    /// <returns>
    /// A new string containing the leading whitespace before the first occurrence of <paramref name="subString"/>
    /// </returns>
    public static string LeadingWhitespace(this string value, string subString)
    {
        int end = value.IndexOf(subString);
        if (end <= 0) return string.Empty;

        int start = end - 1;
        while ((start >= 0) && (value[start] is ' ' or '\t'))
            --start;

        return value[(start + 1)..end];
    }
}