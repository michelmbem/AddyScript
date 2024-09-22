using AddyScript.Runtime.Utilities;

namespace AddyScript.Tests;

public class PackFormatTest
{
    [Fact]
    public void ParseTest()
    {
        var format = PackFormat.Parse("<hI12s");
        Assert.Equal(Endianness.LittleEndian, format.Endianness);
        Assert.Equal(3, format.Items.Count);
        Assert.Equal(3, format.Length);
        Assert.Equal(PackFormatType.Short, format.Items[0].Type);
        Assert.Equal(PackFormatType.UInteger, format.Items[1].Type);
        Assert.Equal(PackFormatType.CString, format.Items[2].Type);
        Assert.Equal(12, format.Items[2].Count);
    }
}
