using System;
using System.Text;
using System.Text.RegularExpressions;


namespace AddyScript.Runtime.Utilities
{
    public static class StringUtil
    {
        public static string Capitalize(string value)
        {
            return value == null
                 ? null
                 : value.Length < 2
                 ? value.ToUpper()
                 : char.ToUpper(value[0]) + value.Substring(1);
        }

        public static string Uncapitalize(string value)
        {
            return value == null
                 ? null
                 : value.Length < 2
                 ? value.ToLower()
                 : char.ToLower(value[0]) + value.Substring(1);
        }

        public static string Repeat(string value, int times)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < times; ++i)
                sb.Append(value);

            return sb.ToString();
        }

        public static Regex GetRegex(string pattern)
        {
            RegexOptions options = RegexOptions.None;

            if (pattern.Length > 0 && pattern[0] == '/')
            {
                int lastSlash = pattern.LastIndexOf('/');
                if (lastSlash > 0)
                {
                    string optionStr = pattern.Substring(lastSlash + 1);
                    pattern = pattern.Substring(1, lastSlash - 1);

                    for (int i = 0; i < optionStr.Length; ++i)
                    {
                        char ch = optionStr[i];
                        switch (ch)
                        {
                            case 's':
                                options |= RegexOptions.Singleline;
                                break;
                            case 'm':
                                options |= RegexOptions.Multiline;
                                break;
                            case 'i':
                                options |= RegexOptions.IgnoreCase;
                                break;
                            case 'x':
                                options |= RegexOptions.IgnorePatternWhitespace;
                                break;
                            case 'u':
                                options |= RegexOptions.CultureInvariant;
                                break;
                            case 'r':
                                options |= RegexOptions.RightToLeft;
                                break;
                            default:
                                throw new FormatException("Invalid regex modifier: " + ch);
                        }
                    }
                }
            }

            return new Regex(pattern, options);
        }

        public static byte[] String2ByteArray(string str)
        {
            var bytes = new byte[str.Length];
            for (int i = 0; i < str.Length; ++i)
                bytes[i] = (byte)str[i];
            return bytes;
        }

        public static string ByteArray2String(byte[] bytes)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; ++i)
                sb.Append((char)bytes[i]);
            return sb.ToString();
        }
    }
}
