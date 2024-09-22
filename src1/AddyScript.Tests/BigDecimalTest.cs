using AddyScript.Runtime.NativeTypes;

namespace AddyScript.Tests;

public class BigDecimalTest
{
    [Fact]
    public void DivisionTest()
    {
        var a = new BigDecimal("987654321098765432109876.54321");
        var b = new BigDecimal("1000000000000");
        var expected = new BigDecimal("987654321098.76543210987654321");
        BigDecimal actual = a / b;
        Assert.Equal(expected, actual);
    }
}
