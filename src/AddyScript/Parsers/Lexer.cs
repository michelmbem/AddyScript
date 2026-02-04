using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Parsers;


/// <summary>
/// AddyScript lexer.
/// </summary>
public class Lexer
{
    private const char EOF = char.MinValue;
    private const int MAX_BUFFER_SIZE = 512;

    private readonly TextReader input;
    private readonly StringBuilder buffer = new ();
    private int charIndex, offset, lineOffset, lineNumber;
    private int startOffset, startLineOffset, startLineNumber;

    /// <summary>
    /// Initializes a new instance of Lexer.
    /// </summary>
    /// <param name="input">Reads characters for the lexer</param>
    public Lexer(TextReader input)
    {
        this.input = input;
        FileName = input switch
        {
            StreamReader { BaseStream: FileStream fs } => fs.Name,
            _ => ":memory:"
        };
    }

    /// <summary>
    /// Gets the name of the source file that's being parsed.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Iterates through the character stream until a token is
    /// recognized or until the end of the buffer is reached.
    /// </summary>
    /// <returns>A <see cref="Token"/></returns>
    public Token NextToken()
    {
        InitToken();

        return SkipWhiteSpace() switch
        {
            EOF => MakeToken(TokenID.EndOfFile, null, true),
            ',' => MakeToken(TokenID.Comma, null, true),
            ';' => MakeToken(TokenID.SemiColon, null, true),
            '(' => MakeToken(TokenID.LeftParenthesis, null, true),
            ')' => MakeToken(TokenID.RightParenthesis, null, true),
            '[' => MakeToken(TokenID.LeftBracket, null, true),
            ']' => MakeToken(TokenID.RightBracket, null, true),
            '{' => MakeToken(TokenID.LeftBrace, null, true),
            '}' => MakeToken(TokenID.RightBrace, null, true),
            '~' => MakeToken(TokenID.Tilda, null, true),
            ':' => Colon(),
            '?' => QuestionMark(),
            '.' => Dot(),
            '=' => EqualSign(),
            '!' => ExclamationMark(),
            '<' => Lt(),
            '>' => Gt(),
            '+' => PlusSign(),
            '-' => MinusSign(),
            '*' => Asterisk(),
            '/' => ForwardSlash(),
            '%' => PercentSign(),
            '&' => Ampersand(),
            '|' => Verticalbar(),
            '^' => CircumflexAccent(),
            '0' => Zero(),
            '$' => DollarSign(),
            '`' => LiteralDate(),
            '@' => VerbatimString(),
            '\'' or '"' => LiteralString(),
            'b' or 'B' when Ll(2) is '\'' or '"' => LiteralBlob(),
            var ch when IsLegalFirstIdChar(ch) => Identifier(),
            var ch when char.IsDigit(ch) => LiteralNumber(),
            _ => Unknown(),
        };
    }

    /// <summary>
    /// Initializes the next token to be returned by <see cref="NextToken"/>.
    /// </summary>
    private void InitToken()
    {
        startOffset = offset;
        startLineOffset = lineOffset;
        startLineNumber = lineNumber;
    }

    /// <summary>
    /// Creates a token with the given <see cref="TokenID"/>.
    /// </summary>
    /// <param name="tokenID">The <see cref="TokenID"/> to be set to the token</param>
    /// <param name="value">The token's value</param>
    /// <param name="consume">Tells if the current character must be skipped or not</param>
    /// <returns>A <see cref="Token"/></returns>
    private Token MakeToken(TokenID tokenID, object value, bool consume)
    {
        var start = new ScriptLocation(startOffset, startLineOffset, startLineNumber);
        if (consume) Consume(1);
        var end = new ScriptLocation(offset, lineOffset, lineNumber);
        return new Token(tokenID, value, start, end);
    }

    /// <summary>
    /// Gets a token representing a literal number.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a literal number</returns>
    private Token LiteralNumber()
    {
        /* Tries to match the following pattern:
         * ([0-9](_?[0-9])*)+(\.([0-9](_?[0-9])*)+)?([Ee]?[+-]?([0-9](_?[0-9])*)+)?[LlFfDd]?
         * The sequence [0-9](_?[0-9])* is macthed by IntegralNumber.
         * Whenever the sequence starts with a dot, it's prepended a literal zero.
         */
        StringBuilder numberBuilder = new ();
        bool prependZero, isReal = false;
        char current;

        // Integral part: (\d[\d_]*)+
        prependZero = ReadDigits(numberBuilder);

        // Decimal part: (\.(\d[\d_]*)+)?
        current = Ll(1);
        if (current == '.' && Ll(2) is >= '0' and <= '9')
        {
            if (prependZero) numberBuilder.Append('0');
            numberBuilder.Append(current);
            Consume(1); // To skip '.'
            ReadDigits(numberBuilder);
            isReal = true;
            current = Ll(1);
        }

        // Exponent part: ([Ee][+-]?(\d[\d_]*)+)?
        if (current is 'e' or 'E')
        {
            numberBuilder.Append(current);
            Consume(1); // To skip 'e' or 'E'

            current = Ll(1);
            if (current is '+' or '-')
            {
                numberBuilder.Append(current);
                Consume(1); // To skip '+' or '-'
            }

            if (ReadDigits(numberBuilder))
                return MakeToken(TokenID.Unknown, numberBuilder.ToString(), false);

            isReal = true;
        }

        // Process the suffix: [LlFfDd]?
        return NumberWithSuffix(numberBuilder.ToString(), isReal);
    }

    /// <summary>
    /// Attempts to read a sequence of digit characters and underscores from the current position, appending them to the
    /// specified buffer.
    /// </summary>
    /// <remarks>
    /// This method appends consecutive digit characters ('0'-'9') and underscores ('_') to the buffer, starting from the current position.
    /// The method advances the position for each character read. If the first character is not a digit, no characters are appended and the method returns <b>true</b>.
    /// </remarks>
    /// <param name="buffer">The buffer to which the parsed digit characters and underscores are appended. Must not be null.</param>
    /// <returns><b>true</b> if no digit was found at the current position; otherwise, <b>false</b>.</returns>
    private bool ReadDigits(StringBuilder buffer)
    {
        char c = Ll(1);
        if (c is not (>= '0' and <= '9')) return true;

        buffer.Append(c);
        Consume(1);
        c = Ll(1);

        while (c is '_' or >= '0' and <= '9')
        {
            buffer.Append(c);
            Consume(1);
            c = Ll(1);
        }

        return false;
    }

    /// <summary>
    /// Parses a numeric string and returns a token representing the number, applying a type suffix if present.
    /// </summary>
    /// <remarks>
    /// Supported suffixes include 'f' or 'F' for float, 'i' or 'I' for complex, 'd' or 'D' for decimal, and 'l' or 'L' for long integers.
    /// If no recognized suffix is present, the method defaults to integer, long, or float types based on the input and context.
    /// </remarks>
    /// <param name="numberStr">The string representation of the number to parse. Must be a valid numeric value in invariant culture format.</param>
    /// <param name="isReal">Indicates whether the number should be treated as a real (floating-point) value. If <see langword="true"/>, real
    /// number suffixes are considered; otherwise, integer and other suffixes are processed.</param>
    /// <returns>A <see cref="Token"/> instance representing the parsed number, with its type determined by any recognized suffix.</returns>
    private Token NumberWithSuffix(string numberStr, bool isReal)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;

        if (isReal)
            return Ll(1) switch
            {
                'f' or 'F' => MakeToken(TokenID.LT_Float, double.Parse(numberStr, ci), true),
                'i' or 'I' => MakeToken(TokenID.LT_Complex, new Complex(0, double.Parse(numberStr, ci)), true),
                'd' or 'D' => MakeToken(TokenID.LT_Decimal, BigDecimal.Parse(numberStr), true),
                _ => MakeToken(TokenID.LT_Float, double.Parse(numberStr, ci), false),
            };

        return Ll(1) switch
        {
            'l' or 'L' => MakeToken(TokenID.LT_Long, BigInteger.Parse(numberStr, ci), true),
            'f' or 'F' => MakeToken(TokenID.LT_Float, double.Parse(numberStr, ci), true),
            'i' or 'I' => MakeToken(TokenID.LT_Complex, new Complex(0, double.Parse(numberStr, ci)), true),
            'd' or 'D' => MakeToken(TokenID.LT_Decimal, BigDecimal.Parse(numberStr), true),
            _ => int.TryParse(numberStr, ci, out var n)
               ? MakeToken(TokenID.LT_Integer, n, false)
               : MakeToken(TokenID.LT_Long, BigInteger.Parse(numberStr, ci), false),
        };
    }

    /// <summary>
    /// Gets a token representing a literal date and/or time.
    /// </summary>
    /// <returns>A literal date value</returns>
    private Token LiteralDate()
    {
        Consume(1);

        StringBuilder dateBuilder = new ();
        char ch = Ll(1);

        while (ch is not ('`' or EOF))
        {
            dateBuilder.Append(ch);
            Consume(1);
            ch = Ll(1);
        }

        string dateStr = dateBuilder.ToString();

        if (ch == EOF) return MakeToken(TokenID.Unknown, $"`{dateStr}", false);

        return DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, out var d)
             ? MakeToken(TokenID.LT_Date, d, true)
             : MakeToken(TokenID.Unknown, $"`{dateStr}`", true);
    }

    /// <summary>
    /// Gets a token representing a literal blob.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a literal blob</returns>
    private Token LiteralBlob()
    {
        Consume(1); // Skip 'b' or 'B'
        var blob = StringUtil.String2ByteArray((string)LiteralString().Value);
        return MakeToken(TokenID.LT_Blob, blob, false);
    }

    /// <summary>
    /// Gets a token representing a literal string.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a literal string</returns>
    private Token LiteralString()
    {
        char wrapper = Ll(1);
        Consume(1);

        StringBuilder strBuilder = new ();
        bool loop = true;
        char ch;

        while (loop)
        {
            ch = Ll(1);

            if (ch is '\r' or '\n' or EOF)
                return MakeToken(TokenID.Unknown, wrapper + strBuilder.ToString(), false);

            if (ch == wrapper)
                loop = false;
            else if (ch == '\\')
                strBuilder.Append(EscapeSequence());
            else
            {
                strBuilder.Append(ch);
                Consume(1);
            }
        }

        return MakeToken(TokenID.LT_String, strBuilder.ToString(), true);
    }

    /// <summary>
    /// Extract an escape sequence from the character stream.
    /// </summary>
    /// <returns>A <see cref="string"/></returns>
    private string EscapeSequence()
    {
        Consume(1); // Skip the initial \
        char ch = Ll(1);

        switch (ch)
        {
            case '\\' or '\'' or '"':
                Consume(1);
                return ch.ToString();
            case '0':
                Consume(1);
                return "\0";
            case 'a':
                Consume(1);
                return "\a";
            case 'b':
                Consume(1);
                return "\b";
            case 'f':
                Consume(1);
                return "\f";
            case 'n':
                Consume(1);
                return "\n";
            case 'r':
                Consume(1);
                return "\r";
            case 't':
                Consume(1);
                return "\t";
            case 'v':
                Consume(1);
                return "\v";
            case 'x' or 'X':
                return HexadecimalSequence(2);
            case 'u' or 'U':
                return HexadecimalSequence(4);
            default:
                return "\\";
        }
    }

    /// <summary>
    /// Extracts an hexadecimal escape sequence from the character stream.
    /// </summary>
    /// <param name="max">The maximum length of the escape sequence</param>
    /// <returns>A <see cref="string"/> made of hexadecimal digits</returns>
    private string HexadecimalSequence(int max)
    {
        StringBuilder hexBuilder = new ();
        bool ok = true;
        char ch;


        for (int i = 0; ok && i < max; ++i)
        {
            ch = Ll(i + 2);

            if (IsHexDigit(ch))
                hexBuilder.Append(ch);
            else
                ok = false;
        }

        string result;

        if (ok)
        {
            var value = int.Parse(hexBuilder.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            Consume(max + 1);
            result = ((char)value).ToString();
        }
        else
        {
            result = "\\" + Ll(1);
            Consume(1);
        }

        return result;
    }

    /// <summary>
    /// Gets a token representing an identifier or a keyword.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing an identifier or a keyword</returns>
    private Token Identifier()
    {
        StringBuilder idBuilder = new ();
        idBuilder.Append(Ll(1));
        Consume(1);

        bool loop = true;
        char ch;

        while (loop)
        {
            ch = Ll(1);

            if (IsLegalIdChar(ch))
            {
                idBuilder.Append(ch);
                Consume(1);
            }
            else if (ch == '\\')
                idBuilder.Append(EscapeSequence());
            else
                loop = false;
        }

        string word = idBuilder.ToString();

        if (Keyword.IsDefined(word))
        {
            Keyword kw = Keyword.Get(word);
            return MakeToken(kw.TokenID, kw.Value, false);
        }
        
        return MakeToken(TokenID.Identifier, word, false);
    }

    /// <summary>
    /// Gets a token with no particular meaning.
    /// </summary>
    /// <returns>A <see cref="Token"/> with no particular meaning</returns>
    private Token Unknown() => MakeToken(TokenID.Unknown, Ll(1).ToString(), true);

    /// <summary>
    /// Gets a token representing a symbol that starts with a colon.
    /// </summary>
    /// <returns>One between <b>::</b> and <b>:</b></returns>
    private Token Colon()
    {
        Consume(1);

        return Ll(1) switch
        {
            ':' => MakeToken(TokenID.DoubleColon, null, true),
            _ => MakeToken(TokenID.Colon, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with a question mark.
    /// </summary>
    /// <returns>One between <b>?.</b>, <b>??</b> and <b>?</b></returns>
    private Token QuestionMark()
    {
        Consume(1);

        switch (Ll(1))
        {
            case '.':
                return MakeToken(TokenID.QuestionDot, null, true);
            case '[':
                return MakeToken(TokenID.QuestionBracket, null, true);
            case '?':
                Consume(1);

                return Ll(1) switch
                {
                    '=' => MakeToken(TokenID.DoubleQuestionEqual, null, true),
                    _ => MakeToken(TokenID.DoubleQuestion, null, false),
                };
            default:
                return MakeToken(TokenID.Question, null, false);
        }
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with a dot.
    /// </summary>
    /// <returns>A range operator, a literal floating point number or a single dot</returns>
    private Token Dot()
    {
        char nextCh = Ll(2);

        if (nextCh == '.')
        {
            Consume(2);
            return MakeToken(TokenID.DoubleDot, null, false);
        }

        return char.IsDigit(nextCh) ? LiteralNumber() : MakeToken(TokenID.Dot, null, true);
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the equal sign.
    /// </summary>
    /// <returns>One between <b>===</b>, <b>==</b>, <b>=></b> and <b>=</b></returns>
    private Token EqualSign()
    {
        Consume(1);

        switch (Ll(1))
        {
            case '=':
                Consume(1);

                return Ll(1) switch
                {
                    '=' => MakeToken(TokenID.TripleEqual, null, true),
                    _ => MakeToken(TokenID.DoubleEqual, null, false),
                };
            case '>':
                return MakeToken(TokenID.Arrow, null, true);
            default:
                return MakeToken(TokenID.Equal, null, false);
        }
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with an exclamation mark.
    /// </summary>
    /// <returns>One between <b>!=</b> and <b>!</b></returns>
    private Token ExclamationMark()
    {
        Consume(1);

        switch (Ll(1))
        {
            case '=':
                Consume(1);

                return Ll(1) switch
                {
                    '=' => MakeToken(TokenID.ExclamationDoubleEqual, null, true),
                    _ => MakeToken(TokenID.ExclamationEqual, null, false),
                };
            default:
                return MakeToken(TokenID.Exclamation, null, false);
        }
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the &lt; symbol.
    /// </summary>
    /// <returns>One between <b>&lt;&lt;=</b>, <b>&lt;&lt;</b>, <b>&lt;=</b> and <b>&lt;</b></returns>
    private Token Lt()
    {
        Consume(1);

        switch (Ll(1))
        {
            case '<':
                Consume(1);

                return Ll(1) switch
                {
                    '=' => MakeToken(TokenID.DoubleLessThanEqual, null, true),
                    _ => MakeToken(TokenID.DoubleLessThan, null, false),
                };
            case '=':
                return MakeToken(TokenID.LessThanEqual, null, true);
            default:
                return MakeToken(TokenID.LessThan, null, false);
        }
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the &gt; symbol.
    /// </summary>
    /// <returns>One between <b>&gt;&gt;=</b>, <b>&gt;&gt;</b>, <b>&gt;=</b> and <b>&gt;</b></returns>
    private Token Gt()
    {
        Consume(1);

        switch (Ll(1))
        {
            case '>':
                Consume(1);

                return Ll(1) switch
                {
                    '=' => MakeToken(TokenID.DoubleGreaterThanEqual, null, true),
                    _ => MakeToken(TokenID.DoubleGreaterThan, null, false),
                };
            case '=':
                return MakeToken(TokenID.GreaterThanEqual, null, true);
            default:
                return MakeToken(TokenID.GreaterThan, null, false);
        }
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the + sign.
    /// </summary>
    /// <returns>One between <b>++</b>, <b>+=</b> and <b>+</b></returns>
    private Token PlusSign()
    {
        Consume(1);

        return Ll(1) switch
        {
            '+' => MakeToken(TokenID.DoublePlus, null, true),
            '=' => MakeToken(TokenID.PlusEqual, null, true),
            _ => MakeToken(TokenID.Plus, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the - sign.
    /// </summary>
    /// <returns>One between <b>--</b>, <b>-=</b> and <b>-</b></returns>
    private Token MinusSign()
    {
        Consume(1);

        return Ll(1) switch
        {
            '-' => MakeToken(TokenID.DoubleMinus, null, true),
            '=' => MakeToken(TokenID.MinusEqual, null, true),
            _ => MakeToken(TokenID.Minus, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with an asterisk.
    /// </summary>
    /// <returns>One between <b>**=</b>, <b>**</b>, <b>*=</b> and <b>*</b></returns>
    private Token Asterisk()
    {
        Consume(1);

        switch (Ll(1))
        {
            case '*':
                Consume(1);

                return Ll(1) switch
                {
                    '=' => MakeToken(TokenID.DoubleAsteriskEqual, null, true),
                    _ => MakeToken(TokenID.DoubleAsterisk, null, false),
                };
            case '=':
                return MakeToken(TokenID.AsteriskEqual, null, true);
            default:
                return MakeToken(TokenID.Asterisk, null, false);
        }
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with a forward slash.
    /// </summary>
    /// <returns>One between <b>/=</b>, <b>/</b> and comments</returns>
    private Token ForwardSlash()
    {
        Consume(1);

        return Ll(1) switch
        {
            '/' => LineComment(),
            '*' => BlockComment(),
            '=' => MakeToken(TokenID.SlashEqual, null, true),
            _ => MakeToken(TokenID.Slash, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a line comment.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a line comment</returns>
    private Token LineComment()
    {
        var cmntBldr = new StringBuilder();
        bool loop = true;

        while (loop)
        {
            Consume(1);
            char ch = Ll(1);

            switch (ch)
            {
                case '\r' or '\n' or EOF:
                    loop = false;
                    break;
                default:
                    cmntBldr.Append(ch);
                    break;
            }
        }

        return MakeToken(TokenID.LineComment, cmntBldr.ToString(), false);
    }

    /// <summary>
    /// Gets a token representing a block comment.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a block comment</returns>
    private Token BlockComment()
    {
        var cmntBldr = new StringBuilder();
        bool loop = true;

        while (loop)
        {
            Consume(1);
            char ch = Ll(1);

            switch (ch)
            {
                case EOF:
                    loop = false;
                    break;
                case '*':
                    if (Ll(2) == '/')
                    {
                        Consume(1);
                        loop = false;
                    }
                    else
                        cmntBldr.Append(ch);
                    break;
                default:
                    cmntBldr.Append(ch);
                    break;
            }
        }

        return MakeToken(TokenID.BlockComment, cmntBldr.ToString(), true);
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the % symbol.
    /// </summary>
    /// <returns>One between <b>%=</b> and <b>%</b></returns>
    private Token PercentSign()
    {
        Consume(1);

        return Ll(1) switch
        {
            '=' => MakeToken(TokenID.PercentEqual, null, true),
            _ => MakeToken(TokenID.Percent, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the &amp; symbol.
    /// </summary>
    /// <returns>One between <b>&amp;&amp;</b>, <b>&amp;=</b> and <b>&amp;</b></returns>
    private Token Ampersand()
    {
        Consume(1);

        return Ll(1) switch
        {
            '&' => MakeToken(TokenID.DoubleAmpersand, null, true),
            '=' => MakeToken(TokenID.AmpersandEqual, null, true),
            _ => MakeToken(TokenID.Ampersand, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the | symbol.
    /// </summary>
    /// <returns>One between <b>||</b>, <b>|=</b> and <b>|</b></returns>
    private Token Verticalbar()
    {
        Consume(1);

        return Ll(1) switch
        {
            '|' => MakeToken(TokenID.DoubleVerticalBar, null, true),
            '=' => MakeToken(TokenID.VerticalBarEqual, null, true),
            _ => MakeToken(TokenID.VerticalBar, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the ^ symbol.
    /// </summary>
    /// <returns>One between <b>^=</b> and <b>^</b></returns>
    private Token CircumflexAccent()
    {
        Consume(1);

        return Ll(1) switch
        {
            '=' => MakeToken(TokenID.CircumflexEqual, null, true),
            _ => MakeToken(TokenID.Circumflex, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with a zero.
    /// </summary>
    /// <returns>A literal zero, a floating-point number or an hexadecimal number</returns>
    private Token Zero()
    {
        Consume(1);
        char ch = Ll(1);

        return ch switch
        {
            'l' or 'L' => MakeToken(TokenID.LT_Long, BigInteger.Zero, true),
            'f' or 'F' => MakeToken(TokenID.LT_Float, 0D, true),
            'd' or 'D' => MakeToken(TokenID.LT_Decimal, BigDecimal.Zero, true),
            'i' or 'I' => MakeToken(TokenID.LT_Complex, Complex.Zero, true),
            'x' or 'X' => HexNumber(),
            '.' => char.IsDigit(Ll(2))
                 ? LiteralNumber()
                 : MakeToken(TokenID.LT_Integer, 0, false),
            _ when char.IsDigit(ch) => LiteralNumber(),
            _ => MakeToken(TokenID.LT_Integer, 0, false)
        };
    }

    /// <summary>
    /// Gets a token representing an hexadecimal number.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing an hexadecimal number</returns>
    private Token HexNumber()
    {
        Consume(1);

        StringBuilder hexNumBuilder = new ();
        CultureInfo ci = CultureInfo.InvariantCulture;
        bool loop = true;
        char ch;
        
        while (loop)
        {
            ch = Ll(1);

            if (IsHexDigit(ch))
            {
                hexNumBuilder.Append(ch);
                Consume(1);
            }
            else
                loop = false;
        }

        string hexNumStr = hexNumBuilder.ToString();

        return Ll(1) switch
        {
            'l' or 'L' => MakeToken(TokenID.LT_Long, BigInteger.Parse(hexNumStr, NumberStyles.HexNumber, ci), true),
            _ => int.TryParse(hexNumStr, NumberStyles.HexNumber, ci, out int anInt)
               ? MakeToken(TokenID.LT_Integer, anInt, false)
               : MakeToken(TokenID.LT_Long, BigInteger.Parse(hexNumStr, NumberStyles.HexNumber, ci), false),
        };
    }

    /// <summary>
    /// Recognizes a verbatim string (one that starts with '@' and can span on multiple lines).
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a literal string</returns>
    private Token VerbatimString()
    {
        // Skip the initial '@' sign
        Consume(1);

        char wrapper = Ll(1);
        if (wrapper is not ('\'' or '"'))
            return MakeToken(TokenID.Unknown, "@", false);

        // Skip the wrapper
        Consume(1);

        StringBuilder strBuilder = new ();
        bool loop = true;
        char ch;

        while (loop)
        {
            ch = Ll(1);

            if (ch == EOF)
                return MakeToken(TokenID.Unknown, $"@{wrapper}{strBuilder}", false);

            if (ch == wrapper)
                if (Ll(2) == ch)
                {
                    strBuilder.Append(ch);
                    Consume(2);
                }
                else
                    loop = false;
            else
            {
                strBuilder.Append(ch);
                Consume(1);
            }
        }

        return MakeToken(TokenID.LT_String, strBuilder.ToString(), true);
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the $ symbol.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a format string or a special identifier</returns>
    private Token DollarSign()
    {
        // Skip '$'
        Consume(1);

        /*
         * Try to recognize a mutable string:
         * Normally we should have dedicated methods for this, but LiteralString and Verbatim can do the job
         */
        var tmpTok = Ll(1) switch
        {
            '\'' or '"' => LiteralString(),
            '@' => VerbatimString(),
            _ => null
        };

        if (tmpTok != null)
            return tmpTok.TokenID == TokenID.Unknown
                 ? tmpTok
                 : MakeToken(TokenID.MutableString, tmpTok.ToString(), false);

        // Try to recognize a special identifier
        StringBuilder idBuilder = new ();
        bool loop = true;

        while (loop)
        {
            char ch = Ll(1);

            if (IsLegalIdChar(ch))
            {
                idBuilder.Append(ch);
                Consume(1);
            }
            else if (ch == '\\')
                idBuilder.Append(EscapeSequence());
            else
                loop = false;
        }

        return idBuilder.Length > 0
             ? MakeToken(TokenID.Identifier, idBuilder.ToString(), false)
             : MakeToken(TokenID.Unknown, "$", false);
    }

    /// <summary>
    /// Gets a character without removing it from the buffer.
    /// </summary>
    /// <param name="k">The relative index of the character to peek</param>
    /// <returns>A <see cref="System.Char"/></returns>
    private char Ll(int k)
    {
        Trace.Assert(k > 0);

        int realIndex = charIndex + k;

        while (buffer.Length < realIndex)
        {
            int code = input.Read();
            if (code < 0)
                buffer.Append(EOF);
            else
                buffer.Append((char) code);
        }

        return buffer[realIndex - 1];
    }

    /// <summary>
    /// Skips a number of characters and peeks the next from the buffer.
    /// </summary>
    /// <param name="count">The number of characters to skip</param>
    private void Consume(int count)
    {
        Trace.Assert(count > 0 && charIndex + count <= buffer.Length);

        for (int i = 0, j = charIndex; i < count; ++i, ++j)
        {
            ++offset;
            if (buffer[j] == '\n')
            {
                lineOffset = offset;
                ++lineNumber;
            }
        }

        charIndex += count;
        
        while (charIndex >= MAX_BUFFER_SIZE)
        {
            buffer.Remove(0, MAX_BUFFER_SIZE);
            charIndex -= MAX_BUFFER_SIZE;
        }
    }

    /// <summary>
    /// Advances the input stream's reader until a non-white space character is reached.
    /// </summary>
    /// <returns>A <see cref="System.Char"/></returns>
    private char SkipWhiteSpace()
    {
        char ch = Ll(1);

        while (char.IsWhiteSpace(ch))
        {
            ++startOffset;
            if (ch == '\n')
            {
                startLineOffset = startOffset;
                ++startLineNumber;
            }

            Consume(1);
            ch = Ll(1);
        }

        return ch;
    }

    /// <summary>
    /// Checks whether a character is a valid base-16 digit or not.
    /// </summary>
    /// <param name="c">The character to check</param>
    /// <returns>
    /// <b>true</b> if <paramref name="c"/> is a decimal digit or a letter between 'A' and 'F'. <b>false</b> otherwise.
    /// </returns>
    public static bool IsHexDigit(char c) =>
        c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';

    /// <summary>
    /// Gets if a character can figure at the beginning of an identifier.
    /// </summary>
    /// <param name="ch">The given character</param>
    /// <returns><b>true</b> if the character is a letter or the underscore; <b>false</b> otherwise</returns>
    public static bool IsLegalFirstIdChar(char ch) =>
        char.IsLetter(ch) || ch == '_' || (0xc0 <= ch && ch < 0xd7) || (0xd7 < ch && ch < 0xf7) || (0xf7 < ch && ch <= 0xff);

    /// <summary>
    /// Gets if a character can figure in an identifier.
    /// </summary>
    /// <param name="ch">The given character</param>
    /// <returns><b>true</b> if the character is a letter, a digit or the underscore; <b>false</b> otherwise</returns>
    public static bool IsLegalIdChar(char ch) => IsLegalFirstIdChar(ch) || char.IsDigit(ch);
}