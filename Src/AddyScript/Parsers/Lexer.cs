using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Parsers
{
    /// <summary>
    /// AddyScript's lexer.
    /// </summary>
    public class Lexer
    {
        private const char EOF = char.MinValue;
        private const int MAX_BUFFER_SIZE = 512;

        private readonly TextReader input;
        private readonly string fileName;
        private readonly StringBuilder buffer = new StringBuilder();
        private int charIndex, offset, lineOffset, lineNumber;
        private int startOffset, startLineOffset, startLineNumber;

        /// <summary>
        /// Initializes a new instance of Lexer.
        /// </summary>
        /// <param name="input">Reads characters for the lexer</param>
        public Lexer(TextReader input)
        {
            this.input = input;
            if (input is StreamReader)
            {
                var streamReader = (StreamReader) input;
                if (streamReader.BaseStream is FileStream)
                {
                    var fileStream = (FileStream) streamReader.BaseStream;
                    fileName = fileStream.Name;
                }
            }
        }

        /// <summary>
        /// Gets the name of the source file being parsed.
        /// </summary>
        public string FileName
        {
            get { return fileName; }
        }

        /// <summary>
        /// Iterates through the character stream until a token is
        /// recognized or until the end of the buffer is reached.
        /// </summary>
        /// <returns>A <see cref="Token"/></returns>
        public Token NextToken()
        {
            InitToken();

            char ch = SkipWhiteSpace();

            switch (ch)
            {
                case EOF:
                    return Token(TokenID.EndOfFile, null, true);
                case ',':
                    return Token(TokenID.Comma, null, true);
                case ';':
                    return Token(TokenID.SemiColon, null, true);
                case '(':
                    return Token(TokenID.LeftParenthesis, null, true);
                case ')':
                    return Token(TokenID.RightParenthesis, null, true);
                case '[':
                    return Token(TokenID.LeftBracket, null, true);
                case ']':
                    return Token(TokenID.RightBracket, null, true);
                case '{':
                    return Token(TokenID.LeftBrace, null, true);
                case '}':
                    return Token(TokenID.RightBrace, null, true);
                case '~':
                    return Token(TokenID.Tilda, null, true);
                case ':':
                    return Colon();
                case '?':
                    return Question();
                case '.':
                    return Dot();
                case '=':
                    return Equal();
                case '!':
                    return Exclamation();
                case '<':
                    return Lt();
                case '>':
                    return Gt();
                case '+':
                    return Plus();
                case '-':
                    return Minus();
                case '*':
                    return Asterisk();
                case '/':
                    return Slash();
                case '%':
                    return Percent();
                case '&':
                    return Ampersand();
                case '|':
                    return Vbar();
                case '^':
                    return Circumflex();
                case '0':
                    return Zero();
                case '@':
                    return Arroba();
                case '\'':
                    return SingleQuoted();
                case '"':
                    return DoubleQuoted();
                case '$':
                    return Dollar();
                case '`':
                    return BackQuote();
                default:
                    if (IsLegalFirstIdChar(ch)) return Identifier();
                    return char.IsDigit(ch) ? Number() : Unknown();
            }
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
             * ([0-9](_[0-9])*)+(\.([0-9](_[0-9])*)+)?([Ee]?[+-]?([0-9](_[0-9])*)+)?[LlFfDd]?
             * Whenever the sequence starts with a dot, it's prepended a literal zero.
             */
            var sb = new StringBuilder();
            bool isFloat = false, zero = true;
            char ch = Ll(1);

            while (char.IsDigit(ch))
            {
                zero = false;
                sb.Append(ch);
                Consume(1);
                ch = Ll(1);
                if (ch == '_' && char.IsDigit(Ll(2)))
                {
                    Consume(1);
                    ch = Ll(1);
                }
            }

            if (ch == '.' && char.IsDigit(Ll(2)))
            {
                isFloat = true;
                if (zero) sb.Append('0');
                sb.Append(ch).Append(Ll(2));
                Consume(2);
                ch = Ll(1);

                while (char.IsDigit(ch))
                {
                    sb.Append(ch);
                    Consume(1);
                    ch = Ll(1);
                    if (ch == '_' && char.IsDigit(Ll(2)))
                    {
                        Consume(1);
                        ch = Ll(1);
                    }
                }
            }

            if (ch == 'e' || ch == 'E')
            {
                bool error = true;
                isFloat = true;
                sb.Append(ch);
                Consume(1);
                ch = Ll(1);

                if (ch == '+' || ch == '-')
                {
                    sb.Append(ch);
                    Consume(1);
                    ch = Ll(1);
                }

                while (char.IsDigit(ch))
                {
                    error = false;
                    sb.Append(ch);
                    Consume(1);
                    ch = Ll(1);
                    if (ch == '_' && char.IsDigit(Ll(2)))
                    {
                        Consume(1);
                        ch = Ll(1);
                    }
                }

                if (error) return Token(TokenID.Unknown, sb.ToString(), false);
            }

            if (isFloat)
            {
                double aDouble;
                switch (Ll(1))
                {
                    case 'f':
                    case 'F':
                        return double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out aDouble)
                             ? Token(TokenID.LT_Float, aDouble, true)
                             : Token(TokenID.LT_Float, double.MaxValue, true);
                    case 'd':
                    case 'D':
                        return Token(TokenID.LT_Decimal, new BigDecimal(sb.ToString()), true);
                    default:
                        return double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out aDouble)
                             ? Token(TokenID.LT_Float, aDouble, false)
                             : Token(TokenID.Unknown, sb.ToString(), false);
                }
            }

            switch (Ll(1))
            {
                case 'l':
                case 'L':
                    return Token(TokenID.LT_Long, BigInteger.Parse(sb.ToString()), true);
                case 'f':
                case 'F':
                    double aDouble;
                    return double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out aDouble)
                         ? Token(TokenID.LT_Float, aDouble, true)
                         : Token(TokenID.LT_Float, double.MaxValue, true);
                case 'd':
                case 'D':
                    return Token(TokenID.LT_Decimal, new BigDecimal(sb.ToString()), true);
                default:
                    int anInt;
                    return int.TryParse(sb.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out anInt)
                         ? Token(TokenID.LT_Integer, anInt, false)
                         : Token(TokenID.LT_Long, BigInteger.Parse(sb.ToString()), false);
            }
        }

        /// <summary>
        /// Gets a token representing a literal string delimited by single quotes.
        /// </summary>
        /// <returns>A <see cref="Token"/> representing a literal string</returns>
        private Token SingleQuoted()
        {
            var sb = new StringBuilder();
            bool loop = true;

            Consume(1);

            while (loop)
            {
                char ch = Ll(1);

                switch (ch)
                {
                    case '\'':
                        loop = false;
                        break;
                    case '\\':
                        sb.Append(Escape());
                        break;
                    default:
                        sb.Append(ch);
                        Consume(1);
                        break;
                }
            }

            return Token(TokenID.LT_String, sb.ToString(), true);
        }

        /// <summary>
        /// Gets a token representing a literal string delimited by double quotes.
        /// </summary>
        /// <returns>A <see cref="Token"/> representing a literal string</returns>
        private Token DoubleQuoted()
        {
            var sb = new StringBuilder();
            bool loop = true;

            Consume(1);

            while (loop)
            {
                char ch = Ll(1);

                switch (ch)
                {
                    case '"':
                        loop = false;
                        break;
                    case '\\':
                        sb.Append(Escape());
                        break;
                    default:
                        sb.Append(ch);
                        Consume(1);
                        break;
                }
            }

            return Token(TokenID.LT_String, sb.ToString(), true);
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
                case '\\':
                case '\'':
                case '"':
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
                case 'x':
                case 'X':
                    return EscapeHex(2);
                case 'u':
                case 'U':
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
            var sb = new StringBuilder();
            bool ok = true;
            
            for (int i = 0; ok && i < max; ++i)
            {
                char ch = Ll(i + 2);

                switch (ch)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                        sb.Append(ch);
                        break;
                    default:
                        ok = false;
                        break;
                }
            }

            string result;

            if (ok)
            {
                int value = int.Parse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                Consume(max + 1);
                result = ((char) value).ToString();
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
            var sb = new StringBuilder();
            sb.Append(Ll(1));
            Consume(1);

            bool loop = true;

            while (loop)
            {
                char ch = Ll(1);

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

            string text = sb.ToString();

            if (Keyword.IsDefined(text))
            {
                Keyword kw = Keyword.Get(text);
                return Token(kw.TokenID, kw.Value, false);
            }
            
            return Token(TokenID.Identifier, text, false);
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

            switch (Ll(1))
            {
                case ':':
                    return Token(TokenID.DoubleColon, null, true);
                default:
                    return Token(TokenID.Colon, null, false);
            }
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with a question mark.
        /// </summary>
        /// <returns>One between <b>??</b> and <b>?</b></returns>
        private Token Question()
        {
            Consume(1);

            switch (Ll(1))
            {
                case '?':
                    return Token(TokenID.DoubleQuestion, null, true);
                default:
                    return Token(TokenID.Question, null, false);
            }
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with a dot.
        /// </summary>
        /// <returns>A literal floating point number or a single dot</returns>
        private Token Dot()
        {
            return char.IsDigit(Ll(2))
                 ? Number()
                 : Token(TokenID.Dot, null, true);
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

                    switch (Ll(1))
                    {
                        case '=':
                            return Token(TokenID.TripleEqual, null, true);
                        default:
                            return Token(TokenID.DoubleEqual, null, false);
                    }
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

                    switch (Ll(1))
                    {
                        case '=':
                            return Token(TokenID.ExclamationDoubleEqual, null, true);
                        default:
                            return Token(TokenID.ExclamationEqual, null, false);
                    }
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

                    switch (Ll(1))
                    {
                        case '=':
                            return Token(TokenID.DoubleLessThanEqual, null, true);
                        default:
                            return Token(TokenID.DoubleLessThan, null, false);
                    }
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

                    switch (Ll(1))
                    {
                        case '=':
                            return Token(TokenID.DoubleGreaterThanEqual, null, true);
                        default:
                            return Token(TokenID.DoubleGreaterThan, null, false);
                    }
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

            switch (Ll(1))
            {
                case '+':
                    return Token(TokenID.DoublePlus, null, true);
                case '=':
                    return Token(TokenID.PlusEqual, null, true);
                default:
                    return Token(TokenID.Plus, null, false);
            }
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with the - sign.
        /// </summary>
        /// <returns>One between <b>--</b>, <b>-=</b> and <b>-</b></returns>
        private Token Minus()
        {
            Consume(1);

            switch (Ll(1))
            {
                case '-':
                    return Token(TokenID.DoubleMinus, null, true);
                case '=':
                    return Token(TokenID.MinusEqual, null, true);
                default:
                    return Token(TokenID.Minus, null, false);
            }
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

                    switch (Ll(1))
                    {
                        case '=':
                            return Token(TokenID.DoubleAsteriskEqual, null, true);
                        default:
                            return Token(TokenID.DoubleAsterisk, null, false);
                    }
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

            switch (Ll(1))
            {
                case '/':
                    return LineComment();
                case '*':
                    return BlockComment();
                case '=':
                    return Token(TokenID.SlashEqual, null, true);
                default:
                    return Token(TokenID.Slash, null, false);
            }
        }

        /// <summary>
        /// Gets a token representing a line-end comment.
        /// </summary>
        /// <returns>A <see cref="Token"/> representing a line-end comment</returns>
        private Token LineComment()
        {
            var sb = new StringBuilder();
            bool loop = true;

            while (loop)
            {
                Consume(1);
                char ch = Ll(1);

                switch (ch)
                {
                    case '\r':
                    case '\n':
                    case EOF:
                        loop = false;
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return Token(TokenID.LineComment, sb.ToString(), false);
        }

        /// <summary>
        /// Gets a token representing a block comment.
        /// </summary>
        /// <returns>A <see cref="Token"/> representing a block comment</returns>
        private Token BlockComment()
        {
            var sb = new StringBuilder();
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
                            sb.Append(ch);
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return Token(TokenID.BlockComment, sb.ToString(), true);
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with the % symbol.
        /// </summary>
        /// <returns>One between <b>%=</b> and <b>%</b></returns>
        private Token Percent()
        {
            Consume(1);

            switch (Ll(1))
            {
                case '=':
                    return Token(TokenID.PercentEqual, null, true);
                default:
                    return Token(TokenID.Percent, null, false);
            }
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with the &amp; symbol.
        /// </summary>
        /// <returns>One between <b>&amp;&amp;</b>, <b>&amp;=</b> and <b>&amp;</b></returns>
        private Token Ampersand()
        {
            Consume(1);

            switch (Ll(1))
            {
                case '&':
                    return Token(TokenID.DoubleAmpersand, null, true);
                case '=':
                    return Token(TokenID.AmpersandEqual, null, true);
                default:
                    return Token(TokenID.Ampersand, null, false);
            }
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with the | symbol.
        /// </summary>
        /// <returns>One between <b>||</b>, <b>|=</b> and <b>|</b></returns>
        private Token Vbar()
        {
            Consume(1);

            switch (Ll(1))
            {
                case '|':
                    return Token(TokenID.DoubleVerticalBar, null, true);
                case '=':
                    return Token(TokenID.VerticalBarEqual, null, true);
                default:
                    return Token(TokenID.VerticalBar, null, false);
            }
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with the ^ symbol.
        /// </summary>
        /// <returns>One between <b>^=</b> and <b>^</b></returns>
        private Token Circumflex()
        {
            Consume(1);

            switch (Ll(1))
            {
                case '=':
                    return Token(TokenID.CircumflexEqual, null, true);
                default:
                    return Token(TokenID.Circumflex, null, false);
            }
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with a zero.
        /// </summary>
        /// <returns>A literal zero, a floating-point number or an hexadecimal number</returns>
        private Token Zero()
        {
            Consume(1);

            switch (Ll(1))
            {
                case 'l':
                case 'L':
                    return Token(TokenID.LT_Long, BigInteger.Zero, true);
                case 'f':
                case 'F':
                    return Token(TokenID.LT_Float, 0D, true);
                case 'd':
                case 'D':
                    return Token(TokenID.LT_Decimal, BigDecimal.Zero, true);
                case '.':
                    return char.IsDigit(Ll(2))
                         ? Number()
                         : Token(TokenID.LT_Integer, 0, false);
                case 'x':
                case 'X':
                    return HexNumber();
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return Number();
                default:
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

            var sb = new StringBuilder();
            bool loop = true;
            
            while (loop)
            {
                char ch = Ll(1);

                switch (ch)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                        sb.Append(ch);
                        Consume(1);
                        break;
                    default:
                        loop = false;
                        break;
                }
            }

            switch (Ll(1))
            {
                case 'l':
                case 'L':
                    return Token(TokenID.LT_Long, BigInteger.Parse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.CurrentUICulture), true);
                default:
                    int anInt;
                    return int.TryParse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.CurrentUICulture, out anInt)
                         ? Token(TokenID.LT_Integer, anInt, false)
                         : Token(TokenID.LT_Long, BigInteger.Parse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.CurrentUICulture), false);
            }
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with the @ symbol.
        /// </summary>
        /// <returns>A verbatim string literal or a token with no particular meaning</returns>
        private Token Arroba()
        {
            Consume(1);

            switch (Ll(1))
            {
                case '\'':
                    return SingleQuotedVerbatim();
                case '"':
                    return DoubleQuotedVerbatim();
                default:
                    return Token(TokenID.Unknown, "@", false);
            }
        }

        /// <summary>
        /// Gets a token representing a verbatim string delimited by single quotes.
        /// </summary>
        /// <returns>A <see cref="Token"/> representing a literal string</returns>
        private Token SingleQuotedVerbatim()
        {
            var sb = new StringBuilder();
            bool loop = true;

            Consume(1);

            while (loop)
            {
                char ch = Ll(1);

                switch (ch)
                {
                    case '\'':
                        if (Ll(2) == ch)
                        {
                            sb.Append(ch);
                            Consume(2);
                        }
                        else
                            loop = false;
                        break;
                    default:
                        sb.Append(ch);
                        Consume(1);
                        break;
                }
            }

            return Token(TokenID.LT_String, sb.ToString(), true);
        }

        /// <summary>
        /// Gets a token representing a verbatim string delimited by double quotes.
        /// </summary>
        /// <returns>A <see cref="Token"/> representing a literal string</returns>
        private Token DoubleQuotedVerbatim()
        {
            var sb = new StringBuilder();
            bool loop = true;

            Consume(1);

            while (loop)
            {
                char ch = Ll(1);

                switch (ch)
                {
                    case '"':
                        if (Ll(2) == ch)
                        {
                            sb.Append(ch);
                            Consume(2);
                        }
                        else
                            loop = false;
                        break;
                    default:
                        sb.Append(ch);
                        Consume(1);
                        break;
                }
            }

            return Token(TokenID.LT_String, sb.ToString(), true);
        }

        /// <summary>
        /// Gets a token representing a symbol that starts with the $ symbol.
        /// </summary>
        /// <returns>A special identifier or a token with no particular meaning</returns>
        private Token Dollar()
        {
            Consume(1);

            var sb = new StringBuilder();
            bool loop = true;

            while (loop)
            {
                char ch = Ll(1);

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
        private Token BackQuote()
        {
            Consume(1);

            DateTime aDateTime;
            var sb = new StringBuilder();
            char ch = Ll(1);

            while (ch != '`')
            {
                sb.Append(ch);
                Consume(1);
                ch = Ll(1);
            }

            return DateTime.TryParse(sb.ToString(), out aDateTime)
                 ? Token(TokenID.LT_Date, aDateTime, true)
                 : Token(TokenID.Unknown, "`" + sb + "`", true);
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
}