using System.Numerics;

using AddyScript.Parsers;
using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Tests;


public class LexerTest
{
    private static Lexer GetLexer(string input) => new(new StringReader(input));

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
}