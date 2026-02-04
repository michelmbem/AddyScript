using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

using AddyScript.Parsers;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Tests;


public class LexerTest
{
    public static Lexer GetLexer(string input) => new (new StringReader(input));

    private static List<Token> GetTokens(string input)
    {
        Lexer lexer = GetLexer(input);
        List<Token> tokens = [];
        Token token;

        do
        {
            token = lexer.NextToken();
            tokens.Add(token);
        } while (token.TokenID != TokenID.EndOfFile);

        return tokens;
    }

    [Fact]
    public void LiteralNullTest()
    {
        // Arrange
        string input = "null";
        Lexer lexer = GetLexer(input);

        // Act
        Token token = lexer.NextToken();

        // Assert
        Assert.Equal(TokenID.LT_Null, token.TokenID);
        Assert.Null(token.Value);
    }

    [Fact]
    public void LiteralBoolTest()
    {
        // Arrange
        string input = "false true";

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(3, tokens.Count);

        Assert.Equal(TokenID.LT_Boolean, tokens[0].TokenID);
        Assert.IsType<bool>(tokens[0].Value);
        Assert.False((bool)tokens[0].Value);

        Assert.Equal(TokenID.LT_Boolean, tokens[1].TokenID);
        Assert.IsType<bool>(tokens[1].Value);
        Assert.True((bool)tokens[1].Value);
    }

    [Fact]
    public void LiteralIntTest()
    {
        // Arrange
        string input = "123 0 0x2F 0057";

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(5, tokens.Count);

        Assert.Equal(TokenID.LT_Integer, tokens[0].TokenID);
        Assert.IsType<int>(tokens[0].Value);
        Assert.Equal(123, tokens[0].Value);

        Assert.Equal(TokenID.LT_Integer, tokens[1].TokenID);
        Assert.IsType<int>(tokens[1].Value);
        Assert.Equal(0, tokens[1].Value);

        Assert.Equal(TokenID.LT_Integer, tokens[2].TokenID);
        Assert.IsType<int>(tokens[2].Value);
        Assert.Equal(0x2f, tokens[2].Value);

        Assert.Equal(TokenID.LT_Integer, tokens[3].TokenID);
        Assert.IsType<int>(tokens[3].Value);
        Assert.Equal(57, tokens[3].Value);
    }

    [Fact]
    public void LiteralLongTest()
    {
        // Arrange
        string input = "9876543210 147l 22L 0XC9l";

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(5, tokens.Count);

        Assert.Equal(TokenID.LT_Long, tokens[0].TokenID);
        Assert.IsType<BigInteger>(tokens[0].Value);
        Assert.Equal(new BigInteger(9876543210UL), tokens[0].Value);

        Assert.Equal(TokenID.LT_Long, tokens[1].TokenID);
        Assert.IsType<BigInteger>(tokens[1].Value);
        Assert.Equal(new BigInteger(147), tokens[1].Value);

        Assert.Equal(TokenID.LT_Long, tokens[2].TokenID);
        Assert.IsType<BigInteger>(tokens[2].Value);
        Assert.Equal(new BigInteger(22), tokens[2].Value);

        Assert.Equal(TokenID.LT_Long, tokens[3].TokenID);
        Assert.IsType<BigInteger>(tokens[3].Value);
        Assert.Equal(new BigInteger(0xc9), tokens[3].Value);
    }

    [Fact]
    public void LiteralFloatTest()
    {
        // Arrange
        string input = "2f 0.625 .314 1e57 0.6E-89";

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(6, tokens.Count);

        Assert.Equal(TokenID.LT_Float, tokens[0].TokenID);
        Assert.IsType<double>(tokens[0].Value);
        Assert.Equal(2D, tokens[0].Value);

        Assert.Equal(TokenID.LT_Float, tokens[1].TokenID);
        Assert.IsType<double>(tokens[1].Value);
        Assert.Equal(0.625, tokens[1].Value);

        Assert.Equal(TokenID.LT_Float, tokens[2].TokenID);
        Assert.IsType<double>(tokens[2].Value);
        Assert.Equal(.314, tokens[2].Value);

        Assert.Equal(TokenID.LT_Float, tokens[3].TokenID);
        Assert.IsType<double>(tokens[3].Value);
        Assert.Equal(1e57, tokens[3].Value);

        Assert.Equal(TokenID.LT_Float, tokens[4].TokenID);
        Assert.IsType<double>(tokens[4].Value);
        Assert.Equal(0.6E-89, tokens[4].Value);
    }

    [Fact]
    public void LiteralDecimalTest()
    {
        // Arrange
        string input = "1234567890.98765D .5d 18E430d 6.4e+98D";

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(5, tokens.Count);

        Assert.Equal(TokenID.LT_Decimal, tokens[0].TokenID);
        Assert.IsType<BigDecimal>(tokens[0].Value);
        Assert.Equal(BigDecimal.Parse("1234567890.98765"), tokens[0].Value);

        Assert.Equal(TokenID.LT_Decimal, tokens[1].TokenID);
        Assert.IsType<BigDecimal>(tokens[1].Value);
        Assert.Equal(new BigDecimal(0.5), tokens[1].Value);

        Assert.Equal(TokenID.LT_Decimal, tokens[2].TokenID);
        Assert.IsType<BigDecimal>(tokens[2].Value);
        Assert.Equal(BigDecimal.Parse("18E430"), tokens[2].Value);

        Assert.Equal(TokenID.LT_Decimal, tokens[3].TokenID);
        Assert.IsType<BigDecimal>(tokens[3].Value);
        Assert.Equal(BigDecimal.Parse("6.4e+98"), tokens[3].Value);
    }

    [Fact]
    public void LiteralComplexTest()
    {
        // Arrange
        string input = "1i .25i 7e+33i";

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(4, tokens.Count);

        Assert.Equal(TokenID.LT_Complex, tokens[0].TokenID);
        Assert.IsType<Complex>(tokens[0].Value);
        Assert.Equal(Complex.ImaginaryOne, tokens[0].Value);

        Assert.Equal(TokenID.LT_Complex, tokens[1].TokenID);
        Assert.IsType<Complex>(tokens[1].Value);
        Assert.Equal(new Complex(0, 0.25), tokens[1].Value);

        Assert.Equal(TokenID.LT_Complex, tokens[2].TokenID);
        Assert.IsType<Complex>(tokens[2].Value);
        Assert.Equal(new Complex(0, 7e+33), tokens[2].Value);
    }

    [Fact]
    public void LiteralDateTest()
    {
        // Arrange
        string input = "`2017-09-21` `14:30:53.987` `2025-10-01T11:40:15`";
        CultureInfo ci = CultureInfo.InvariantCulture;

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(4, tokens.Count);

        Assert.Equal(TokenID.LT_Date, tokens[0].TokenID);
        Assert.IsType<DateTime>(tokens[0].Value);
        Assert.Equal(DateTime.Parse("2017-09-21", ci), tokens[0].Value);

        Assert.Equal(TokenID.LT_Date, tokens[1].TokenID);
        Assert.IsType<DateTime>(tokens[1].Value);
        Assert.Equal(DateTime.Parse("14:30:53.987", ci), tokens[1].Value);

        Assert.Equal(TokenID.LT_Date, tokens[2].TokenID);
        Assert.IsType<DateTime>(tokens[2].Value);
        Assert.Equal(DateTime.Parse("2025-10-01T11:40:15", ci), tokens[2].Value);
    }

    [Fact]
    public void LiteralStringTest()
    {
        // Arrange
        string input = """
                       'Lorem ipsum dolor sit amet, consectetur adipiscing elit'

                       "Neque porro quisquam est qui dolorem ipsum quia dolor sit amet"

                       @'Fusce vitae ante magna. Sed venenatis dictum lacus, eget blandit velit volutpat et.
                       Phasellus auctor, sapien eget malesuada elementum, urna eros placerat nulla,
                       vel vestibulum est est id nunc. Aliquam interdum elit ut orci tincidunt vehicula'

                       @"Donec tristique luctus lacus non maximus. Cras faucibus maximus erat a feugiat.
                       Maecenas malesuada arcu quis mollis imperdiet. Mauris pellentesque metus eu convallis blandit.
                       Curabitur rhoncus sodales elit, consectetur eleifend risus semper vel"
                       """;

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(5, tokens.Count);

        Assert.Equal(TokenID.LT_String, tokens[0].TokenID);
        Assert.IsType<string>(tokens[0].Value);
        Assert.StartsWith("Lorem ipsum", (string)tokens[0].Value);
        Assert.EndsWith("adipiscing elit", (string)tokens[0].Value);

        Assert.Equal(TokenID.LT_String, tokens[1].TokenID);
        Assert.IsType<string>(tokens[1].Value);
        Assert.StartsWith("Neque porro", (string)tokens[1].Value);
        Assert.EndsWith("sit amet", (string)tokens[1].Value);

        Assert.Equal(TokenID.LT_String, tokens[2].TokenID);
        Assert.IsType<string>(tokens[2].Value);
        Assert.StartsWith("Fusce vitae", (string)tokens[2].Value);
        Assert.EndsWith("tincidunt vehicula", (string)tokens[2].Value);

        Assert.Equal(TokenID.LT_String, tokens[3].TokenID);
        Assert.IsType<string>(tokens[3].Value);
        Assert.StartsWith("Donec tristique", (string)tokens[3].Value);
        Assert.EndsWith("semper vel", (string)tokens[3].Value);
    }

    [Fact]
    public void LiteralBlobTest()
    {
        // Arrange
        string input = """
                       b'Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae'
                       B"Praesent tortor lorem, aliquam nec libero in, ultrices elementum felis"
                       """;

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(3, tokens.Count);

        Assert.Equal(TokenID.LT_Blob, tokens[0].TokenID);
        Assert.IsType<byte[]>(tokens[0].Value);
        Assert.Equal(86, ((byte[])tokens[0].Value).Length);

        Assert.Equal(TokenID.LT_Blob, tokens[1].TokenID);
        Assert.IsType<byte[]>(tokens[1].Value);
        Assert.Equal(70, ((byte[])tokens[1].Value).Length);
    }

    [Fact]
    public void CommentTest()
    {
        // Arrange
        string input = """
                       // Cras at orci fermentum, mollis ante vitae, varius dolor

                       /*
                       Mauris sagittis consequat nibh, vitae aliquet magna volutpat sit amet.
                       Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae;
                       Donec pulvinar id velit eget placerat. Donec vitae libero tincidunt, consequat purus ac, sodales eros.
                       Nunc tortor massa, faucibus et quam nec, aliquam hendrerit urna
                       */
                       """;

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(3, tokens.Count);

        Assert.Equal(TokenID.LineComment, tokens[0].TokenID);
        Assert.IsType<string>(tokens[0].Value);
        Assert.StartsWith(" Cras at orci", (string)tokens[0].Value);
        Assert.EndsWith("varius dolor", (string)tokens[0].Value);

        Assert.Equal(TokenID.BlockComment, tokens[1].TokenID);
        Assert.IsType<string>(tokens[1].Value);
        Assert.StartsWith("\nMauris sagittis", (string)tokens[1].Value);
        Assert.EndsWith("hendrerit urna\n", (string)tokens[1].Value);
    }

    [Fact]
    public void IdentifierTest()
    {
        // Arrange
        string input = @"counter _ __fields one_shot $bool $75 all\x20of b16";

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(9, tokens.Count);

        Assert.Equal(TokenID.Identifier, tokens[0].TokenID);
        Assert.IsType<string>(tokens[0].Value);
        Assert.Equal("counter", tokens[0].Value);

        Assert.Equal(TokenID.Identifier, tokens[1].TokenID);
        Assert.IsType<string>(tokens[1].Value);
        Assert.Equal("_", tokens[1].Value);

        Assert.Equal(TokenID.Identifier, tokens[2].TokenID);
        Assert.IsType<string>(tokens[2].Value);
        Assert.Equal("__fields", tokens[2].Value);

        Assert.Equal(TokenID.Identifier, tokens[3].TokenID);
        Assert.IsType<string>(tokens[3].Value);
        Assert.Equal("one_shot", tokens[3].Value);

        Assert.Equal(TokenID.Identifier, tokens[4].TokenID);
        Assert.IsType<string>(tokens[4].Value);
        Assert.Equal("bool", tokens[4].Value);

        Assert.Equal(TokenID.Identifier, tokens[5].TokenID);
        Assert.IsType<string>(tokens[5].Value);
        Assert.Equal("75", tokens[5].Value);

        Assert.Equal(TokenID.Identifier, tokens[6].TokenID);
        Assert.IsType<string>(tokens[6].Value);
        Assert.Equal("all of", tokens[6].Value);

        Assert.Equal(TokenID.Identifier, tokens[7].TokenID);
        Assert.IsType<string>(tokens[7].Value);
        Assert.Equal("b16", tokens[7].Value);
    }

    [Fact]
    public void ScopeTest()
    {
        // Arrange
        string input = "private protected public";

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(4, tokens.Count);

        Assert.Equal(TokenID.Scope, tokens[0].TokenID);
        Assert.IsType<Scope>(tokens[0].Value);
        Assert.Equal(Scope.Private, tokens[0].Value);

        Assert.Equal(TokenID.Scope, tokens[1].TokenID);
        Assert.IsType<Scope>(tokens[1].Value);
        Assert.Equal(Scope.Protected, tokens[1].Value);

        Assert.Equal(TokenID.Scope, tokens[2].TokenID);
        Assert.IsType<Scope>(tokens[2].Value);
        Assert.Equal(Scope.Public, tokens[2].Value);
    }

    [Fact]
    public void ModifierTest()
    {
        // Arrange
        string input = "final static abstract";

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(4, tokens.Count);

        Assert.Equal(TokenID.Modifier, tokens[0].TokenID);
        Assert.IsType<Modifier>(tokens[0].Value);
        Assert.Equal(Modifier.Final, tokens[0].Value);

        Assert.Equal(TokenID.Modifier, tokens[1].TokenID);
        Assert.IsType<Modifier>(tokens[1].Value);
        Assert.Equal(Modifier.Static, tokens[1].Value);

        Assert.Equal(TokenID.Modifier, tokens[2].TokenID);
        Assert.IsType<Modifier>(tokens[2].Value);
        Assert.Equal(Modifier.Abstract, tokens[2].Value);
    }

    [Fact]
    public void TypeNameTest()
    {
        // Arrange
        string input = @"void bool int long float decimal rational complex date duration
                        string blob tuple list set queue stack map object resource closure";

        string[] typeNames = Regex.Split(input, @"\s+");

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(typeNames.Length + 1, tokens.Count);

        for (int i = 0; i < typeNames.Length; i++)
        {
            Assert.Equal(TokenID.TypeName, tokens[i].TokenID);
            Assert.Equal(typeNames[i], tokens[i].Value);
        }
    }

    [Fact]
    public void PunctuationTest()
    {
        // Arrange
        string input = ", ; : :: . .. ( ) [ ] { }";
        TokenID[] ids = [
            TokenID.Comma, TokenID.SemiColon, TokenID.Colon, TokenID.DoubleColon,
            TokenID.Dot, TokenID.DoubleDot, TokenID.LeftParenthesis, TokenID.RightParenthesis,
            TokenID.LeftBracket, TokenID.RightBracket, TokenID.LeftBrace, TokenID.RightBrace,
        ];

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(ids.Length + 1, tokens.Count);

        for (int i = 0; i < ids.Length; i++)
        {
            Assert.Equal(ids[i], tokens[i].TokenID);
            Assert.Null(tokens[i].Value);
        }
    }

    [Fact]
    public void OperatorTest()
    {
        // Arrange
        string input = @"+ ++ += - -- -= * ** *= **= / /= & && &= | || |= ^ ^= ! != !==
                        ~ ? ?? ??= ?. ?[ = == === => < << <= <<= > >> >= >>=";

        TokenID[] ids = [
            TokenID.Plus, TokenID.DoublePlus, TokenID.PlusEqual, TokenID.Minus, TokenID.DoubleMinus, TokenID.MinusEqual,
            TokenID.Asterisk, TokenID.DoubleAsterisk, TokenID.AsteriskEqual, TokenID.DoubleAsteriskEqual, TokenID.Slash,
            TokenID.SlashEqual, TokenID.Ampersand, TokenID.DoubleAmpersand, TokenID.AmpersandEqual, TokenID.VerticalBar,
            TokenID.DoubleVerticalBar, TokenID.VerticalBarEqual, TokenID.Circumflex, TokenID.CircumflexEqual, TokenID.Exclamation,
            TokenID.ExclamationEqual, TokenID.ExclamationDoubleEqual, TokenID.Tilda, TokenID.Question, TokenID.DoubleQuestion,
            TokenID.DoubleQuestionEqual, TokenID.QuestionDot, TokenID.QuestionBracket, TokenID.Equal, TokenID.DoubleEqual,
            TokenID.TripleEqual, TokenID.Arrow, TokenID.LessThan, TokenID.DoubleLessThan, TokenID.LessThanEqual,
            TokenID.DoubleLessThanEqual, TokenID.GreaterThan, TokenID.DoubleGreaterThan, TokenID.GreaterThanEqual,
            TokenID.DoubleGreaterThanEqual
        ];

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(ids.Length + 1, tokens.Count);

        for (int i = 0; i < ids.Length; i++)
        {
            Assert.Equal(ids[i], tokens[i].TokenID);
            Assert.Null(tokens[i].Value);
        }
    }

    [Fact]
    public void KeywordTest()
    {
        // Arrange
        string input = """
            and as break case catch class const constructor contains continue
            default do else endswith event extern finally for foreach function
            goto if import in is let matches new not operator or property record
            return startswith super switch this throw try typeof var when while
            with yield
            """;

        TokenID[] ids = [
            TokenID.KW_And, TokenID.KW_As, TokenID.KW_Break, TokenID.KW_Case, TokenID.KW_Catch, TokenID.KW_Class,
            TokenID.KW_Const, TokenID.KW_Constructor, TokenID.KW_Contains, TokenID.KW_Continue, TokenID.KW_Default,
            TokenID.KW_Do, TokenID.KW_Else, TokenID.KW_EndsWith, TokenID.KW_Event, TokenID.KW_Extern, TokenID.KW_Finally,
            TokenID.KW_For, TokenID.KW_ForEach, TokenID.KW_Function, TokenID.KW_Goto, TokenID.KW_If, TokenID.KW_Import,
            TokenID.KW_In, TokenID.KW_Is, TokenID.KW_Let, TokenID.KW_Matches, TokenID.KW_New, TokenID.KW_Not,
            TokenID.KW_Operator, TokenID.KW_Or, TokenID.KW_Property, TokenID.KW_Record, TokenID.KW_Return,
            TokenID.KW_StartsWith, TokenID.KW_Super, TokenID.KW_Switch, TokenID.KW_This, TokenID.KW_Throw,
            TokenID.KW_Try, TokenID.KW_TypeOf, TokenID.KW_Var, TokenID.KW_When, TokenID.KW_While, TokenID.KW_With,
            TokenID.KW_Yield,
        ];

        // Act
        List<Token> tokens = GetTokens(input);

        // Assert
        Assert.Equal(ids.Length + 1, tokens.Count);

        for (int i = 0; i < ids.Length; i++)
        {
            Assert.Equal(ids[i], tokens[i].TokenID);
            Assert.Null(tokens[i].Value);
        }
    }
}