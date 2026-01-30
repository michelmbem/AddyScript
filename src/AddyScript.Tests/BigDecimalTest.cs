using AddyScript.Runtime.NativeTypes;

namespace AddyScript.Tests;

public class BigDecimalTest
{
    [Fact]
    public void DivisionTest()
    {
        var a = BigDecimal.Parse("987654321098765432109876.54321");
        var b = BigDecimal.Parse("1000000000000");
        var expected = BigDecimal.Parse("987654321098.76543210987654321");
        BigDecimal actual = a / b;
        Assert.Equal(expected, actual);
    }
}
