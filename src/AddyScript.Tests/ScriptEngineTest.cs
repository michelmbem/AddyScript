namespace AddyScript.Tests;

public class ScriptEngineTest
{
	delegate int SumType(int a, int b);

	[Fact]
	public void GetDelegateTest()
	{
		var ctx = new ScriptContext();
		var engine = new ScriptEngine(ctx);
		engine.Execute("function sum(a, b) {return a + b;}");

		var sum = engine.GetDelegate<SumType>("sum");
		var x = sum(7, -3);

		Assert.Equal(4, x);
	}

	[Fact]
	public void InvokeTest()
	{
		var ctx = new ScriptContext();
		var engine = new ScriptEngine(ctx);
		engine.Execute("function sum(a, b) {return a + b;}");

		var x = engine.Invoke("sum", 10, 5).AsInt32;

		Assert.Equal(15, x);
	}
}
