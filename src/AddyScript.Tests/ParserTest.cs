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

        var switchStmt = Assert.IsType<SwitchBlock>(statements[0]);
        Assert.Equal("score", Assert.IsType<VariableRef>(switchStmt.Test).Name);
        Assert.Equal(2, switchStmt.Cases.Length);
        Assert.Equal(0, switchStmt.Cases[0].Address);
        Assert.Equal(1, Assert.IsType<DataItems.Integer>(switchStmt.Cases[0].Value).AsInt32);
        Assert.Equal(2, switchStmt.Cases[1].Address);
        Assert.Equal(2, Assert.IsType<DataItems.Integer>(switchStmt.Cases[1].Value).AsInt32);
        Assert.Equal(4, switchStmt.DefaultCase);
        Assert.Equal(5, switchStmt.Statements.Length);

        for (int i = 0; i < switchStmt.Statements.Length; i++)
        {
            if (i % 2 == 0) // case 1, case 2 and default case
            {
                var printCall = Assert.IsType<FunctionCall>(switchStmt.Statements[i]);
                Assert.Equal("println", printCall.FunctionName);
                Assert.Empty(printCall.NamedArgs);

                var arg = Assert.Single(printCall.Arguments);
                string expected = "ABF"[i / 2].ToString();
                Assert.Equal(expected, Assert.IsType<DataItems.String>(Assert.IsType<Literal>(arg.Value).Value).ToString());
                Assert.False(arg.Spread);
            }
            else // break statements
            {
                Assert.IsType<Break>(switchStmt.Statements[i]);
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

        var incrementer1 = Assert.IsType<UnaryExpression>(forLoop.Incrementers[0]);
        Assert.Equal(UnaryOperator.PreIncrement, incrementer1.Operator);
        Assert.Equal("k", Assert.IsType<VariableRef>(incrementer1.Operand).Name);

        var incrementer2 = Assert.IsType<Assignment>(forLoop.Incrementers[1]);
        Assert.Equal("l", Assert.IsType<VariableRef>(incrementer2.LeftOperand).Name);
        Assert.Equal(BinaryOperator.Times, incrementer2.Operator);
        Assert.Equal(2, Assert.IsType<DataItems.Integer>(Assert.IsType<Literal>(incrementer2.RightOperand).Value).AsInt32);

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
}
