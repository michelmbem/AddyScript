using System.Globalization;
using System.Numerics;

using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Parsers;
using AddyScript.Runtime.OOP;
using DataItems = AddyScript.Runtime.DataItems;


namespace AddyScript.Tests;


public class ParserTest
{
    public static Parser GetParser(string input) => new(LexerTest.GetLexer(input));

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
                Assert.Equal(expression, Assert.IsType<VariableRef>(statement).Name);
                break;
            case "vec[4]":
            {
                var itemRef = Assert.IsType<ItemRef>(statement);
                var index = Assert.IsType<Literal>(itemRef.Index);
                Assert.Equal("vec", Assert.IsType<VariableRef>(itemRef.Owner).Name);
                Assert.Equal(4, Assert.IsType<DataItems.Integer>(index.Value).AsInt32);
                break;
            }
            case "dict['name']":
            {
                var itemRef = Assert.IsType<ItemRef>(statement);
                var index = Assert.IsType<Literal>(itemRef.Index);
                Assert.Equal("dict", Assert.IsType<VariableRef>(itemRef.Owner).Name);
                Assert.Equal("name", Assert.IsType<DataItems.String>(index.Value).ToString());
                break;
            }
            case "obj.prop":
            {
                var propRef = Assert.IsType<PropertyRef>(statement);
                Assert.Equal("obj", Assert.IsType<VariableRef>(propRef.Owner).Name);
                Assert.Equal("prop", propRef.PropertyName);
                break;
            }
            case "cls::member" or "a::b::c::d":
            {
                var propRef = Assert.IsType<StaticPropertyRef>(statement);
                Assert.Equal(QualifiedName.Parse(expression), propRef.Name);
                break;
            }
        }
    }

    [Theory]
    [InlineData("(1, 2, 3, 4, 5)")]
    [InlineData("['a', 'b', 'c']")]
    [InlineData("{w, x, ..y, z}")]
    [InlineData("{'one' => 1f, 'two' => 2f}")]
    [InlineData("new {value, parent, left = null, right = null}")]
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
                    Assert.Equal(i + 1, Assert.IsType<DataItems.Integer>(tupleItem.Value).AsInt32);
                }
                break;
            case ListInitializer list:
                Assert.Equal(3, list.Items.Length);
                for (int i = 0; i < list.Items.Length; ++i)
                {
                    var listItem = Assert.IsType<Literal>(list.Items[i].Value);
                    Assert.Equal('a'.Plus(i), Assert.IsType<DataItems.String>(listItem.Value).ToString());
                }
                break;
            case SetInitializer set:
                Assert.Equal(4, set.Items.Length);
                Assert.True(set.Items[2].Spread);
                for (int i = 0; i < set.Items.Length; ++i)
                {
                    var setItem = Assert.IsType<VariableRef>(set.Items[i].Value);
                    Assert.Equal('w'.Plus(i), setItem.Name);
                }
                break;
            case MapInitializer map:
                Assert.Equal(2, map.Entries.Length);
                foreach (var entry in map.Entries)
                {
                    var entryKey = Assert.IsType<Literal>(entry.Key);
                    Assert.IsType<DataItems.String>(entryKey.Value);

                    string keyValue = entryKey.Value.ToString();
                    Assert.True(keyValue is "one" or "two");

                    var entryValue = Assert.IsType<Literal>(entry.Value);
                    var valueValue = Assert.IsType<DataItems.Float>(entryValue.Value);

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
            case ObjectInitializer _object:
                Assert.Equal(4, _object.PropertySetters.Length);
                Assert.Equal("value", _object.PropertySetters[0].Name);
                Assert.Equal("value", Assert.IsType<VariableRef>(_object.PropertySetters[0].Value).Name);
                Assert.Equal("parent", _object.PropertySetters[1].Name);
                Assert.Equal("parent", Assert.IsType<VariableRef>(_object.PropertySetters[1].Value).Name);
                Assert.Equal("left", _object.PropertySetters[2].Name);
                Assert.IsType<DataItems.Void>(Assert.IsType<Literal>(_object.PropertySetters[2].Value).Value);
                Assert.Equal("right", _object.PropertySetters[3].Name);
                Assert.IsType<DataItems.Void>(Assert.IsType<Literal>(_object.PropertySetters[3].Value).Value);
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

                var arg = Assert.Single(fnCall.Arguments);
                Assert.Equal("bar", Assert.IsType<VariableRef>(arg.Value).Name);
                Assert.False(arg.Spread);
                break;
            }
            case "vec[4]()":
            {
                var anoCall = Assert.IsType<AnonymousCall>(statement);
                Assert.Empty(anoCall.Arguments);
                Assert.Empty(anoCall.NamedArgs);

                var itemRef = Assert.IsType<ItemRef>(anoCall.FunctionSource);
                Assert.Equal("vec", Assert.IsType<VariableRef>(itemRef.Owner).Name);

                var index = Assert.IsType<Literal>(itemRef.Index);
                Assert.Equal(4, Assert.IsType<DataItems.Integer>(index.Value).AsInt32);
                break;
            }
            case "obj.meth(1, 2)":
            {
                var methCall = Assert.IsType<MethodCall>(statement);
                Assert.Equal("obj", Assert.IsType<VariableRef>(methCall.Target).Name);
                Assert.Equal("meth", methCall.FunctionName);
                Assert.Equal(2, methCall.Arguments.Length);
                Assert.Empty(methCall.NamedArgs);

                for (int i = 0; i < methCall.Arguments.Length; ++i)
                {
                    Assert.False(methCall.Arguments[i].Spread);

                    var arg = Assert.IsType<Literal>(methCall.Arguments[i].Value);
                    Assert.Equal(i + 1, Assert.IsType<DataItems.Integer>(arg.Value).AsInt32);
                }
                break;
            }
            case "a::b::c(w, x, y: 'a', z: 'b')":
            {
                var staticCall = Assert.IsType<StaticMethodCall>(statement);
                Assert.Equal(new QualifiedName("a", "b", "c"), staticCall.Name);
                Assert.Equal(2, staticCall.Arguments.Length);
                Assert.Equal(2, staticCall.NamedArgs.Count);
                Assert.True(staticCall.NamedArgs.ContainsKey("y"));
                Assert.True(staticCall.NamedArgs.ContainsKey("z"));

                for (int i = 0; i < staticCall.Arguments.Length; ++i)
                {
                    Assert.False(staticCall.Arguments[i].Spread);

                    var arg = Assert.IsType<VariableRef>(staticCall.Arguments[i].Value);
                    Assert.Equal('w'.Plus(i), arg.Name);
                }

                var yValue = Assert.IsType<Literal>(staticCall.NamedArgs["y"]);
                Assert.Equal("a", Assert.IsType<DataItems.String>(yValue.Value).ToString());

                var zValue = Assert.IsType<Literal>(staticCall.NamedArgs["z"]);
                Assert.Equal("b", Assert.IsType<DataItems.String>(zValue.Value).ToString());
                break;
            }
        }
    }

    [Fact]
    public void FunctionCallWithSpreadArgTest()
    {
        // Arrange
        string input = "foo(bar, ..baz);";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var call = Assert.IsType<FunctionCall>(statements[0]);
        Assert.Equal("foo", call.FunctionName);
        Assert.Equal(2, call.Arguments.Length);

        var arg1 = call.Arguments[0];
        Assert.Equal("bar", Assert.IsType<VariableRef>(arg1.Value).Name);
        Assert.False(arg1.Spread);

        var arg2 = call.Arguments[1];
        Assert.Equal("baz", Assert.IsType<VariableRef>(arg2.Value).Name);
        Assert.True(arg2.Spread);
    }

    [Theory]
    [InlineData("!valid")]
    [InlineData("~0x52fl")]
    [InlineData("--vec.size")]
    [InlineData("++t[i]")]
    public void PrefixUnaryExpressionTest(string expression)
    {
        // Arrange
        string input = $"{expression};";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var prefix = Assert.IsType<UnaryExpression>(statements[0]);

        switch (expression)
        {
            case "!valid":
                Assert.Equal(UnaryOperator.Not, prefix.Operator);
                Assert.Equal("valid", Assert.IsType<VariableRef>(prefix.Operand).Name);
                break;
            case "~0x52fl":
            {
                Assert.Equal(UnaryOperator.BitwiseNot, prefix.Operator);

                var operand = Assert.IsType<Literal>(prefix.Operand);
                var _long = Assert.IsType<DataItems.Long>(operand.Value);
                Assert.Equal(new BigInteger(0x52f), _long.AsBigInteger);
                break;
            }
            case "--vec.size":
            {
                Assert.Equal(UnaryOperator.PreDecrement, prefix.Operator);

                var operand = Assert.IsType<PropertyRef>(prefix.Operand);
                Assert.Equal("vec", Assert.IsType<VariableRef>(operand.Owner).Name);
                Assert.Equal("size", operand.PropertyName);
                break;
            }
            case "++t[i]":
            {
                Assert.Equal(UnaryOperator.PreIncrement, prefix.Operator);

                var operand = Assert.IsType<ItemRef>(prefix.Operand);
                Assert.Equal("t", Assert.IsType<VariableRef>(operand.Owner).Name);
                Assert.Equal("i", Assert.IsType<VariableRef>(operand.Index).Name);
                break;
            }
        }
    }

    [Theory]
    [InlineData("action!")]
    [InlineData("buffer.length--")]
    [InlineData("elem[n]++")]
    public void PostfixUnaryExpressionTest(string expression)
    {
        // Arrange
        string input = $"{expression};";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var postfix = Assert.IsType<UnaryExpression>(statements[0]);

        switch (expression)
        {
            case "action!":
                Assert.Equal(UnaryOperator.NotEmpty, postfix.Operator);
                Assert.Equal("action", Assert.IsType<VariableRef>(postfix.Operand).Name);
                break;
            case "buffer.length--":
            {
                Assert.Equal(UnaryOperator.PostDecrement, postfix.Operator);

                var operand = Assert.IsType<PropertyRef>(postfix.Operand);
                Assert.Equal("buffer", Assert.IsType<VariableRef>(operand.Owner).Name);
                Assert.Equal("length", operand.PropertyName);
                break;
            }
            case "elem[n]++":
            {
                Assert.Equal(UnaryOperator.PostIncrement, postfix.Operator);

                var operand = Assert.IsType<ItemRef>(postfix.Operand);
                Assert.Equal("elem", Assert.IsType<VariableRef>(operand.Owner).Name);
                Assert.Equal("n", Assert.IsType<VariableRef>(operand.Index).Name);
                break;
            }
        }
    }

    [Fact]
    public void ArithmeticExpressionTest()
    {
        // Arrange
        string input = "2*x**3 + 5/y - rand();";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        /*
         * Expectation:
         * 
         * statements[0] = binary
         * binary = term1 - term2
         * term1 = term1_1 + term1_2
         * term1_1 = factor1 * factor2
         * factor1 = literal { type: int, value: 2 }
         * factor2 = expo1 ** expo2
         * expo1 = variable_ref { name: 'x' }
         * expo2 = literal { type: int, value: 3 }
         * term1_2 = factor3 / factor4
         * factor3 = literal { type: int, value: 5 }
         * factor4 = variable_ref { name: 'y' }
         * term2 = function_call { name: 'rand', positional_args: [], named_args: [] }
         */

        var binary = Assert.IsType<BinaryExpression>(statements[0]);
        Assert.Equal(BinaryOperator.Minus, binary.Operator);

        var term1 = Assert.IsType<BinaryExpression>(binary.LeftOperand);
        Assert.Equal(BinaryOperator.Plus, term1.Operator);

        var term1_1 = Assert.IsType<BinaryExpression>(term1.LeftOperand);
        Assert.Equal(BinaryOperator.Times, term1_1.Operator);

        var factor1 = Assert.IsType<Literal>(term1_1.LeftOperand);
        Assert.Equal(2, Assert.IsType<DataItems.Integer>(factor1.Value).AsInt32);

        var factor2 = Assert.IsType<BinaryExpression>(term1_1.RightOperand);
        Assert.Equal(BinaryOperator.Power, factor2.Operator);

        var expo1 = Assert.IsType<VariableRef>(factor2.LeftOperand);
        Assert.Equal("x", expo1.Name);

        var expo2 = Assert.IsType<Literal>(factor2.RightOperand);
        Assert.Equal(3, Assert.IsType<DataItems.Integer>(expo2.Value).AsInt32);

        var term1_2 = Assert.IsType<BinaryExpression>(term1.RightOperand);
        Assert.Equal(BinaryOperator.Divide, term1_2.Operator);

        var factor3 = Assert.IsType<Literal>(term1_2.LeftOperand);
        Assert.Equal(5, Assert.IsType<DataItems.Integer>(factor3.Value).AsInt32);

        var factor4 = Assert.IsType<VariableRef>(term1_2.RightOperand);
        Assert.Equal("y", factor4.Name);

        var term2 = Assert.IsType<FunctionCall>(binary.RightOperand);
        Assert.Equal("rand", term2.FunctionName);
        Assert.Empty(term2.Arguments);
        Assert.Empty(term2.NamedArgs);
    }

    [Fact]
    public void LogicalExpressionTest()
    {
        // Arrange
        string input = "sin(x) > 0.5 && cos(y) <= 0.2 || randint(10) >= 5;";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        /*
         * Expectation:
         *
         * statements[0] = condition
         * condition = relation1 || relation2
         * relation1 = relation1_1 && relation1_2
         * relation1_1 = term1 > term2
         * term1 = function_call { name: 'sin', positional_args: [variable_ref { name: 'x' }], named_args: [] }
         * term2 = literal { type: float, value: 0.5 }
         * relation1_2 = term3 <= term4
         * term3 = function_call { name: 'cos', positional_args: [variable_ref { name: 'y' }], named_args: [] }
         * term4 = literal { type: float, value: 0.2 }
         * relation2 = term5 >= term6
         * term5 = function_call { name: 'randint', positional_args: [literal { type: int, value: 10 }], named_args: [] }
         * term6 = literal { type: int, value: 5 }
         */

        var condition = Assert.IsType<BinaryExpression>(statements[0]);
        Assert.Equal(BinaryOperator.OrElse, condition.Operator);

        var relation1 = Assert.IsType<BinaryExpression>(condition.LeftOperand);
        Assert.Equal(BinaryOperator.AndAlso, relation1.Operator);

        var relation1_1 = Assert.IsType<BinaryExpression>(relation1.LeftOperand);
        Assert.Equal(BinaryOperator.GreaterThan, relation1_1.Operator);

        var term1 = Assert.IsType<FunctionCall>(relation1_1.LeftOperand);
        Assert.Equal("sin", term1.FunctionName);
        Assert.Empty(term1.NamedArgs);

        var arg1 = Assert.Single(term1.Arguments);
        Assert.Equal("x", Assert.IsType<VariableRef>(arg1.Value).Name);
        Assert.False(arg1.Spread);

        var term2 = Assert.IsType<Literal>(relation1_1.RightOperand);
        Assert.Equal(0.5, Assert.IsType<DataItems.Float>(term2.Value).AsDouble);

        var relation1_2 = Assert.IsType<BinaryExpression>(relation1.RightOperand);
        Assert.Equal(BinaryOperator.LessThanOrEqual, relation1_2.Operator);

        var term3 = Assert.IsType<FunctionCall>(relation1_2.LeftOperand);
        Assert.Equal("cos", term3.FunctionName);
        Assert.Empty(term3.NamedArgs);

        var arg2 = Assert.Single(term3.Arguments);
        Assert.Equal("y", Assert.IsType<VariableRef>(arg2.Value).Name);
        Assert.False(arg2.Spread);

        var term4 = Assert.IsType<Literal>(relation1_2.RightOperand);
        Assert.Equal(0.2, Assert.IsType<DataItems.Float>(term4.Value).AsDouble);

        var relation2 = Assert.IsType<BinaryExpression>(condition.RightOperand);
        Assert.Equal(BinaryOperator.GreaterThanOrEqual, relation2.Operator);

        var term5 = Assert.IsType<FunctionCall>(relation2.LeftOperand);
        Assert.Equal("randint", term5.FunctionName);
        Assert.Empty(term5.NamedArgs);

        var arg3 = Assert.Single(term5.Arguments);
        Assert.Equal(10, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(arg3.Value).Value).AsInt32);
        Assert.False(arg3.Spread);

        var term6 = Assert.IsType<Literal>(relation2.RightOperand);
        Assert.Equal(5, Assert.IsType<DataItems.Integer>(term6.Value).AsInt32);
    }

    [Fact]
    public void TernaryExpressionTest()
    {
        // Arrange
        string input = "doc.approved ? 'Approved' : doc.rejected ? 'Rejected' : 'Unknown';";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var ternary = Assert.IsType<TernaryExpression>(statements[0]);
        var test = Assert.IsType<PropertyRef>(ternary.Test);
        Assert.Equal("doc", Assert.IsType<VariableRef>(test.Owner).Name);
        Assert.Equal("approved", test.PropertyName);

        var truePart = Assert.IsType<Literal>(ternary.TruePart);
        Assert.Equal("Approved", Assert.IsType<DataItems.String>(truePart.Value).ToString());

        var falsePart = Assert.IsType<TernaryExpression>(ternary.FalsePart);
        var test2 = Assert.IsType<PropertyRef>(falsePart.Test);
        Assert.Equal("doc", Assert.IsType<VariableRef>(test2.Owner).Name);
        Assert.Equal("rejected", test2.PropertyName);

        var truePart2 = Assert.IsType<Literal>(falsePart.TruePart);
        Assert.Equal("Rejected", Assert.IsType<DataItems.String>(truePart2.Value).ToString());

        var falsePart2 = Assert.IsType<Literal>(falsePart.FalsePart);
        Assert.Equal("Unknown", Assert.IsType<DataItems.String>(falsePart2.Value).ToString());
    }

    [Fact]
    public void AssignmentTest()
    {
        // Arrange
        string input = "a = b += c[-1] *= d ?? 2;";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var assignment1 = Assert.IsType<Assignment>(statements[0]);
        Assert.Equal(BinaryOperator.None, assignment1.Operator);
        Assert.Equal("a", Assert.IsType<VariableRef>(assignment1.LeftOperand).Name);

        var assignment2 = Assert.IsType<Assignment>(assignment1.RightOperand);
        Assert.Equal(BinaryOperator.Plus, assignment2.Operator);
        Assert.Equal("b", Assert.IsType<VariableRef>(assignment2.LeftOperand).Name);

        var assignment3 = Assert.IsType<Assignment>(assignment2.RightOperand);
        Assert.Equal(BinaryOperator.Times, assignment3.Operator);

        var itemRef = Assert.IsType<ItemRef>(assignment3.LeftOperand);
        Assert.Equal("c", Assert.IsType<VariableRef>(itemRef.Owner).Name);

        var index = Assert.IsType<UnaryExpression>(itemRef.Index);
        Assert.Equal(UnaryOperator.Minus, index.Operator);
        Assert.Equal(1, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(index.Operand).Value).AsInt32);

        var assignee = Assert.IsType<BinaryExpression>(assignment3.RightOperand);
        Assert.Equal(BinaryOperator.IfEmpty, assignee.Operator);
        Assert.Equal("d", Assert.IsType<VariableRef>(assignee.LeftOperand).Name);

        var defaultVal = Assert.IsType<Literal>(assignee.RightOperand);
        Assert.Equal(2, Assert.IsType<DataItems.Integer>(defaultVal.Value).AsInt32);
    }

    [Fact]
    public void ConstantDeclarationTest()
    {
        // Arrange
        string input = "const PI = 3.14, MAX = 2;";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var constDecl = Assert.IsType<ConstantDecl>(statements[0]);
        Assert.Equal(2, constDecl.Setters.Length);

        var setter1 = constDecl.Setters[0];
        Assert.Equal("PI", setter1.Name);
        Assert.Equal(3.14, Assert.IsType<DataItems.Float>(Assert.IsType<Literal>(setter1.Value).Value).AsDouble);

        var setter2 = constDecl.Setters[1];
        Assert.Equal("MAX", setter2.Name);
        Assert.Equal(2, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(setter2.Value).Value).AsInt32);
    }

    [Fact]
    public void VariableDeclarationTest()
    {
        // Arrange
        string input = "var name = 'Bob', age = 25, job;";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var varDecl = Assert.IsType<VariableDecl>(statements[0]);
        Assert.Equal(3, varDecl.Setters.Length);

        var setter1 = varDecl.Setters[0];
        Assert.Equal("name", setter1.Name);
        Assert.Equal("Bob", Assert.IsType<DataItems.String>(Assert.IsType<Literal>(setter1.Value).Value).ToString());

        var setter2 = varDecl.Setters[1];
        Assert.Equal("age", setter2.Name);
        Assert.Equal(25, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(setter2.Value).Value).AsInt32);

        var setter3 = varDecl.Setters[2];
        Assert.Equal("job", setter3.Name);
        Assert.Null(setter3.Value);
    }

    [Fact]
    public void FunctionDeclarationTest()
    {
        // Arrange
        string input = "function foo(a, b) => a + b;";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var funcDecl = Assert.IsType<FunctionDecl>(statements[0]);
        Assert.Equal("foo", funcDecl.Name);
        Assert.Equal(2, funcDecl.Parameters.Length);

        var param1 = funcDecl.Parameters[0];
        Assert.Equal("a", param1.Name);
        Assert.False(param1.ByRef);
        Assert.False(param1.VaList);
        Assert.True(param1.CanBeEmpty);
        Assert.Null(param1.DefaultValue);

        var param2 = funcDecl.Parameters[1];
        Assert.Equal("b", param2.Name);
        Assert.False(param2.ByRef);
        Assert.False(param2.VaList);
        Assert.True(param2.CanBeEmpty);
        Assert.Null(param2.DefaultValue);

        var ret = Assert.IsType<Return>(Assert.Single(funcDecl.Body.Statements));
        var expr = Assert.IsType<BinaryExpression>(ret.Expression);
        Assert.Equal(BinaryOperator.Plus, expr.Operator);
        Assert.Equal("a", Assert.IsType<VariableRef>(expr.LeftOperand).Name);
        Assert.Equal("b", Assert.IsType<VariableRef>(expr.RightOperand).Name);
    }

    [Fact]
    public void FunctionDeclarationTest2()
    {
        // Arrange
        string input = """
                       function bar(&res, x!, y = 0L, ..more)
                       {
                           res = x + y + sum(..more);
                           return res >= 0;
                       }
                       """;

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var funcDecl = Assert.IsType<FunctionDecl>(statements[0]);
        Assert.Equal("bar", funcDecl.Name);
        Assert.Equal(4, funcDecl.Parameters.Length);
        Assert.Equal(3, funcDecl.Body.Statements.Length); // There is an appended return statement
        Assert.Null(Assert.IsType<Return>(funcDecl.Body.Statements[2]).Expression);

        var param1 = funcDecl.Parameters[0];
        Assert.Equal("res", param1.Name);
        Assert.True(param1.ByRef);
        Assert.False(param1.VaList);
        Assert.True(param1.CanBeEmpty);
        Assert.Null(param1.DefaultValue);

        var param2 = funcDecl.Parameters[1];
        Assert.Equal("x", param2.Name);
        Assert.False(param2.ByRef);
        Assert.False(param2.VaList);
        Assert.False(param2.CanBeEmpty);
        Assert.Null(param2.DefaultValue);

        var param3 = funcDecl.Parameters[2];
        Assert.Equal("y", param3.Name);
        Assert.False(param3.ByRef);
        Assert.False(param3.VaList);
        Assert.True(param3.CanBeEmpty);
        Assert.Equal(BigInteger.Zero, Assert.IsType<DataItems.Long>(param3.DefaultValue).AsBigInteger);

        var param4 = funcDecl.Parameters[3];
        Assert.Equal("more", param4.Name);
        Assert.False(param4.ByRef);
        Assert.True(param4.VaList);
        Assert.True(param4.CanBeEmpty);
        Assert.Null(param4.DefaultValue);

        var assign = Assert.IsType<Assignment>(funcDecl.Body.Statements[0]);
        Assert.Equal("res", Assert.IsType<VariableRef>(assign.LeftOperand).Name);
        Assert.Equal(BinaryOperator.None, assign.Operator);

        var assignee = Assert.IsType<BinaryExpression>(assign.RightOperand);
        Assert.Equal(BinaryOperator.Plus, assignee.Operator);

        var left = Assert.IsType<BinaryExpression>(assignee.LeftOperand);
        Assert.Equal(BinaryOperator.Plus, left.Operator);
        Assert.Equal("x", Assert.IsType<VariableRef>(left.LeftOperand).Name);
        Assert.Equal("y", Assert.IsType<VariableRef>(left.RightOperand).Name);

        var right = Assert.IsType<FunctionCall>(assignee.RightOperand);
        Assert.Equal("sum", right.FunctionName);

        var arg = Assert.Single(right.Arguments);
        Assert.Equal("more", Assert.IsType<VariableRef>(arg.Value).Name);
        Assert.True(arg.Spread);

        var ret = Assert.IsType<Return>(funcDecl.Body.Statements[1]);
        var returned = Assert.IsType<BinaryExpression>(ret.Expression);
        Assert.Equal(BinaryOperator.GreaterThanOrEqual, returned.Operator);
        Assert.Equal("res", Assert.IsType<VariableRef>(returned.LeftOperand).Name);
        Assert.Equal(0, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(returned.RightOperand).Value).AsInt32);
    }

    [Fact]
    public static void ExternalFunctionDeclarationTest()
    {
        // Arrange
        string input = "extern function routine(handle, struct);";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var funcDecl = Assert.IsType<ExternalFunctionDecl>(statements[0]);
        Assert.Equal("routine", funcDecl.Name);
        Assert.Equal(2, funcDecl.Parameters.Length);

        var param1 = funcDecl.Parameters[0];
        Assert.Equal("handle", param1.Name);
        Assert.False(param1.ByRef);
        Assert.False(param1.VaList);
        Assert.True(param1.CanBeEmpty);
        Assert.Null(param1.DefaultValue);

        var param2 = funcDecl.Parameters[1];
        Assert.Equal("struct", param2.Name);
        Assert.False(param2.ByRef);
        Assert.False(param2.VaList);
        Assert.True(param2.CanBeEmpty);
        Assert.Null(param2.DefaultValue);
    }

    [Fact]
    public void IfElseTest()
    {
        // Arrange
        string input = "if (canExit()) exit(0);";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var ifElse = Assert.IsType<IfElse>(statements[0]);
        Assert.Null(ifElse.AlternativeAction);

        var guard = Assert.IsType<FunctionCall>(ifElse.Guard);
        Assert.Equal("canExit", guard.FunctionName);
        Assert.Empty(guard.Arguments);
        Assert.Empty(guard.NamedArgs);

        var action = Assert.IsType<FunctionCall>(ifElse.Action);
        Assert.Equal("exit", action.FunctionName);
        Assert.Empty(action.NamedArgs);

        var arg = Assert.Single(action.Arguments);
        Assert.Equal(0, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(arg.Value).Value).AsInt32);
        Assert.False(arg.Spread);
    }

    [Fact]
    public void IfElseTest2()
    {
        // Arrange
        string input = "if (x >= 0) println('Positive'); else println('Negative');";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var ifElse = Assert.IsType<IfElse>(statements[0]);

        var guard = Assert.IsType<BinaryExpression>(ifElse.Guard);
        Assert.Equal(BinaryOperator.GreaterThanOrEqual, guard.Operator);
        Assert.Equal("x", Assert.IsType<VariableRef>(guard.LeftOperand).Name);
        Assert.Equal(0, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(guard.RightOperand).Value).AsInt32);

        var action = Assert.IsType<FunctionCall>(ifElse.Action);
        Assert.Equal("println", action.FunctionName);

        var arg1 = Assert.Single(action.Arguments);
        Assert.Equal("Positive", Assert.IsType<DataItems.String>(Assert.IsType<Literal>(arg1.Value).Value).ToString());
        Assert.False(arg1.Spread);

        var altAction = Assert.IsType<FunctionCall>(ifElse.AlternativeAction);
        Assert.Equal("println", altAction.FunctionName);

        var arg2 = Assert.Single(altAction.Arguments);
        Assert.Equal("Negative", Assert.IsType<DataItems.String>(Assert.IsType<Literal>(arg2.Value).Value).ToString());
        Assert.False(arg2.Spread);
    }

    [Fact]
    public void SwitchBlockTest()
    {
        // Arrange
        string input = """
                       switch (score)
                       {
                           case 1:
                               println('A');
                               break;
                           case 2:
                               println('B');
                               break;
                           default:
                               println('F');
                       }
                       """;

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var switchBlk = Assert.IsType<SwitchBlock>(statements[0]);
        Assert.Equal("score", Assert.IsType<VariableRef>(switchBlk.Test).Name);
        Assert.Equal(2, switchBlk.Cases.Length);
        Assert.Equal(0, switchBlk.Cases[0].Address);
        Assert.Equal(1, Assert.IsType<DataItems.Integer>(switchBlk.Cases[0].Value).AsInt32);
        Assert.Equal(2, switchBlk.Cases[1].Address);
        Assert.Equal(2, Assert.IsType<DataItems.Integer>(switchBlk.Cases[1].Value).AsInt32);
        Assert.Equal(4, switchBlk.DefaultCase);
        Assert.Equal(5, switchBlk.Statements.Length);

        for (int i = 0; i < switchBlk.Statements.Length; i++)
        {
            if (i % 2 == 0) // case 1, case 2 and default case
            {
                var printCall = Assert.IsType<FunctionCall>(switchBlk.Statements[i]);
                Assert.Equal("println", printCall.FunctionName);
                Assert.Empty(printCall.NamedArgs);

                var arg = Assert.Single(printCall.Arguments);
                string expected = "ABF"[i / 2].ToString();
                Assert.Equal(expected, Assert.IsType<DataItems.String>(Assert.IsType<Literal>(arg.Value).Value).ToString());
                Assert.False(arg.Spread);
            }
            else // break statements
            {
                Assert.IsType<Break>(switchBlk.Statements[i]);
            }
        }
    }

    [Fact]
    public void ForLoopTest()
    {
        // Arrange
        string input = "for (i = 0; i < 10; i++) println(i);";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var forLoop = Assert.IsType<ForLoop>(statements[0]);

        var init = Assert.IsType<Assignment>(Assert.Single(forLoop.Initializers));
        Assert.Equal("i", Assert.IsType<VariableRef>(init.LeftOperand).Name);
        Assert.Equal(BinaryOperator.None, init.Operator);
        Assert.Equal(0, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(init.RightOperand).Value).AsInt32);

        var guard = Assert.IsType<BinaryExpression>(forLoop.Guard);
        Assert.Equal(BinaryOperator.LessThan, guard.Operator);
        Assert.Equal("i", Assert.IsType<VariableRef>(guard.LeftOperand).Name);
        Assert.Equal(10, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(guard.RightOperand).Value).AsInt32);

        var increment = Assert.IsType<UnaryExpression>(Assert.Single(forLoop.Incrementers));
        Assert.Equal(UnaryOperator.PostIncrement, increment.Operator);
        Assert.Equal("i", Assert.IsType<VariableRef>(increment.Operand).Name);

        var bodyCall = Assert.IsType<FunctionCall>(forLoop.Action);
        Assert.Equal("println", bodyCall.FunctionName);
        Assert.Empty(bodyCall.NamedArgs);

        var arg = Assert.Single(bodyCall.Arguments);
        Assert.Equal("i", Assert.IsType<VariableRef>(arg.Value).Name);
        Assert.False(arg.Spread);
    }

    [Fact]
    public void ForLoopTest2()
    {
        // Arrange
        string input = "for (var k = 0, l = 1; k < n; ++k, l *= 2) vec.add((k, l));";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var forLoop = Assert.IsType<ForLoop>(statements[0]);
        Assert.Equal(2, forLoop.Incrementers.Length);

        var varDecl = Assert.IsType<VariableDecl>(Assert.Single(forLoop.Initializers));
        Assert.Equal(2, varDecl.Setters.Length);

        var setter1 = varDecl.Setters[0];
        Assert.Equal("k", setter1.Name);
        Assert.Equal(0, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(setter1.Value).Value).AsInt32);

        var setter2 = varDecl.Setters[1];
        Assert.Equal("l", setter2.Name);
        Assert.Equal(1, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(setter2.Value).Value).AsInt32);

        var guard = Assert.IsType<BinaryExpression>(forLoop.Guard);
        Assert.Equal(BinaryOperator.LessThan, guard.Operator);
        Assert.Equal("k", Assert.IsType<VariableRef>(guard.LeftOperand).Name);
        Assert.Equal("n", Assert.IsType<VariableRef>(guard.RightOperand).Name);

        var incr1 = Assert.IsType<UnaryExpression>(forLoop.Incrementers[0]);
        Assert.Equal(UnaryOperator.PreIncrement, incr1.Operator);
        Assert.Equal("k", Assert.IsType<VariableRef>(incr1.Operand).Name);

        var incr2 = Assert.IsType<Assignment>(forLoop.Incrementers[1]);
        Assert.Equal("l", Assert.IsType<VariableRef>(incr2.LeftOperand).Name);
        Assert.Equal(BinaryOperator.Times, incr2.Operator);
        Assert.Equal(2, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(incr2.RightOperand).Value).AsInt32);

        var methodCall = Assert.IsType<MethodCall>(forLoop.Action);
        Assert.Equal("vec", Assert.IsType<VariableRef>(methodCall.Target).Name);
        Assert.Equal("add", methodCall.FunctionName);
        Assert.Empty(methodCall.NamedArgs);

        var arg = Assert.Single(methodCall.Arguments);
        Assert.False(arg.Spread);

        var tuple = Assert.IsType<TupleInitializer>(arg.Value);
        Assert.Equal(2, tuple.Items.Length);
        Assert.False(tuple.Items[0].Spread);
        Assert.Equal("k", Assert.IsType<VariableRef>(tuple.Items[0].Value).Name);
        Assert.False(tuple.Items[1].Spread);
        Assert.Equal("l", Assert.IsType<VariableRef>(tuple.Items[1].Value).Name);
    }

    [Fact]
    public void FoEachLoopTest()
    {
        // Arrange
        string input = "foreach (item in collection) process(item);";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var forEach = Assert.IsType<ForEachLoop>(statements[0]);
        Assert.Equal(ForEachLoop.DEFAULT_KEY_NAME, forEach.KeyName);
        Assert.Equal("item", forEach.ValueName);
        Assert.Equal("collection", Assert.IsType<VariableRef>(forEach.Guard).Name);

        var bodyCall = Assert.IsType<FunctionCall>(forEach.Action);
        Assert.Equal("process", bodyCall.FunctionName);
        Assert.Empty(bodyCall.NamedArgs);

        var arg = Assert.Single(bodyCall.Arguments);
        Assert.Equal("item", Assert.IsType<VariableRef>(arg.Value).Name);
        Assert.False(arg.Spread);
    }

    [Fact]
    public void FoEachLoopTest2()
    {
        // Arrange
        string input = "foreach (key => value in dictionary) file.appendLine($'{key} : {value}');";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var forEach = Assert.IsType<ForEachLoop>(statements[0]);
        Assert.Equal("key", forEach.KeyName);
        Assert.Equal("value", forEach.ValueName);
        Assert.Equal("dictionary", Assert.IsType<VariableRef>(forEach.Guard).Name);

        var bodyCall = Assert.IsType<MethodCall>(forEach.Action);
        Assert.Equal("file", Assert.IsType<VariableRef>(bodyCall.Target).Name);
        Assert.Equal("appendLine", bodyCall.FunctionName);
        Assert.Empty(bodyCall.NamedArgs);

        var arg = Assert.Single(bodyCall.Arguments);
        Assert.False(arg.Spread);

        var strInterp = Assert.IsType<StringInterpolation>(arg.Value);
        Assert.Equal("{0} : {1}", strInterp.Pattern);
        Assert.Equal(2, strInterp.Substitions.Length);
        Assert.Equal("key", Assert.IsType<VariableRef>(strInterp.Substitions[0]).Name);
        Assert.Equal("value", Assert.IsType<VariableRef>(strInterp.Substitions[1]).Name);
    }

    [Fact]
    public void WhileLoopTest()
    {
        // Arrange
        string input = "while (hasNext()) process(next());";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var whileLoop = Assert.IsType<WhileLoop>(statements[0]);

        var guard = Assert.IsType<FunctionCall>(whileLoop.Guard);
        Assert.Equal("hasNext", guard.FunctionName);
        Assert.Empty(guard.Arguments);
        Assert.Empty(guard.NamedArgs);

        var bodyCall = Assert.IsType<FunctionCall>(whileLoop.Action);
        Assert.Equal("process", bodyCall.FunctionName);
        Assert.Empty(bodyCall.NamedArgs);

        var arg = Assert.Single(bodyCall.Arguments);
        Assert.False(arg.Spread);

        var nextCall = Assert.IsType<FunctionCall>(arg.Value);
        Assert.Equal("next", nextCall.FunctionName);
        Assert.Empty(nextCall.Arguments);
        Assert.Empty(nextCall.NamedArgs);
    }

    [Fact]
    public void DoLoopTest()
    {
        // Arrange
        string input = "do { step(); } while (canContinue());";

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var doLoop = Assert.IsType<DoLoop>(statements[0]);

        var guard = Assert.IsType<FunctionCall>(doLoop.Guard);
        Assert.Equal("canContinue", guard.FunctionName);
        Assert.Empty(guard.Arguments);
        Assert.Empty(guard.NamedArgs);

        var body = Assert.IsType<Block>(doLoop.Action);
        var bodyCall = Assert.IsType<FunctionCall>(Assert.Single(body.Statements));
        Assert.Equal("step", bodyCall.FunctionName);
        Assert.Empty(bodyCall.Arguments);
        Assert.Empty(bodyCall.NamedArgs);
    }

    [Fact]
    public void PatternMatchingTest()
    {
        // Arrange
        string input = """
            randint(-10, 20) switch
            {
                < 0 => println('Negative'),
                0 => println('Zero'),
                1 or 2 or 3 => println('Small'),
                >= 4 and <= 5 => println('Medium'),
                not 10 => println('Large'),
                10 => println('Perfect'),
                _ => println('Other')
            };
            """;

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var patMatch = Assert.IsType<PatternMatching>(statements[0]);
        Assert.Equal(7, patMatch.MatchCases.Length);
        Assert.IsType<AlwaysTruePattern>(patMatch.MatchCases[6].Pattern);

        var testCall = Assert.IsType<FunctionCall>(patMatch.Expression);
        Assert.Equal("randint", testCall.FunctionName);
        Assert.Equal(2, testCall.Arguments.Length);
        Assert.Empty(testCall.NamedArgs);

        var arg1 = Assert.IsType<UnaryExpression>(testCall.Arguments[0].Value);
        Assert.Equal(UnaryOperator.Minus, arg1.Operator);
        Assert.Equal(10, Assert.IsType<DataItems.Integer>(
            Assert.IsType<Literal>(arg1.Operand).Value).AsInt32);

        var arg2 = Assert.IsType<Literal>(testCall.Arguments[1].Value);
        Assert.Equal(20, Assert.IsType<DataItems.Integer>(arg2.Value).AsInt32);

        var case1 = Assert.IsType<RelationalPattern>(patMatch.MatchCases[0].Pattern);
        Assert.Equal(BinaryOperator.LessThan, case1.Operator);
        Assert.Equal(0, Assert.IsType<DataItems.Integer>(case1.Value).AsInt32);

        var caseCall1 = Assert.IsType<FunctionCall>(patMatch.MatchCases[0].Expression);
        Assert.Equal("println", caseCall1.FunctionName);
        Assert.Empty(caseCall1.NamedArgs);

        var caseArg1 = Assert.Single(caseCall1.Arguments);
        Assert.Equal("Negative", Assert.IsType<DataItems.String>(
            Assert.IsType<Literal>(caseArg1.Value).Value).ToString());

        var case2 = Assert.IsType<RelationalPattern>(patMatch.MatchCases[1].Pattern);
        Assert.Equal(BinaryOperator.Equal, case2.Operator);
        Assert.Equal(0, Assert.IsType<DataItems.Integer>(case2.Value).AsInt32);

        var caseCall2 = Assert.IsType<FunctionCall>(patMatch.MatchCases[1].Expression);
        Assert.Equal("println", caseCall2.FunctionName);
        Assert.Empty(caseCall2.NamedArgs);

        var caseArg2 = Assert.Single(caseCall2.Arguments);
        Assert.Equal("Zero", Assert.IsType<DataItems.String>(
            Assert.IsType<Literal>(caseArg2.Value).Value).ToString());

        var case3 = Assert.IsType<LogicalPattern>(patMatch.MatchCases[2].Pattern);
        Assert.False(case3.Inclusive);

        var case3_left = Assert.IsType<LogicalPattern>(case3.Left);
        Assert.False(case3_left.Inclusive);

        var case3_left_left = Assert.IsType<RelationalPattern>(case3_left.Left);
        Assert.Equal(BinaryOperator.Equal, case3_left_left.Operator);
        Assert.Equal(1, Assert.IsType<DataItems.Integer>(case3_left_left.Value).AsInt32);

        var case3_left_right = Assert.IsType<RelationalPattern>(case3_left.Right);
        Assert.Equal(BinaryOperator.Equal, case3_left_right.Operator);
        Assert.Equal(2, Assert.IsType<DataItems.Integer>(case3_left_right.Value).AsInt32);

        var case3_right = Assert.IsType<RelationalPattern>(case3.Right);
        Assert.Equal(BinaryOperator.Equal, case3_right.Operator);
        Assert.Equal(3, Assert.IsType<DataItems.Integer>(case3_right.Value).AsInt32);

        var caseCall3 = Assert.IsType<FunctionCall>(patMatch.MatchCases[2].Expression);
        Assert.Equal("println", caseCall3.FunctionName);
        Assert.Empty(caseCall3.NamedArgs);

        var caseArg3 = Assert.Single(caseCall3.Arguments);
        Assert.Equal("Small", Assert.IsType<DataItems.String>(
            Assert.IsType<Literal>(caseArg3.Value).Value).ToString());

        var case4 = Assert.IsType<LogicalPattern>(patMatch.MatchCases[3].Pattern);
        Assert.True(case4.Inclusive);

        var case4_left = Assert.IsType<RelationalPattern>(case4.Left);
        Assert.Equal(BinaryOperator.GreaterThanOrEqual, case4_left.Operator);
        Assert.Equal(4, Assert.IsType<DataItems.Integer>(case4_left.Value).AsInt32);

        var case4_right = Assert.IsType<RelationalPattern>(case4.Right);
        Assert.Equal(BinaryOperator.LessThanOrEqual, case4_right.Operator);
        Assert.Equal(5, Assert.IsType<DataItems.Integer>(case4_right.Value).AsInt32);

        var caseCall4 = Assert.IsType<FunctionCall>(patMatch.MatchCases[3].Expression);
        Assert.Equal("println", caseCall4.FunctionName);
        Assert.Empty(caseCall4.NamedArgs);

        var caseArg4 = Assert.Single(caseCall4.Arguments);
        Assert.Equal("Medium", Assert.IsType<DataItems.String>(
            Assert.IsType<Literal>(caseArg4.Value).Value).ToString());

        var case5 = Assert.IsType<NegativePattern>(patMatch.MatchCases[4].Pattern);
        var case5_child = Assert.IsType<RelationalPattern>(case5.Child);
        Assert.Equal(BinaryOperator.Equal, case5_child.Operator);
        Assert.Equal(10, Assert.IsType<DataItems.Integer>(case5_child.Value).AsInt32);

        var caseCall5 = Assert.IsType<FunctionCall>(patMatch.MatchCases[4].Expression);
        Assert.Equal("println", caseCall5.FunctionName);
        Assert.Empty(caseCall5.NamedArgs);

        var caseArg5 = Assert.Single(caseCall5.Arguments);
        Assert.Equal("Large", Assert.IsType<DataItems.String>(
            Assert.IsType<Literal>(caseArg5.Value).Value).ToString());

        var case6 = Assert.IsType<RelationalPattern>(patMatch.MatchCases[5].Pattern);
        Assert.Equal(BinaryOperator.Equal, case6.Operator);
        Assert.Equal(10, Assert.IsType<DataItems.Integer>(case6.Value).AsInt32);

        var caseCall6 = Assert.IsType<FunctionCall>(patMatch.MatchCases[5].Expression);
        Assert.Equal("println", caseCall6.FunctionName);
        Assert.Empty(caseCall6.NamedArgs);

        var caseArg6 = Assert.Single(caseCall6.Arguments);
        Assert.Equal("Perfect", Assert.IsType<DataItems.String>(
            Assert.IsType<Literal>(caseArg6.Value).Value).ToString());

        var caseCall7 = Assert.IsType<FunctionCall>(patMatch.MatchCases[6].Expression);
        Assert.Equal("println", caseCall7.FunctionName);
        Assert.Empty(caseCall7.NamedArgs);

        var caseArg7 = Assert.Single(caseCall7.Arguments);
        Assert.Equal("Other", Assert.IsType<DataItems.String>(
            Assert.IsType<Literal>(caseArg7.Value).Value).ToString());

        foreach (var matchCase in patMatch.MatchCases)
        {
            Assert.Null(matchCase.Guard);
        }
    }

    [Fact]
    public void PatternMatchingTest2()
    {
        // Arrange
        string input = """
            data switch
            {
                null => 'No value',
                CustomerData => $'Customer Data, name: {data.name}, phone: {data.phone}',
                OrderData { orderDate: >= `2025-07-01` } => 'Recent order',
                EmployeeData(jobTitle, department) => $'Employee: {jobTitle} at {department}',
                { shipping.$date.year: 2026  } => 'Shipping in 2026',
                (date, decimal) => $'Invoice date: {__value[0]:d}, amount: {__value[1]:c}',
                _ when data is not string => throw 'Unsupported data',
                _ => {
                    println('Require more processing');
                    yield data;
                }
            };
            """;

        // Act
        var (statements, error) = Parse(input);

        // Assert
        Assert.Single(statements);
        Assert.Null(error);

        var patMatch = Assert.IsType<PatternMatching>(statements[0]);
        Assert.Equal("data", Assert.IsType<VariableRef>(patMatch.Expression).Name);
        Assert.Equal(8, patMatch.MatchCases.Length);
        Assert.IsType<AlwaysTruePattern>(patMatch.MatchCases[6].Pattern);
        Assert.IsType<AlwaysTruePattern>(patMatch.MatchCases[7].Pattern);

        var case1 = Assert.IsType<RelationalPattern>(patMatch.MatchCases[0].Pattern);
        Assert.Equal(BinaryOperator.Identical, case1.Operator);
        Assert.Equal(DataItems.Void.Value, case1.Value);

        var caseExpr1 = Assert.IsType<Literal>(patMatch.MatchCases[0].Expression);
        Assert.Equal("No value", Assert.IsType<DataItems.String>(caseExpr1.Value).ToString());

        var case2 = Assert.IsType<TypePattern>(patMatch.MatchCases[1].Pattern);
        Assert.Equal("CustomerData", case2.TypeName);

        var caseExpr2 = Assert.IsType<StringInterpolation>(patMatch.MatchCases[1].Expression);
        Assert.Equal("Customer Data, name: {0}, phone: {1}", caseExpr2.Pattern);
        Assert.Equal(2, caseExpr2.Substitions.Length);

        var sub1 = Assert.IsType<PropertyRef>(caseExpr2.Substitions[0]);
        Assert.Equal("data", Assert.IsType<VariableRef>(sub1.Owner).Name);
        Assert.Equal("name", sub1.PropertyName);

        var sub2 = Assert.IsType<PropertyRef>(caseExpr2.Substitions[1]);
        Assert.Equal("data", Assert.IsType<VariableRef>(sub2.Owner).Name);
        Assert.Equal("phone", sub2.PropertyName);

        var case3 = Assert.IsType<ObjectPattern>(patMatch.MatchCases[2].Pattern);
        Assert.Equal("OrderData", case3.TypeName);

        var propMatcher = Assert.Single(case3.PropertyMatchers);
        Assert.Equal("orderDate", Assert.Single(propMatcher.Path));

        var propPattern1 = Assert.IsType<RelationalPattern>(propMatcher.Pattern);
        Assert.Equal(BinaryOperator.GreaterThanOrEqual, propPattern1.Operator);
        Assert.Equal(DateTime.Parse("2025-07-01", CultureInfo.InvariantCulture),
            Assert.IsType<DataItems.Date>(propPattern1.Value).AsDateTime);

        var caseExpr3 = Assert.IsType<Literal>(patMatch.MatchCases[2].Expression);
        Assert.Equal("Recent order", Assert.IsType<DataItems.String>(caseExpr3.Value).ToString());

        var case4 = Assert.IsType<DestructuringPattern>(patMatch.MatchCases[3].Pattern);
        Assert.Equal("EmployeeData", case4.TypeName);
        Assert.Equal(2, case4.PropertyNames.Length);
        Assert.Equal("jobTitle", case4.PropertyNames[0]);
        Assert.Equal("department", case4.PropertyNames[1]);

        var caseExpr4 = Assert.IsType<StringInterpolation>(patMatch.MatchCases[3].Expression);
        Assert.Equal("Employee: {0} at {1}", caseExpr4.Pattern);
        Assert.Equal(2, caseExpr4.Substitions.Length);
        Assert.Equal("jobTitle", Assert.IsType<VariableRef>(caseExpr4.Substitions[0]).Name);
        Assert.Equal("department", Assert.IsType<VariableRef>(caseExpr4.Substitions[1]).Name);

        var case5 = Assert.IsType<ObjectPattern>(patMatch.MatchCases[4].Pattern);
        Assert.Null(case5.TypeName);

        var shippingMatcher = Assert.Single(case5.PropertyMatchers);
        Assert.Equal(3, shippingMatcher.Path.Length);
        Assert.Equal("shipping", shippingMatcher.Path[0]);
        Assert.Equal("date", shippingMatcher.Path[1]);
        Assert.Equal("year", shippingMatcher.Path[2]);

        var propPattern2 = Assert.IsType<RelationalPattern>(shippingMatcher.Pattern);
        Assert.Equal(BinaryOperator.Equal, propPattern2.Operator);
        Assert.Equal(2026, Assert.IsType<DataItems.Integer>(propPattern2.Value).AsInt32);

        var caseExpr5 = Assert.IsType<Literal>(patMatch.MatchCases[4].Expression);
        Assert.Equal("Shipping in 2026", Assert.IsType<DataItems.String>(caseExpr5.Value).ToString());

        var case6 = Assert.IsType<PositionalPattern>(patMatch.MatchCases[5].Pattern);
        Assert.Equal(2, case6.Items.Length);
        Assert.Equal("date", Assert.IsType<TypePattern>(case6.Items[0]).TypeName);
        Assert.Equal("decimal", Assert.IsType<TypePattern>(case6.Items[1]).TypeName);

        var caseExpr6 = Assert.IsType<StringInterpolation>(patMatch.MatchCases[5].Expression);
        Assert.Equal("Invoice date: {0:d}, amount: {1:c}", caseExpr6.Pattern);
        Assert.Equal(2, caseExpr6.Substitions.Length);

        var sub6_1 = Assert.IsType<ItemRef>(caseExpr6.Substitions[0]);
        Assert.Equal("__value", Assert.IsType<VariableRef>(sub6_1.Owner).Name);
        Assert.Equal(0, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(sub6_1.Index).Value).AsInt32);

        var sub6_2 = Assert.IsType<ItemRef>(caseExpr6.Substitions[1]);
        Assert.Equal("__value", Assert.IsType<VariableRef>(sub6_2.Owner).Name);
        Assert.Equal(1, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(sub6_2.Index).Value).AsInt32);

        var guard7 = Assert.IsType<PatternMatching>(patMatch.MatchCases[6].Guard);
        Assert.Equal("data", Assert.IsType<VariableRef>(guard7.Expression).Name);
        Assert.True(guard7.IsSimple);

        var typeCheck7 = Assert.IsType<TypePattern>(Assert.IsType<NegativePattern>(guard7.MatchCases[0].Pattern).Child);
        Assert.Equal("string", typeCheck7.TypeName);

        var caseExpr7 = Assert.IsType<ThrowExpression>(patMatch.MatchCases[6].Expression);
        Assert.Equal("Unsupported data", Assert.IsType<DataItems.String>(
            Assert.IsType<Literal>(caseExpr7.Throw.Expression).Value).ToString());

        var caseExpr8 = Assert.IsType<BlockAsExpression>(patMatch.MatchCases[7].Expression);
        Assert.Equal(2, caseExpr8.Block.Statements.Length);

        var printCall = Assert.IsType<FunctionCall>(caseExpr8.Block.Statements[0]);
        Assert.Equal("println", printCall.FunctionName);
        Assert.Empty(printCall.NamedArgs);
        
        var printArg = Assert.Single(printCall.Arguments);
        Assert.Equal("Require more processing", Assert.IsType<DataItems.String>(
            Assert.IsType<Literal>(printArg.Value).Value).ToString());

        var yieldStmt = Assert.IsType<Yield>(caseExpr8.Block.Statements[1]);
        Assert.Equal("data", Assert.IsType<VariableRef>(yieldStmt.Expression).Name);

        for (var i = 0; i < patMatch.MatchCases.Length; i++)
        {
            if (i == 6) continue; // the 7th case has a guard
            Assert.Null(patMatch.MatchCases[i].Guard);
        }
    }

    [Fact]
    public void ClassDefinitionTest()
    {
        // Arrange
        string input = """
                       class Single
                       {
                           private _element;
                           
                           public constructor (element)
                           {
                               this.element = element;
                           }
                           
                           public property element
                           {
                              read { return this._element; }
                              write { this._element = __value; }
                           }
                           
                           public function toString()
                           {
                               return "Single(" + this.element + ")";
                           }
                       }
                       """;
        
        // Act
        var (statements, error) = Parse(input);
        
        // Assert
        Assert.Single(statements);
        Assert.Null(error);
        
        var classDef = Assert.IsType<ClassDefinition>(statements[0]);
        Assert.Equal("Single", classDef.ClassName);
        Assert.Equal(Modifier.Default, classDef.Modifier);
        Assert.Null(classDef.SuperClassName);
        Assert.Null(classDef.Indexer);
        Assert.Empty(classDef.Events);
        Assert.Null(classDef.Attributes);

        var constructor = Assert.IsType<ClassMethodDecl>(classDef.Constructor);
        Assert.Equal(classDef.ClassName, constructor.Name);
        Assert.Equal(Scope.Public, constructor.Scope);
        Assert.Equal(Modifier.Default, constructor.Modifier);
        Assert.Null(constructor.Attributes);
        
        var ctorParam = Assert.Single(constructor.Parameters);
        Assert.Equal("element", ctorParam.Name);
        Assert.False(ctorParam.ByRef);
        Assert.False(ctorParam.VaList);
        Assert.True(ctorParam.CanBeEmpty);
        Assert.Null(ctorParam.DefaultValue);
        Assert.Null(ctorParam.Attributes);
        Assert.Equal(2, constructor.Body.Statements.Length);
        Assert.IsType<Return>(constructor.Body.Statements[1]);
        
        var ctorBodyAssign = Assert.IsType<Assignment>(constructor.Body.Statements[0]);
        Assert.Equal(BinaryOperator.None, ctorBodyAssign.Operator);
        Assert.Equal("element", Assert.IsType<VariableRef>(ctorBodyAssign.RightOperand).Name);
        
        var lValue = Assert.IsType<PropertyRef>(ctorBodyAssign.LeftOperand);
        Assert.Equal("element", lValue.PropertyName);
        Assert.IsType<SelfReference>(lValue.Owner);
        
        var elementField = Assert.Single(classDef.Fields);
        Assert.Equal("_element", elementField.Name);
        Assert.Equal(Scope.Private, elementField.Scope);
        Assert.Equal(Modifier.Default, elementField.Modifier);
        Assert.Null(elementField.Initializer);
        Assert.Null(elementField.Attributes);

        var elementProp = Assert.Single(classDef.Properties);
        Assert.Equal("element", elementProp.Name);
        Assert.Equal(Scope.Public, elementProp.Scope);
        Assert.Equal(Modifier.Default, elementProp.Modifier);
        Assert.True(elementProp.CanRead);
        Assert.True(elementProp.CanWrite);
        Assert.Equal(elementProp.Scope, elementProp.ReaderScope);
        Assert.Equal(elementProp.Scope, elementProp.WriterScope);
        Assert.Null(elementProp.Attributes);
        
        var readerStmt = Assert.IsType<Return>(elementProp.ReaderBody.Statements[0]);
        var returned = Assert.IsType<PropertyRef>(readerStmt.Expression);
        Assert.Equal(elementField.Name, returned.PropertyName);
        Assert.IsType<SelfReference>(returned.Owner);
        
        var writerStmt = Assert.IsType<Assignment>(elementProp.WriterBody.Statements[0]);
        Assert.Equal(BinaryOperator.None, writerStmt.Operator);
        Assert.Equal("__value", Assert.IsType<VariableRef>(writerStmt.RightOperand).Name);
        
        var lValue2 = Assert.IsType<PropertyRef>(writerStmt.LeftOperand);
        Assert.Equal(elementField.Name, lValue2.PropertyName);
        Assert.IsType<SelfReference>(lValue2.Owner);
        
        var toStringMethod = Assert.Single(classDef.Methods);
        Assert.Equal("toString", toStringMethod.Name);
        Assert.Equal(Scope.Public, toStringMethod.Scope);
        Assert.Equal(Modifier.Default, toStringMethod.Modifier);
        Assert.Empty(toStringMethod.Parameters);
        Assert.Equal(2, toStringMethod.Body.Statements.Length);
        Assert.IsType<Return>(toStringMethod.Body.Statements[1]);
        Assert.Null(toStringMethod.Attributes);
        
        var toStringBodyReturn = Assert.IsType<Return>(toStringMethod.Body.Statements[0]);
        var returned2 = Assert.IsType<BinaryExpression>(toStringBodyReturn.Expression);
        Assert.Equal(BinaryOperator.Plus, returned2.Operator);
        Assert.Equal(")", Assert.IsType<DataItems.String>(Assert.IsType<Literal>(returned2.RightOperand).Value).ToString());
        
        var leftTerm = Assert.IsType<BinaryExpression>(returned2.LeftOperand);
        Assert.Equal(BinaryOperator.Plus, leftTerm.Operator);
        Assert.Equal("Single(", Assert.IsType<DataItems.String>(Assert.IsType<Literal>(leftTerm.LeftOperand).Value).ToString());
        
        var middleTerm = Assert.IsType<PropertyRef>(leftTerm.RightOperand);
        Assert.Equal(elementProp.Name, middleTerm.PropertyName);
        Assert.IsType<SelfReference>(middleTerm.Owner);
    }
}