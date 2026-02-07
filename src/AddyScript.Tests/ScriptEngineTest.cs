namespace AddyScript.Tests;


public class ScriptEngineTest
{
	delegate int SumType(int a, int b);
	
	private readonly ScriptEngine engine;
	
	public ScriptEngineTest()
	{
		engine = new ScriptEngine();
		engine.Execute("function sum(a, b) {return a + b;}");
	}

	[Theory]
	[InlineData(7, -3, 4)]
	[InlineData(10, 5, 15)]
	[InlineData(0, 0, 0)]
	public void GetDelegateTest(int a, int b, int res)
	{
		// Arrange
		var sum = engine.GetDelegate<SumType>("sum");
		
		// Act
		var x = sum(a, b);

		// Assert
		Assert.Equal(res, x);
	}

	[Theory]
	[InlineData(7, -3, 4)]
	[InlineData(10, 5, 15)]
	[InlineData(0, 0, 0)]
	public void InvokeTest(int a, int b, int res)
	{
		// Arrange
		
		// Act
		var x = engine.Invoke("sum", a, b);

		// Assert
		Assert.Equal(res, x.AsInt32);
	}
}
