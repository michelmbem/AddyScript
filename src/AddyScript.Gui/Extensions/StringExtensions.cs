using System;

namespace AddyScript.Gui.Extensions;

/// <summary>
/// Provides extension methods for working with strings.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Capitalizes a string.
    /// </summary>
    /// <param name="value">The string to capitalize</param>
    /// <returns>A <see cref="string"/> with the first letter uppercased</returns>
    public static string Capitalize(this string value) =>
        value.Length == 0 ? value : char.ToUpper(value[0]) + value[1..];
    
    /// <summary>
    /// Escapes a string to be used as a command line argument.
    /// </summary>
    /// <param name="value">The string to escape</param>
    /// <returns>A new string that is properly escaped to be used as a command line argument.</returns>
    public static string EscapeAsCmdLineArg(this string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    /// <summary>
    /// Shorten a string by replacing any exceding part by an ellipsis mark (...).
    /// </summary>
    /// <param name="value">The string that should be made shorter</param>
    /// <param name="maxLength">The desired maximum length</param>
    /// <param name="side">The side of the string where to put the ellipsis mark (negative -> left, 0 -> middle, positive -> right)</param>
    /// <returns>A <see cref="string"/> with <paramref name="maxLength"/>characters</returns>
    public static string Ellipsis(this string value, int maxLength, int side = 1)
    {
        if (value == null || value.Length <= maxLength)
            return value;

        int diff = value.Length - maxLength;
        int firstHalf = maxLength / 2;

        return side switch
        {
            < 0 => $"...{value.AsSpan(diff)}",
            0 => $"{value.AsSpan(0, firstHalf)}...{value.AsSpan(firstHalf + diff)}",
            > 0 => $"{value.AsSpan(0, maxLength)}...",
        };
    }

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
            lines[i] = indentation + lines[i];
        }
        
        return string.Join(Environment.NewLine, lines);
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
        while (start >= 0 && value[start] is ' ' or '\t')
            --start;

        return value[(start + 1)..end];
    }
}