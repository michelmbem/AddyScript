namespace AddyScript.Tests;


public static class TestExtensions
{
    public static string Plus(this char self, int offset = 0) => ((char)(self + offset)).ToString();
}
