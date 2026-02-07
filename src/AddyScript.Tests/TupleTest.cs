using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;
using Tuple = AddyScript.Runtime.DataItems.Tuple;


namespace AddyScript.Tests;


public class TupleTest
{

    [Fact]
    public void TupleSerializationTest()
    {
        // Arrange
        var value1 = (10, 0.5);
        var value2 = new Tuple<int, double>(8, -.33);
        
        // Act
        var t1 = DataItemFactory.CreateDataItem(value1);
        var t2 = DataItemFactory.CreateDataItem(value2);

        // Assert
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
        // Arrange
        const int first = -7;
        const float second = 3.1f;
        
        var t = new Tuple([new Integer(first), new Float(second)]);
        
        // Act
        var v1 = t.ConvertTo(typeof((int, float)));
        var v2 = t.ConvertTo(typeof(Tuple<int, float>));

        // Assert
        Assert.Equal((first, second), v1);
        Assert.Equal(new Tuple<int, float>(first, second), v2);
    }
}
