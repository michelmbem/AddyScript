using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;


namespace AddyScript.Tests;


public class TupleTest
{

    [Fact]
    public void TupleSerializationTest()
    {
        var t1 = DataItemFactory.CreateDataItem((10, 0.5));
        var t2 = DataItemFactory.CreateDataItem(new Tuple<int, double>(8, -.33));

        Assert.Equal(Class.Tuple, t1.Class);
        Assert.Equal(new Integer(10), t1.AsArray[0]);
        Assert.Equal(new Float(0.5), t1.AsArray[1]);

        Assert.Equal(Class.Tuple, t2.Class);
        Assert.Equal(new Integer(8), t2.AsArray[0]);
        Assert.Equal(new Float(-.33), t2.AsArray[1]);
    }

    [Fact]
    public void TupleDeserializationTest()
    {
        var t = DataItemFactory.CreateDataItem((-7, 3.1f));
        var v1 = t.ConvertTo(typeof((int, float)));
        var v2 = t.ConvertTo(typeof(Tuple<int, float>));

        Assert.Equal((-7, 3.1f), v1);
        Assert.Equal(new Tuple<int, float>(-7, 3.1f), v2);
    }
}
