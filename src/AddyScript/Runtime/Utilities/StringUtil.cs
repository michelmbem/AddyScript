using System;
using System.Text;
using System.Text.RegularExpressions;


namespace AddyScript.Runtime.Utilities;


public static class StringUtil
{
    private static readonly UTF8Encoding Encoding = new (false, false);
    
    public static string Capitalize(string value) =>
        value == null
            ? null
            : value.Length < 2
                ? value.ToUpper()
                : char.ToUpper(value[0]) + value[1..];

    public static string Uncapitalize(string value) =>
        value == null
            ? null
            : value.Length < 2
                ? value.ToLower()
                : char.ToLower(value[0]) + value[1..];

    public static string Repeat(string value, int times)
    {
        StringBuilder sb = new ();

        for (int i = 0; i < times; ++i)
            sb.Append(value);

        return sb.ToString();
    }

    public static Regex ToRegex(string pattern)
    {
        RegexOptions options = RegexOptions.Compiled;

        if (pattern.Length > 0 && pattern[0] == '/')
        {
            int lastSlash = pattern.LastIndexOf('/');
            if (lastSlash > 0)
            {
                string optionStr = pattern[(lastSlash + 1)..];
                pattern = pattern[1..lastSlash];

                foreach (var ch in optionStr)
                {
                    options |= ch switch
                    {
                        's' => RegexOptions.Singleline,
                        'm' => RegexOptions.Multiline,
                        'i' => RegexOptions.IgnoreCase,
                        'x' => RegexOptions.IgnorePatternWhitespace,
                        'u' => RegexOptions.CultureInvariant,
                        'r' => RegexOptions.RightToLeft,
                        _ => throw new FormatException("Invalid regex modifier: " + ch),
                    };
                }
            }
        }

        return new Regex(pattern, options);
    }
    
    public static string ToString(Regex regex)
    {
        StringBuilder sb = new ();
        sb.Append('/').Append(regex).Append('/');
        
        var options = regex.Options;
        if (options.HasFlag(RegexOptions.Singleline)) sb.Append('s');
        if (options.HasFlag(RegexOptions.Multiline)) sb.Append('m');
        if (options.HasFlag(RegexOptions.IgnoreCase)) sb.Append('i');
        if (options.HasFlag(RegexOptions.IgnorePatternWhitespace)) sb.Append('x');
        if (options.HasFlag(RegexOptions.CultureInvariant)) sb.Append('u');
        if (options.HasFlag(RegexOptions.RightToLeft)) sb.Append('r');
        
        return sb.ToString();
    }

    public static byte[] String2ByteArray(string str) => Encoding.GetBytes(str);

    public static string ByteArray2String(byte[] bytes) => Encoding.GetString(bytes);
}
