using System.Numerics;

using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Tests;


public class BigDecimalTest
{
    [Theory]
    [InlineData("1000000000000", "1000000000000", 0, 1)]
    [InlineData("0.9876543210987654321", "9876543210987654321", 19, 1)]
    [InlineData("-346.0000000000001", "-3460000000000001", 13, -1)]
    [InlineData("-0.000", "0", 0, 0)]
    public void ParseTest(string first, string second, int third, int fourth)
    {
        // Arrange
        var unscaled = BigInteger.Parse(second);
        
        // Act
        BigDecimal actual = BigDecimal.Parse(first);
        
        // Assert
        Assert.Equal(unscaled, actual.Unscaled);
        Assert.Equal(third, actual.Scale);
        Assert.Equal(fourth, actual.Sign);
    }
    
    [Theory]
    [InlineData("1000000000000", "545.78", "1000000000545.78")]
    [InlineData("3450", "0.9876543210987654321", "3450.9876543210987654321")]
    [InlineData("1", "-0.0000000000001", "0.9999999999999")]
    public void AdditionTest(string first, string second, string third)
    {
        // Arrange
        var a = BigDecimal.Parse(first);
        var b = BigDecimal.Parse(second);
        var expected = BigDecimal.Parse(third);
        
        // Act
        BigDecimal actual = a + b;
        
        // Assert
        Assert.Equal(expected, actual);
    }
    
    [Theory]
    [InlineData("1000000000000", "1", "999999999999")]
    [InlineData("3450", "0.9876543210987654321", "3449.0123456789012345679")]
    public void SubtractionTest(string first, string second, string third)
    {
        // Arrange
        var a = BigDecimal.Parse(first);
        var b = BigDecimal.Parse(second);
        var expected = BigDecimal.Parse(third);
        
        // Act
        BigDecimal actual = a - b;
        
        // Assert
        Assert.Equal(expected, actual);
    }
    
    [Theory]
    [InlineData("1234567890.987654321", "1000000000", "1234567890987654321")]
    [InlineData("5678", "250", "1419500")]
    [InlineData("35986741283256.67073", "0.0001", "3598674128.325667073")]
    public void MultiplicationTest(string first, string second, string third)
    {
        // Arrange
        var a = BigDecimal.Parse(first);
        var b = BigDecimal.Parse(second);
        var expected = BigDecimal.Parse(third);
        
        // Act
        BigDecimal actual = a * b;
        
        // Assert
        Assert.Equal(expected, actual);
    }
    
    [Theory]
    [InlineData("987654321098765432109876.54321", "1000000000000", "987654321098.76543210987654321")]
    [InlineData("456876", "250", "1827.504")]
    [InlineData("35986741283256.67073", "500", "71973482566.51334146")]
    [InlineData("1275441907856452785.6673345560098", "0.0000001", "12754419078564527856673345.560098")]
    public void DivisionTest(string first, string second, string third)
    {
        // Arrange
        var a = BigDecimal.Parse(first);
        var b = BigDecimal.Parse(second);
        var expected = BigDecimal.Parse(third);
        
        // Act
        BigDecimal actual = a / b;
        
        // Assert
        Assert.Equal(expected, actual);
    }
    
    [Theory]
    [InlineData(10, 20, "100000000000000000000")]
    [InlineData(2, 100, "1267650600228229401496703205376")]
    public void PowerTest(decimal first, int second, string third)
    {
        // Arrange
        var a = (BigDecimal)first;
        var expected = BigDecimal.Parse(third);
        
        // Act
        BigDecimal actual = a.Pow(second);
        
        // Assert
        Assert.Equal(expected, actual);
    }
}
