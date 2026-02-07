namespace AddyScript.Tests;

public class ScriptEngineTest
{
	delegate int SumType(int a, int b);

	[Fact]
	public void GetDelegateTest()
	{
		// Arrange
		var ctx = new ScriptContext();
		var engine = new ScriptEngine(ctx);
		engine.Execute("function sum(a, b) {return a + b;}");

		// Act
		var sum = engine.GetDelegate<SumType>("sum");
		var x = sum(7, -3);

		// Assert
		Assert.Equal(4, x);
	}

	[Fact]
	public void InvokeTest()
	{
		// Arrange
		var ctx = new ScriptContext();
		var engine = new ScriptEngine(ctx);
		engine.Execute("function sum(a, b) {return a + b;}");

		// Act
		var x = engine.Invoke("sum", 10, 5).AsInt32;

		// Assert
		Assert.Equal(15, x);
	}
}
