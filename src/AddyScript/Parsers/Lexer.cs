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
    private readonly string fileName;
    private readonly StringBuilder buffer = new();
    private int charIndex, offset, lineOffset, lineNumber;
    private int startOffset, startLineOffset, startLineNumber;

    /// <summary>
    /// Initializes a new instance of Lexer.
    /// </summary>
    /// <param name="input">Reads characters for the lexer</param>
    public Lexer(TextReader input)
    {
        this.input = input;

        if (input is StreamReader sr && sr.BaseStream is FileStream fs)
            fileName = fs.Name;
        else
            fileName = ":memory:";
    }

    /// <summary>
    /// Gets the name of the source file that's being parsed.
    /// </summary>
    public string FileName => fileName;

    /// <summary>
    /// Iterates through the character stream until a token is
    /// recognized or until the end of the buffer is reached.
    /// </summary>
    /// <returns>A <see cref="Token"/></returns>
    public Token NextToken()
    {
        InitToken();

        char ch = SkipWhiteSpace();

        return ch switch
        {
            EOF => Token(TokenID.EndOfFile, null, true),
            ',' => Token(TokenID.Comma, null, true),
            ';' => Token(TokenID.SemiColon, null, true),
            '(' => Token(TokenID.LeftParenthesis, null, true),
            ')' => Token(TokenID.RightParenthesis, null, true),
            '[' => Token(TokenID.LeftBracket, null, true),
            ']' => Token(TokenID.RightBracket, null, true),
            '{' => Token(TokenID.LeftBrace, null, true),
            '}' => Token(TokenID.RightBrace, null, true),
            '~' => Token(TokenID.Tilda, null, true),
            ':' => Colon(),
            '?' => Question(),
            '.' => Dot(),
            '=' => Equal(),
            '!' => Exclamation(),
            '<' => Lt(),
            '>' => Gt(),
            '+' => Plus(),
            '-' => Minus(),
            '*' => Asterisk(),
            '/' => Slash(),
            '%' => Percent(),
            '&' => Ampersand(),
            '|' => Vbar(),
            '^' => Circumflex(),
            '0' => Zero(),
            '@' => Verbatim(),
            '\'' or '"' => LiteralString(),
            '$' => Dollar(),
            '`' => Backtick(),
            'b' or 'B' => LetterB(),
            _ => IsLegalFirstIdChar(ch) ? Identifier() : char.IsDigit(ch) ? Number() : Unknown(),
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
    private Token Token(TokenID tokenID, object value, bool consume)
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
    private Token Number()
    {
        /* Tries to match the following pattern:
         * ([0-9](_?[0-9])*)+(\.([0-9](_?[0-9])*)+)?([Ee]?[+-]?([0-9](_?[0-9])*)+)?[LlFfDd]?
         * Whenever the sequence starts with a dot, it's prepended a literal zero.
         */
        var nbrBldr = new StringBuilder();
        CultureInfo ci = CultureInfo.InvariantCulture;
        bool isReal = false, prependZero = true;
        char ch = Ll(1);

        // Integral part: ([0-9](_?[0-9])*)+
        while (char.IsDigit(ch))
        {
            prependZero = false;
            nbrBldr.Append(ch);
            Consume(1);
            ch = Ll(1);
            if (ch == '_' && char.IsDigit(Ll(2)))
            {
                Consume(1);
                ch = Ll(1);
            }
        }

        // Decimal part: (\.([0-9](_?[0-9])*)+)?
        if (ch == '.' && char.IsDigit(Ll(2)))
        {
            isReal = true;
            if (prependZero) nbrBldr.Append('0');
            nbrBldr.Append(ch).Append(Ll(2));
            Consume(2);
            ch = Ll(1);

            while (char.IsDigit(ch))
            {
                nbrBldr.Append(ch);
                Consume(1);
                ch = Ll(1);
                if (ch == '_' && char.IsDigit(Ll(2)))
                {
                    Consume(1);
                    ch = Ll(1);
                }
            }
        }

        // Exponent part: ([Ee]?[+-]?([0-9](_?[0-9])*)+)?
        if (ch == 'e' || ch == 'E')
        {
            bool error = true;
            isReal = true;
            nbrBldr.Append(ch);
            Consume(1);
            ch = Ll(1);

            if (ch == '+' || ch == '-')
            {
                nbrBldr.Append(ch);
                Consume(1);
                ch = Ll(1);
            }

            while (char.IsDigit(ch))
            {
                error = false;
                nbrBldr.Append(ch);
                Consume(1);
                ch = Ll(1);
                if (ch == '_' && char.IsDigit(Ll(2)))
                {
                    Consume(1);
                    ch = Ll(1);
                }
            }

            if (error) return Token(TokenID.Unknown, nbrBldr.ToString(), false);
        }

        // Suffix part: [LlFfDd]?
        string nbrStr = nbrBldr.ToString();

        if (isReal)
            return Ll(1) switch
            {
                'f' or 'F' => double.TryParse(nbrStr, NumberStyles.Float, ci, out double aDouble)
                            ? Token(TokenID.LT_Float, aDouble, true)
                            : Token(TokenID.Unknown, nbrStr, true),
                'i' or 'I' => double.TryParse(nbrStr, NumberStyles.Float, ci, out double aDouble)
                            ? Token(TokenID.LT_Complex, new Complex(0, aDouble), true)
                            : Token(TokenID.Unknown, nbrStr, true),
                'd' or 'D' => Token(TokenID.LT_Decimal, new BigDecimal(nbrStr), true),
                _ => double.TryParse(nbrStr, NumberStyles.Float, ci, out double aDouble)
                   ? Token(TokenID.LT_Float, aDouble, false)
                   : Token(TokenID.Unknown, nbrStr, false),
            };

        return Ll(1) switch
        {
            'l' or 'L' => Token(TokenID.LT_Long, BigInteger.Parse(nbrStr), true),
            'f' or 'F' => double.TryParse(nbrStr, NumberStyles.Float, ci, out double aDouble)
                        ? Token(TokenID.LT_Float, aDouble, true)
                        : Token(TokenID.Unknown, nbrStr, true),
            'i' or 'I' => double.TryParse(nbrStr, NumberStyles.Float, ci, out double aDouble)
                        ? Token(TokenID.LT_Complex, new Complex(0, aDouble), true)
                        : Token(TokenID.Unknown, nbrStr, true),
            'd' or 'D' => Token(TokenID.LT_Decimal, new BigDecimal(nbrStr), true),
            _ => int.TryParse(nbrStr, NumberStyles.Integer, ci, out int anInt)
               ? Token(TokenID.LT_Integer, anInt, false)
               : Token(TokenID.LT_Long, BigInteger.Parse(nbrStr), false),
        };
    }

    /// <summary>
    /// Gets a token representing a literal string.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a literal string</returns>
    private Token LiteralString()
    {
        char wrapper = Ll(1);
        Consume(1);

        var strBldr = new StringBuilder();
        bool loop = true;
        char ch;

        while (loop)
        {
            ch = Ll(1);

            if (ch == '\r' || ch == '\n' || ch == EOF)
                return Token(TokenID.Unknown, wrapper + strBldr.ToString(), false);

            if (ch == wrapper)
                loop = false;
            else if (ch == '\\')
                strBldr.Append(Escape());
            else
            {
                strBldr.Append(ch);
                Consume(1);
            }
        }

        return Token(TokenID.LT_String, strBldr.ToString(), true);
    }

    /// <summary>
    /// Extract an escape sequence from the character stream.
    /// </summary>
    /// <returns>A <see cref="string"/></returns>
    private string Escape()
    {
        Consume(1);

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
                return EscapeHex(2);
            case 'u' or 'U':
                return EscapeHex(4);
            default:
                return "\\";
        }
    }

    /// <summary>
    /// Extracts an hexadecimal escape sequence from the character stream.
    /// </summary>
    /// <param name="max">The maximum length of the escape sequence</param>
    /// <returns>A <see cref="string"/></returns>
    private string EscapeHex(int max)
    {
        var escBldr = new StringBuilder();
        bool ok = true;
        char ch;


        for (int i = 0; ok && i < max; ++i)
        {
            ch = Ll(i + 2);

            if (('0' <= ch && ch <= '9') || ('A' <= ch && ch <= 'F') || ('a' <= ch && ch <= 'f'))
                escBldr.Append(ch);
            else
                ok = false;
        }

        string result;

        if (ok)
        {
            int value = int.Parse(escBldr.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
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
        var idBldr = new StringBuilder();
        idBldr.Append(Ll(1));
        Consume(1);

        bool loop = true;
        char ch;

        while (loop)
        {
            ch = Ll(1);

            if (IsLegalIdChar(ch))
            {
                idBldr.Append(ch);
                Consume(1);
            }
            else if (ch == '\\')
                idBldr.Append(Escape());
            else
                loop = false;
        }

        string word = idBldr.ToString();

        if (Keyword.IsDefined(word))
        {
            Keyword kw = Keyword.Get(word);
            return Token(kw.TokenID, kw.Value, false);
        }
        
        return Token(TokenID.Identifier, word, false);
    }

    /// <summary>
    /// Gets a token with no particular meaning.
    /// </summary>
    /// <returns>A <see cref="Token"/> with no particular meaning</returns>
    private Token Unknown()
    {
        return Token(TokenID.Unknown, Ll(1).ToString(), true);
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with a colon.
    /// </summary>
    /// <returns>One between <b>::</b> and <b>:</b></returns>
    private Token Colon()
    {
        Consume(1);

        return Ll(1) switch
        {
            ':' => Token(TokenID.DoubleColon, null, true),
            _ => Token(TokenID.Colon, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with a question mark.
    /// </summary>
    /// <returns>One between <b>?.</b>, <b>??</b> and <b>?</b></returns>
    private Token Question()
    {
        Consume(1);

        switch (Ll(1))
        {
            case '.':
                return Token(TokenID.QuestionDot, null, true);
            case '[':
                return Token(TokenID.QuestionBracket, null, true);
            case '?':
                Consume(1);

                return Ll(1) switch
                {
                    '=' => Token(TokenID.DoubleQuestionEqual, null, true),
                    _ => Token(TokenID.DoubleQuestion, null, false),
                };
            default:
                return Token(TokenID.Question, null, false);
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
            return Token(TokenID.DoubleDot, null, false);
        }

        return char.IsDigit(nextCh) ? Number() : Token(TokenID.Dot, null, true);
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the equal sign.
    /// </summary>
    /// <returns>One between <b>===</b>, <b>==</b>, <b>=></b> and <b>=</b></returns>
    private Token Equal()
    {
        Consume(1);

        switch (Ll(1))
        {
            case '=':
                Consume(1);

                return Ll(1) switch
                {
                    '=' => Token(TokenID.TripleEqual, null, true),
                    _ => Token(TokenID.DoubleEqual, null, false),
                };
            case '>':
                return Token(TokenID.Arrow, null, true);
            default:
                return Token(TokenID.Equal, null, false);
        }
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with an exclamation mark.
    /// </summary>
    /// <returns>One between <b>!=</b> and <b>!</b></returns>
    private Token Exclamation()
    {
        Consume(1);

        switch (Ll(1))
        {
            case '=':
                Consume(1);

                return Ll(1) switch
                {
                    '=' => Token(TokenID.ExclamationDoubleEqual, null, true),
                    _ => Token(TokenID.ExclamationEqual, null, false),
                };
            default:
                return Token(TokenID.Exclamation, null, false);
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
                    '=' => Token(TokenID.DoubleLessThanEqual, null, true),
                    _ => Token(TokenID.DoubleLessThan, null, false),
                };
            case '=':
                return Token(TokenID.LessThanEqual, null, true);
            default:
                return Token(TokenID.LessThan, null, false);
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
                    '=' => Token(TokenID.DoubleGreaterThanEqual, null, true),
                    _ => Token(TokenID.DoubleGreaterThan, null, false),
                };
            case '=':
                return Token(TokenID.GreaterThanEqual, null, true);
            default:
                return Token(TokenID.GreaterThan, null, false);
        }
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the + sign.
    /// </summary>
    /// <returns>One between <b>++</b>, <b>+=</b> and <b>+</b></returns>
    private Token Plus()
    {
        Consume(1);

        return Ll(1) switch
        {
            '+' => Token(TokenID.DoublePlus, null, true),
            '=' => Token(TokenID.PlusEqual, null, true),
            _ => Token(TokenID.Plus, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the - sign.
    /// </summary>
    /// <returns>One between <b>--</b>, <b>-=</b> and <b>-</b></returns>
    private Token Minus()
    {
        Consume(1);

        return Ll(1) switch
        {
            '-' => Token(TokenID.DoubleMinus, null, true),
            '=' => Token(TokenID.MinusEqual, null, true),
            _ => Token(TokenID.Minus, null, false),
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
                    '=' => Token(TokenID.DoubleAsteriskEqual, null, true),
                    _ => Token(TokenID.DoubleAsterisk, null, false),
                };
            case '=':
                return Token(TokenID.AsteriskEqual, null, true);
            default:
                return Token(TokenID.Asterisk, null, false);
        }
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with a slash.
    /// </summary>
    /// <returns>One between <b>/=</b>, <b>/</b> and comments</returns>
    private Token Slash()
    {
        Consume(1);

        return Ll(1) switch
        {
            '/' => LineComment(),
            '*' => BlockComment(),
            '=' => Token(TokenID.SlashEqual, null, true),
            _ => Token(TokenID.Slash, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a line-end comment.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a line-end comment</returns>
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

        return Token(TokenID.LineComment, cmntBldr.ToString(), false);
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

        return Token(TokenID.BlockComment, cmntBldr.ToString(), true);
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the % symbol.
    /// </summary>
    /// <returns>One between <b>%=</b> and <b>%</b></returns>
    private Token Percent()
    {
        Consume(1);

        return Ll(1) switch
        {
            '=' => Token(TokenID.PercentEqual, null, true),
            _ => Token(TokenID.Percent, null, false),
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
            '&' => Token(TokenID.DoubleAmpersand, null, true),
            '=' => Token(TokenID.AmpersandEqual, null, true),
            _ => Token(TokenID.Ampersand, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the | symbol.
    /// </summary>
    /// <returns>One between <b>||</b>, <b>|=</b> and <b>|</b></returns>
    private Token Vbar()
    {
        Consume(1);

        return Ll(1) switch
        {
            '|' => Token(TokenID.DoubleVerticalBar, null, true),
            '=' => Token(TokenID.VerticalBarEqual, null, true),
            _ => Token(TokenID.VerticalBar, null, false),
        };
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the ^ symbol.
    /// </summary>
    /// <returns>One between <b>^=</b> and <b>^</b></returns>
    private Token Circumflex()
    {
        Consume(1);

        return Ll(1) switch
        {
            '=' => Token(TokenID.CircumflexEqual, null, true),
            _ => Token(TokenID.Circumflex, null, false),
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

        switch (ch)
        {
            case 'l' or 'L':
                return Token(TokenID.LT_Long, BigInteger.Zero, true);
            case 'f' or 'F':
                return Token(TokenID.LT_Float, 0D, true);
            case 'd' or 'D':
                return Token(TokenID.LT_Decimal, BigDecimal.Zero, true);
            case 'i' or 'I':
                return Token(TokenID.LT_Complex, Complex.Zero, true);
            case '.':
                return char.IsDigit(Ll(2)) ? Number() : Token(TokenID.LT_Integer, 0, false);
            case 'x' or 'X':
                return HexNumber();
            default:
                if (char.IsDigit(ch)) return Number();
                return Token(TokenID.LT_Integer, 0, false);
        }
    }

    /// <summary>
    /// Gets a token representing an hexadecimal number.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing an hexadecimal number</returns>
    private Token HexNumber()
    {
        Consume(1);

        var hexNbrBldr = new StringBuilder();
        CultureInfo ci = CultureInfo.InvariantCulture;
        bool loop = true;
        char ch;
        
        while (loop)
        {
            ch = Ll(1);

            if (('0' <= ch && ch <= '9') || ('A' <= ch && ch <= 'F') || ('a' <= ch && ch <= 'f'))
            {
                hexNbrBldr.Append(ch);
                Consume(1);
            }
            else
                loop = false;
        }

        string hexNumStr = hexNbrBldr.ToString();

        return Ll(1) switch
        {
            'l' or 'L' => Token(TokenID.LT_Long, BigInteger.Parse(hexNumStr, NumberStyles.HexNumber, ci), true),
            _ => int.TryParse(hexNumStr, NumberStyles.HexNumber, ci, out int anInt)
               ? Token(TokenID.LT_Integer, anInt, false)
               : Token(TokenID.LT_Long, BigInteger.Parse(hexNumStr, NumberStyles.HexNumber, ci), false),
        };
    }

    /// <summary>
    /// Recognizes a verbatim string (one that starts with '@').
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a literal string</returns>
    private Token Verbatim()
    {
        // Skip '@'
        Consume(1);

        char wrapper = Ll(1);
        if (!(wrapper == '\'' || wrapper == '"'))
            return Token(TokenID.Unknown, "@", false);

        // Skip wrapper
        Consume(1);

        var sb = new StringBuilder();
        bool loop = true;
        char ch;

        while (loop)
        {
            ch = Ll(1);

            if (ch == EOF)
                return Token(TokenID.Unknown, "@" + wrapper + sb, false);

            if (ch == wrapper)
                if (Ll(2) == ch)
                {
                    sb.Append(ch);
                    Consume(2);
                }
                else
                    loop = false;
            else
            {
                sb.Append(ch);
                Consume(1);
            }
        }

        return Token(TokenID.LT_String, sb.ToString(), true);
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with the $ symbol.
    /// </summary>
    /// <returns>A <see cref="Token"/> representing a format string or a special identifier</returns>
    private Token Dollar()
    {
        // Skip '$'
        Consume(1);

        Token tmpTok = null;
        char ch = Ll(1);

        /*
         * Try to recognize a format string:
         * Normally we should have dedicated methods for this but LiteralString and Verbatim can do the job
         */
        switch (ch)
        {
            case '\'' or '"':
                tmpTok = LiteralString();
                break;
            case '@':
                tmpTok = Verbatim();
                break;
        }

        if (tmpTok != null)
            return tmpTok.TokenID == TokenID.Unknown
                 ? tmpTok
                 : Token(TokenID.MutableString, tmpTok.ToString(), false);

        // Try to recognize a special identifier
        var sb = new StringBuilder();
        bool loop = true;

        while (loop)
        {
            ch = Ll(1);

            if (IsLegalIdChar(ch))
            {
                sb.Append(ch);
                Consume(1);
            }
            else if (ch == '\\')
                sb.Append(Escape());
            else
                loop = false;
        }

        return sb.Length > 0
             ? Token(TokenID.Identifier, sb.ToString(), false)
             : Token(TokenID.Unknown, "$", false);
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with <b>`</b>.
    /// </summary>
    /// <returns>A literal date value</returns>
    private Token Backtick()
    {
        Consume(1);

        var sb = new StringBuilder();
        char ch = Ll(1);

        while (ch != '`' && ch != EOF)
        {
            sb.Append(ch);
            Consume(1);
            ch = Ll(1);
        }

        return ch == EOF
             ? Token(TokenID.Unknown, "`" + sb, false)
             : DateTime.TryParse(sb.ToString(), out DateTime aDateTime)
             ? Token(TokenID.LT_Date, aDateTime, true)
             : Token(TokenID.Unknown, "`" + sb + "`", true);
    }

    /// <summary>
    /// Gets a token representing a symbol that starts with <b>b</b> or <b>B</b>.
    /// </summary>
    /// <returns>A banary string</returns>
    private Token LetterB()
    {
        char ch = Ll(2);

        if (ch == '\'' || ch == '"')
        {
            Consume(1);
            return Token(TokenID.LT_Blob, StringUtil.String2ByteArray((string)LiteralString().Value), false);
        }

        return Identifier();
    }

    /// <summary>
    /// Gets a character without removing it from the buffer.
    /// </summary>
    /// <param name="k">The relative index of the character to peek</param>
    /// <returns>A <see cref="System.Char"/></returns>
    private char Ll(int k)
    {
        Debug.Assert(k > 0);

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
        Debug.Assert(count > 0 && charIndex + count <= buffer.Length);

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
    /// Gets if a character can figure at the beginning of an identifier.
    /// </summary>
    /// <param name="ch">The given character</param>
    /// <returns><b>true</b> if the character is a letter or the underscore; <b>false</b> otherwise</returns>
    public static bool IsLegalFirstIdChar(char ch)
    {
        return char.IsLetter(ch) ||
               ch == '_' ||
               (0xc0 <= ch && ch < 0xd7) ||
               (0xd7 < ch && ch < 0xf7) ||
               (0xf7 < ch && ch <= 0xff);
    }

    /// <summary>
    /// Gets if a character can figure in an identifier.
    /// </summary>
    /// <param name="ch">The given character</param>
    /// <returns><b>true</b> if the character is a letter, a digit or the underscore; <b>false</b> otherwise</returns>
    public static bool IsLegalIdChar(char ch)
    {
        return IsLegalFirstIdChar(ch) || char.IsDigit(ch);
    }
}