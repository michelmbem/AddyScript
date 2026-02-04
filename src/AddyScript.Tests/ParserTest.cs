using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Parsers;
using AddyScript.Runtime.OOP;


namespace AddyScript.Tests;


public class ParserTest
{
    public static Parser GetParser(string input) => new (LexerTest.GetLexer(input));

    private static (List<Statement>, ScriptError) Parse(string input)
    {
        Parser parser = GetParser(input);
        List<Statement> statements = [];
        ScriptError error = null;
        Statement statement;

        try
        {
            while (input.Length > 0)
            {
                statement = parser.RequiredStatement();
                statements.Add(statement);
                input = input[statement.End.Offset..];
            }
        }
        catch (ScriptError se)
        {
            error = se;
        }

        return (statements, error);
    }

    [Theory]
    [InlineData("null", ClassID.Void)]
    [InlineData("false", ClassID.Boolean)]
    [InlineData("10", ClassID.Integer)]
    [InlineData("174l", ClassID.Long)]
    [InlineData("6.2354", ClassID.Float)]
    [InlineData("458.125d", ClassID.Decimal)]
    [InlineData("6i", ClassID.Complex)]
    [InlineData("'Hello World'", ClassID.String)]
    [InlineData("b'AddyScript'", ClassID.Blob)]
    public void LiteralValueTest(string expression, ClassID classId)
    {
        // Arrange
        string input = $"{expression};";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var statement = statements[0];
        Assert.IsType<Literal>(statement);
        Assert.Equal(classId, ((Literal)statement).Value.Class.ClassID);
    }

    [Theory]
    [InlineData("delta", typeof(VariableRef))]
    [InlineData("vec[4]", typeof(ItemRef))]
    [InlineData("dict['name']", typeof(ItemRef))]
    [InlineData("obj.prop", typeof(PropertyRef))]
    [InlineData("cls::member", typeof(StaticPropertyRef))]
    [InlineData("a::b::c::d", typeof(StaticPropertyRef))]
    public void ReferenceTest(string expression, Type type)
    {
        // Arrange
        string input = $"{expression};";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var statement = statements[0];
        Assert.IsAssignableFrom<IReference>(statement);
        Assert.Equal(type, statement.GetType());

        switch (expression)
        {
            case "delta":
                Assert.Equal(expression, ((VariableRef)statement).Name);
                break;
            case "vec[4]":
            {
                var itemRef = (ItemRef)statement;
                var owner = Assert.IsType<VariableRef>(itemRef.Owner);
                var index = Assert.IsType<Literal>(itemRef.Index);
                Assert.Equal("vec", owner.Name);
                Assert.Equal(4, index.Value.AsInt32);
                break;
            }
            case "dict['name']":
            {
                var itemRef = (ItemRef)statement;
                var owner = Assert.IsType<VariableRef>(itemRef.Owner);
                var index = Assert.IsType<Literal>(itemRef.Index);
                Assert.Equal("dict", owner.Name);
                Assert.Equal("name", index.Value.ToString());
                break;
            }
            case "obj.prop":
            {
                var propRef = (PropertyRef)statement;
                var owner = Assert.IsType<VariableRef>(propRef.Owner);
                Assert.Equal("obj", owner.Name);
                Assert.Equal("prop", propRef.PropertyName);
                break;
            }
            default:
            {
                var propRef = (StaticPropertyRef)statement;
                Assert.Equal(QualifiedName.Parse(expression), propRef.Name);
                break;
            }
        }
    }
}
