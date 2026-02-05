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

        var literal = Assert.IsType<Literal>(statements[0]);
        Assert.Equal(classId, literal.Value.Class.ClassID);
    }

    [Theory]
    [InlineData("delta")]
    [InlineData("vec[4]")]
    [InlineData("dict['name']")]
    [InlineData("obj.prop")]
    [InlineData("cls::member")]
    [InlineData("a::b::c::d")]
    public void ReferenceTest(string expression)
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

        switch (expression)
        {
            case "delta":
            {
                var varRef = Assert.IsType<VariableRef>(statement);
                Assert.Equal(expression, varRef.Name);
                break;
            }
            case "vec[4]":
            {
                var itemRef = Assert.IsType<ItemRef>(statement);
                var owner = Assert.IsType<VariableRef>(itemRef.Owner);
                var index = Assert.IsType<Literal>(itemRef.Index);
                Assert.Equal("vec", owner.Name);
                Assert.Equal(4, index.Value.AsInt32);
                break;
            }
            case "dict['name']":
            {
                var itemRef = Assert.IsType<ItemRef>(statement);
                var owner = Assert.IsType<VariableRef>(itemRef.Owner);
                var index = Assert.IsType<Literal>(itemRef.Index);
                Assert.Equal("dict", owner.Name);
                Assert.Equal("name", index.Value.ToString());
                break;
            }
            case "obj.prop":
            {
                var propRef = Assert.IsType<PropertyRef>(statement);
                var owner = Assert.IsType<VariableRef>(propRef.Owner);
                Assert.Equal("obj", owner.Name);
                Assert.Equal("prop", propRef.PropertyName);
                break;
            }
            case "cls::member" or "a::b::c::d":
            {
                var propRef = Assert.IsType<StaticPropertyRef>(statement);
                Assert.Equal(QualifiedName.Parse(expression), propRef.Name);
                break;
            }
            default:
                Assert.Fail($"Unexpected statement type : {statement.GetType().Name}");
                break;
        }
    }

    [Theory]
    [InlineData("(1, 2, 3, 4, 5)")]
    [InlineData("['a', 'b', 'c']")]
    [InlineData("{w, x, ..y, z}")]
    [InlineData("{'one' => 1f, 'two' => 2f}")]
    public void InitializerTest(string expression)
    {
        // Arrange
        string input = $"_ = {expression};";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var assignment = Assert.IsType<Assignment>(statements[0]);
        Assert.Equal(BinaryOperator.None, assignment.Operator);

        switch (assignment.RightOperand)
        {
            case TupleInitializer tuple:
                Assert.Equal(5, tuple.Items.Length);
                for (int i = 0; i < tuple.Items.Length; ++i)
                {
                    var tupleItem = Assert.IsType<Literal>(tuple.Items[i].Value);
                    Assert.Equal(i + 1, tupleItem.Value.AsInt32);
                }
                break;
            case ListInitializer list:
                Assert.Equal(3, list.Items.Length);
                for (int i = 0; i < list.Items.Length; ++i)
                {
                    var listItem = Assert.IsType<Literal>(list.Items[i].Value);
                    Assert.Equal(((char)('a' + i)).ToString(), listItem.Value.ToString());
                }
                break;
            case SetInitializer set:
                Assert.Equal(4, set.Items.Length);
                Assert.True(set.Items[2].Spread);
                for (int i = 0; i < set.Items.Length; ++i)
                {
                    var setItem = Assert.IsType<VariableRef>(set.Items[i].Value);
                    Assert.Equal(((char)('w' + i)).ToString(), setItem.Name);
                }
                break;
            case MapInitializer map:
                Assert.Equal(2, map.Entries.Length);
                foreach (var entry in map.Entries)
                {
                    var entryKey = Assert.IsType<Literal>(entry.Key);
                    Assert.IsType<Runtime.DataItems.String>(entryKey.Value);

                    string keyValue = entryKey.Value.ToString();
                    Assert.True(keyValue is "one" or "two");

                    var entryValue = Assert.IsType<Literal>(entry.Value);
                    var valueValue = Assert.IsType<Runtime.DataItems.Float>(entryValue.Value);

                    switch (keyValue)
                    {
                        case "one":
                            Assert.Equal(1.0, valueValue.AsDouble);
                            break;
                        case "two":
                            Assert.Equal(2.0, valueValue.AsDouble);
                            break;
                        default:
                            Assert.Fail($"Unexpected key in MapInitializer : {keyValue}");
                            break;
                    }
                }
                break;
            default:
                Assert.Fail($"Unexpected statement type : {assignment.GetType().Name}");
                break;
        }
    }

    [Theory]
    [InlineData("foo(bar)")]
    [InlineData("vec[4]()")]
    [InlineData("obj.meth(1, 2)")]
    [InlineData("a::b::c(w, x, y: 'a', z: 'b')")]
    public void CallTest(string expression)
    {
        // Arrange
        string input = $"{expression};";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var statement = statements[0];
        switch (expression)
        {
            case "foo(bar)":
            {
                var fnCall = Assert.IsType<FunctionCall>(statement);
                Assert.Equal("foo", fnCall.FunctionName);
                Assert.Empty(fnCall.NamedArgs);
                Assert.Single(fnCall.Arguments);
                Assert.False(fnCall.Arguments[0].Spread);

                var arg0 = Assert.IsType<VariableRef>(fnCall.Arguments[0].Value);
                Assert.Equal("bar", arg0.Name);
                break;
            }
            case "vec[4]()":
            {
                var anoCall = Assert.IsType<AnonymousCall>(statement);
                var itemRef = Assert.IsType<ItemRef>(anoCall.FunctionSource);
                var owner = Assert.IsType<VariableRef>(itemRef.Owner);
                var index = Assert.IsType<Literal>(itemRef.Index);
                Assert.Equal("vec", owner.Name);
                Assert.Equal(4, index.Value.AsInt32);
                Assert.Empty(anoCall.Arguments);
                Assert.Empty(anoCall.NamedArgs);
                break;
            }
            case "obj.meth(1, 2)":
            {
                var methCall = Assert.IsType<MethodCall>(statement);
                var target = Assert.IsType<VariableRef>(methCall.Target);
                Assert.Equal("obj", target.Name);
                Assert.Equal("meth", methCall.FunctionName);
                Assert.Empty(methCall.NamedArgs);
                Assert.Equal(2, methCall.Arguments.Length);

                for (int i = 0; i < methCall.Arguments.Length; ++i)
                {
                    Assert.False(methCall.Arguments[i].Spread);

                    var arg = Assert.IsType<Literal>(methCall.Arguments[i].Value);
                    Assert.Equal(i + 1, arg.Value.AsInt32);
                }
                break;
            }
            case "a::b::c(w, x, y: 'a', z: 'b')":
            {
                var staticCall = Assert.IsType<StaticMethodCall>(statement);
                Assert.Equal(new QualifiedName("a", "b", "c"), staticCall.Name);
                Assert.Equal(2, staticCall.Arguments.Length);
                Assert.Equal(2, staticCall.NamedArgs.Count);

                for (int i = 0; i < staticCall.Arguments.Length; ++i)
                {
                    Assert.False(staticCall.Arguments[i].Spread);

                    var arg = Assert.IsType<VariableRef>(staticCall.Arguments[i].Value);
                    Assert.Equal(((char)('w' + i)).ToString(), arg.Name);
                }

                Assert.True(staticCall.NamedArgs.ContainsKey("y"));
                var yValue = Assert.IsType<Literal>(staticCall.NamedArgs["y"]);
                Assert.Equal("a", yValue.Value.ToString());

                Assert.True(staticCall.NamedArgs.ContainsKey("z"));
                var zValue = Assert.IsType<Literal>(staticCall.NamedArgs["z"]);
                Assert.Equal("b", zValue.Value.ToString());
                break;
            }
            default:
                Assert.Fail($"Unexpected statement type : {statement.GetType().Name}");
                break;
        }
    }
}
