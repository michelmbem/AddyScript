using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Properties;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.NativeTypes;
using Complex64 = System.Numerics.Complex;
using Void = AddyScript.Runtime.DataItems.Void;
using Boolean = AddyScript.Runtime.DataItems.Boolean;
using Decimal = AddyScript.Runtime.DataItems.Decimal;
using Complex = AddyScript.Runtime.DataItems.Complex;
using String = AddyScript.Runtime.DataItems.String;
using Blob = AddyScript.Runtime.DataItems.Blob;


namespace AddyScript.Parsers;


/// <summary>
/// A parser for expressions only.
/// </summary>
/// <remarks>
/// Initializes a new instance of the parser
/// </remarks>
/// <param name="lexer">The bound lexer</param>
public class ExpressionParser(Lexer lexer) : BasicParser(lexer)
{
    /// <summary>
    /// Recognizes a non-null expression.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.Expression"/></returns>
    public Expression RequiredExpression()
    {
        return Required(Expression, Resources.ExpressionRequired);
    }

    /// <summary>
    /// Recognizes an expression.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.Expression"/></returns>
    public Expression Expression()
    {
        // Well, assignment operators have the lowest priority
        // So we start by parsing an assignment
        return Assignment();
    }

    /// <summary>
    /// Recognizes an expression that can be used as a valid lvalue;
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.Expression"/></returns>
    protected Expression Reference()
    {
        Expression expr = Composite();
        return expr switch
        {
            IReference => expr,
            null => throw new SyntaxError(FileName, token, Resources.ExpressionRequired),
            _ => throw new ScriptError(FileName, expr, Resources.InvalidLValue)
        };
    }

    /// <summary>
    /// Recognizes an assignment.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.Assignment"/></returns>
    private Expression Assignment()
    {
        Expression expr = TernaryExpression();
        if (expr == null) return null;

        if (TryMatchAny(TokenID.Equal, TokenID.PlusEqual, TokenID.MinusEqual, TokenID.AsteriskEqual,
                        TokenID.DoubleAsteriskEqual, TokenID.SlashEqual, TokenID.PercentEqual,
                        TokenID.AmpersandEqual, TokenID.VerticalBarEqual, TokenID.CircumflexEqual,
                        TokenID.DoubleLessThanEqual, TokenID.DoubleGreaterThanEqual, TokenID.DoubleQuestionEqual))
        {
            BinaryOperator oper = token.ToBinaryOperator();
            Consume(1);
            var rvalue = Required(Assignment, Resources.ExpressionRequired);
            expr = new Assignment(oper, expr, rvalue);
        }

        return expr;
    }

    /// <summary>
    /// Recognizes a ternary expression.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.TernaryExpression"/></returns>
    private Expression TernaryExpression()
    {
        Expression test = Condition();
        if (test == null) return null;

        if (!TryMatch(TokenID.Question)) return test;
        Consume(1);

        var truePart = RequiredExpression();
        Match(TokenID.Colon);
        var falsePart = TryMatch(TokenID.KW_Throw) ? ThrowExpression() : RequiredExpression();

        return new TernaryExpression(test, truePart, falsePart);
    }

    /// <summary>
    /// Recognizes a logical expression.
    /// </summary>
    /// <returns>A <see cref="BinaryExpression"/> with the &, &&, |, ||, ^ or ?? operator</returns>
    private Expression Condition()
    {
        Expression expr = Relation();
        if (expr == null) return null;

        var moreRelations = new Queue<(BinaryOperator, Expression)>();

        while (TryMatchAny(TokenID.Ampersand, TokenID.DoubleAmpersand, TokenID.VerticalBar,
                           TokenID.DoubleVerticalBar, TokenID.Circumflex, TokenID.DoubleQuestion))
        {
            BinaryOperator oper = token.ToBinaryOperator();
            Consume(1);

            var relation = oper == BinaryOperator.IfEmpty && TryMatch(TokenID.KW_Throw)
                         ? ThrowExpression()
                         : Required(Relation, Resources.ExpressionRequired);
            
            moreRelations.Enqueue((oper, relation));
        }

        return LeftAssociativeChain(expr, moreRelations);
    }

    /// <summary>
    /// Recognizes a relational expession.
    /// </summary>
    /// <returns>
    /// A <see cref="BinaryExpression"/> with the ==, !=, ===, !==, &lt;, &lt;=, &gt;, &gt;=,
    /// <i>startswith</i>, <i>endswith</i>, <i>contains</i>, <i>matches</i> or <i>in</i> operator;
    /// or a <see cref="TypeVerification"/> (like in <i>expr</i> <b>is</b> <i>typeName</i>)
    /// </returns>
    private Expression Relation()
    {
        Expression expr = Term();
        if (expr == null) return null;

        switch (token.TokenID)
        {
            case TokenID.DoubleEqual or TokenID.ExclamationEqual or TokenID.TripleEqual or
                 TokenID.ExclamationDoubleEqual or TokenID.LessThan or TokenID.LessThanEqual or
                 TokenID.GreaterThan or TokenID.GreaterThanEqual or TokenID.KW_StartsWith or
                 TokenID.KW_EndsWith or TokenID.KW_Contains or TokenID.KW_Matches:
            {
                BinaryOperator oper = token.ToBinaryOperator();
                Consume(1);
                var term = Required(Term, Resources.ExpressionRequired);
                expr = new BinaryExpression(oper, expr, term);
                break;
            }
            case TokenID.KW_Not:
                Consume(1);
                expr = BelongingCheck(expr, true);
                break;
            case TokenID.KW_In:
                expr = BelongingCheck(expr, false);
                break;
            case TokenID.KW_Is:
                expr = SimplePatternMatching(expr);
                break;
        }

        return expr;
    }

    /// <summary>
    /// Recognizes a belonging check expression (one like <i>x in y</i>).
    /// </summary>
    /// <param name="element">The element whose belonging to a container is being checked</param>
    /// <param name="negate">Determines whether the belonging check is negated (like in <i>x not in y</i>)</param>
    /// <returns>
    /// A <see cref="BinaryExpression"/> with the <see cref="BinaryOperator.Contains"/> operator,
    /// possibly negated with a <see cref="UnaryExpression"/> with the <see cref="UnaryOperator.Not"/> operator
    /// </returns>
    private Expression BelongingCheck(Expression element, bool negate)
    {
        Match(TokenID.KW_In);
        var container = Required(Term, Resources.ExpressionRequired);
        Expression expr = new BinaryExpression(BinaryOperator.Contains, container, element);
        if (negate) expr = new UnaryExpression(UnaryOperator.Not, expr);
        expr.SetLocation(element.Start, container.End);
        return expr;
    }

    /// <summary>
    /// Recognizes a simple pattern matching expression (one like <i>expr is [not] pattern</i>).
    /// </summary>
    /// <param name="checkedExpr">The expression being checked against the pattern</param>
    /// <returns>
    /// A <see cref="TypeVerification"/> if the pattern is a type pattern;
    /// or a <see cref="PatternMatching"/> otherwise,
    /// </returns>
    /// <exception cref="SyntaxError">
    /// If no valid pattern is found after the <b>is</b> [<b>not</b>] keywords
    /// </exception>
    private Expression SimplePatternMatching(Expression checkedExpr)
    {
        Match(TokenID.KW_Is);
        
        bool negate = false;
        if (TryMatch(TokenID.KW_Not))
        {
            negate = true;
            Consume(1);
        }

        var pattern = MatchCasePattern();
        Expression expr = pattern switch
        {
            null => throw new SyntaxError(FileName, token, Resources.TypeNameExpected),
            TypePattern typePattern and not ObjectPattern =>
                new TypeVerification(checkedExpr, typePattern.TypeName),
            _ => new PatternMatching(checkedExpr,
                new (pattern, new Literal(Boolean.True)),
                new (new AlwaysPattern(), new Literal(Boolean.False))),
        };

        if (negate) expr = new UnaryExpression(UnaryOperator.Not, expr);
        expr.SetLocation(expr.Start, pattern.End);
        return expr;
    }

    /// <summary>
    /// Recognizes an addition or a subtraction.
    /// </summary>
    /// <returns>A <see cref="BinaryExpression"/> with the + or - operator</returns>
    private Expression Term()
    {
        Expression expr = Factor();
        if (expr == null) return null;

        var moreFactors = new Queue<(BinaryOperator, Expression)>();

        while (TryMatchAny(TokenID.Plus, TokenID.Minus))
        {
            BinaryOperator oper = token.ToBinaryOperator();
            Consume(1);
            var factor = Required(Factor, Resources.ExpressionRequired);
            moreFactors.Enqueue((oper, factor));
        }

        return LeftAssociativeChain(expr, moreFactors);
    }

    /// <summary>
    /// Recognizes a multiplication, a divison, a modulo or a shift.
    /// </summary>
    /// <returns>A <see cref="BinaryExpression"/> with the *, /, %, &lt;&lt; or &gt;&gt; operator</returns>
    private Expression Factor()
    {
        Expression expr = Exponentiation();
        if (expr == null) return null;

        var moreExponentiations = new Queue<(BinaryOperator, Expression)>();

        while (TryMatchAny(TokenID.Asterisk, TokenID.Slash, TokenID.Percent,
                           TokenID.DoubleLessThan, TokenID.DoubleGreaterThan))
        {
            BinaryOperator oper = token.ToBinaryOperator();
            Consume(1);
            var expon = Required(Exponentiation, Resources.ExpressionRequired);
            moreExponentiations.Enqueue((oper, expon));
        }

        return LeftAssociativeChain(expr, moreExponentiations);
    }

    /// <summary>
    /// Recognizes an exponentiation.
    /// </summary>
    /// <returns>A <see cref="BinaryExpression"/> with the ** operator</returns>
    private Expression Exponentiation()
    {
        Expression expr = PostfixUnaryExpression();
        if (expr == null) return null;

        if (TryMatch(TokenID.DoubleAsterisk))
        {
            BinaryOperator oper = token.ToBinaryOperator();
            Consume(1);
            var expon = Required(Exponentiation, Resources.ExpressionRequired);
            expr = new BinaryExpression(oper, expr, expon);
        }

        return expr;
    }

    /// <summary>
    /// Recognizes a unary expression with a postfix operator.
    /// </summary>
    /// <returns>A <see cref="UnaryExpression"/> with the postfix variant of the ++, -- or ! operator</returns>
    private Expression PostfixUnaryExpression()
    {
        Expression expr = PrefixUnaryExpression();
        if (expr == null) return null;

        while (true)
        {
            SkipComments();
            
            Expression operand = expr;
            Token last = token;

            switch (last.TokenID)
            {
                case TokenID.DoublePlus:
                    Consume(1);
                    expr = new UnaryExpression(UnaryOperator.PostIncrement, operand);
                    break;
                case TokenID.DoubleMinus:
                    Consume(1);
                    expr = new UnaryExpression(UnaryOperator.PostDecrement, operand);
                    break;
                case TokenID.Exclamation:
                    Consume(1);
                    expr = new UnaryExpression(UnaryOperator.NotEmpty, operand);
                    break;
                default:
                    return expr;
            }

            expr.SetLocation(operand.Start, last.End);
        }
    }

    /// <summary>
    /// Recognizes a unary expression with a prefix operator.
    /// </summary>
    /// <returns>A <see cref="UnaryExpression"/> with the +, -, ! or ~ operator</returns>
    private Expression PrefixUnaryExpression()
    {
        Expression expr;

        if (TryMatchAny(TokenID.Plus, TokenID.DoublePlus, TokenID.Minus,
                        TokenID.DoubleMinus, TokenID.Exclamation, TokenID.Tilda))
        {
            Token first = token;
            UnaryOperator oper = first.ToUnaryOperator(false);
            Consume(1);
            var operand = Required(PrefixUnaryExpression, Resources.ExpressionRequired);
            expr = new UnaryExpression(oper, operand);
            expr.SetLocation(first.Start, operand.End);
        }
        else
            expr = Composite();

        return expr;
    }

    /// <summary>
    /// Recognizes composite expressions like those with a couple of brackets, a dot or one of the
    /// <b>switch</b> and <b>with</b> operators.
    /// </summary>
    /// <returns>
    /// An <see cref="ItemRef"/>, a <see cref="SliceRef"/>, a <see cref="PropertyRef"/>, a <see cref="MethodCall"/>,
    /// a <see cref="PatternMatching"/> or an <see cref="MutableCopy"/>
    /// </returns>
    private Expression Composite()
    {
        Expression expr = Atom();
        if (expr == null) return null;

        bool doChaining;

        do
        {
            SkipComments();
            doChaining = true;

            switch (token.TokenID)
            {
                case TokenID.LeftBracket or TokenID.QuestionBracket:
                    expr = ItemOrSliceRef(expr);
                    break;
                case TokenID.Dot or TokenID.QuestionDot:
                    expr = MemberRef(expr);
                    break;
                case TokenID.LeftParenthesis:
                {
                    Consume(1);
                    var (positionalArgs, namedArgs) = FunctionArguments();
                    var last = Match(TokenID.RightParenthesis);

                    Expression callee = expr;
                    expr = new AnonymousCall(callee, positionalArgs, namedArgs);
                    expr.SetLocation(callee.Start, last.End);
                    break;
                }
                case TokenID.KW_Switch:
                {
                    Consume(1);
                    Match(TokenID.LeftBrace);
                    MatchCase[] cases = List(MatchCase, false, null);
                    Token last = Match(TokenID.RightBrace);

                    Expression checkedExpr = expr;
                    expr = new PatternMatching(checkedExpr, cases);
                    expr.SetLocation(checkedExpr.Start, last.End);
                    break;
                }
                case TokenID.KW_With:
                {
                    Consume(1);
                    Match(TokenID.LeftBrace);
                    VariableSetter[] fields = List(VariableSetter, true, Resources.DuplicatedProperty);
                    Token last = Match(TokenID.RightBrace);

                    Expression original = expr;
                    expr = new MutableCopy(original, fields);
                    expr.SetLocation(original.Start, last.End);
                    break;
                }
                default:
                    doChaining = false;
                    break;
            }
        }
        while (doChaining);

        return expr;
    }

    /// <summary>
    /// Recognizes an item or a slice reference.
    /// </summary>
    /// <param name="owner">The expression representing the owner of the item or slice being referenced</param>
    /// <returns>A <see cref="ItemRef"/> or a <see cref="SliceRef"/></returns>
    private Expression ItemOrSliceRef(Expression owner)
    {
        Expression lBound = null, uBound = null;
        bool isSlice = false;
        bool isOptional = MatchAny(TokenID.LeftBracket, TokenID.QuestionBracket)
            .TokenID == TokenID.QuestionBracket;

        if (TryMatch(TokenID.DoubleDot))
        {
            Consume(1);
            uBound = Expression();
            isSlice = true;
        }
        else
        {
            lBound = RequiredExpression();

            if (TryMatch(TokenID.DoubleDot))
            {
                Consume(1);
                uBound = Expression();
                isSlice = true;
            }
        }

        var last = Match(TokenID.RightBracket);

        Expression expr = isSlice
            ? new SliceRef(owner, lBound, uBound) { Optional = isOptional }
            : new ItemRef(owner, lBound) { Optional = isOptional };
        expr.SetLocation(owner.Start, last.End);
        return expr;
    }

    /// <summary>
    /// Recognizes a member reference (property or method).
    /// </summary>
    /// <param name="owner">The expression representing the owner of the member being referenced</param>
    /// <returns>A <see cref="PropertyRef"/> or a <see cref="MethodCall"/></returns>
    private Expression MemberRef(Expression owner)
    {
        bool isOptional = MatchAny(TokenID.Dot, TokenID.QuestionDot)
            .TokenID == TokenID.QuestionDot;
        
        var last = Match(TokenID.Identifier);
        string memberName = last.ToString();

        Expression expr;
        if (TryMatch(TokenID.LeftParenthesis))
        {
            Consume(1);
            var (positionalArgs, namedArgs) = FunctionArguments();
            last = Match(TokenID.RightParenthesis);
            expr = new MethodCall(owner, memberName, positionalArgs, namedArgs) { Optional = isOptional };
        }
        else
            expr = new PropertyRef(owner, memberName) { Optional = isOptional };

        expr.SetLocation(owner.Start, last.End);
        return expr;
    }

    /// <summary>
    /// Recognizes atomic expressions like literal values, collection initializers, simple references,
    /// simple calls, conversions, parenthesized expressions and the <b>typeof</b> expression.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.Expression"/></returns>
    private Expression Atom()
    {
        SkipComments();

        return token.TokenID switch
        {
            TokenID.LT_Null => Literal(Void.Value),
            TokenID.LT_Boolean => Literal(Boolean.FromBool((bool)token.Value)),
            TokenID.LT_Integer => Literal(new Integer((int)token.Value)),
            TokenID.LT_Long => Literal(new Long((BigInteger)token.Value)),
            TokenID.LT_Float => Literal(new Float((double)token.Value)),
            TokenID.LT_Decimal => Literal(new Decimal((BigDecimal)token.Value)),
            TokenID.LT_Complex => Literal(new Complex((Complex64)token.Value)),
            TokenID.LT_Date => Literal(new Date((DateTime)token.Value)),
            TokenID.LT_Blob => Literal(new Blob((byte[])token.Value)),
            TokenID.LT_String => Literal(new String((string)token.Value)),
            TokenID.KW_This => SelfReference(),
            TokenID.KW_Super => AtomStartingWithSuper(),
            TokenID.KW_New => AtomStartingWithNew(),
            TokenID.KW_TypeOf => AtomStartingWithTypeOf(),
            TokenID.TypeName => AtomStartingWithTypeName(),
            TokenID.Identifier => AtomStartingWithId(),
            TokenID.LeftParenthesis => AtomStartingWithLParen(),
            TokenID.LeftBrace => AtomStartingWithLBrace(),
            TokenID.LeftBracket => ListInitializer(),
            TokenID.VerticalBar => Lambda(),
            TokenID.KW_Function => InlineFunction(),
            TokenID.MutableString => StringInterpolation(),
            _ => null,
        };
    }

    /// <summary>
    /// Creates an instance of <see cref="Ast.Expressions.Literal"/> with the given value.
    /// </summary>
    /// <param name="value">The value to wrap in a literal expression</param>
    /// <returns>A <see cref="Ast.Expressions.Literal"/></returns>
    private Literal Literal(DataItem value)
    {
        var literal = new Literal(value);
        literal.CopyLocation(token);
        Consume(1);

        return literal;
    }

    /// <summary>
    /// Creates an instance of <see cref="Ast.Expressions.SelfReference"/>.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.SelfReference"/></returns>
    private SelfReference SelfReference()
    {
        if (!CurrentFunction.IsMethod || CurrentFunction.IsStatic)
            throw new SyntaxError(FileName, token, Resources.ThisUsedOutOfMethod);

        var selfRef = new SelfReference();
        selfRef.CopyLocation(token);
        Consume(1);

        return selfRef;
    }

    /// <summary>
    /// Recognizes expressions that start with the <b>super</b> keyword.
    /// </summary>
    /// <returns>
    /// A <see cref="ParentMethodCall"/>, a <see cref="ParentPropertyRef"/>, or a <see cref="ParentIndexerRef"/>
    /// </returns>
    private Expression AtomStartingWithSuper()
    {
        Expression expr;
        Token first = Match(TokenID.KW_Super), last;

        if (!CurrentFunction.IsMethod)
            throw new SyntaxError(FileName, first, Resources.SuperUsedOutOfMethod);

        if (TryMatch(TokenID.DoubleColon))
        {
            Consume(1);
            last = Match(TokenID.Identifier);
            string memberName = last.ToString();

            if (TryMatch(TokenID.LeftParenthesis))
            {
                Consume(1);
                var (positionalArgs, namedArgs) = FunctionArguments();
                expr = new ParentMethodCall(memberName, positionalArgs, namedArgs);
                last = Match(TokenID.RightParenthesis);
            }
            else
                expr = new ParentPropertyRef(memberName);
        }
        else
        {
            Match(TokenID.LeftBracket);
            expr = new ParentIndexerRef(RequiredExpression());
            last = Match(TokenID.RightBracket);
        }

        expr.SetLocation(first.Start, last.End);
        return expr;
    }

    /// <summary>
    /// Recognizes expressions that start with the <b>new</b> keyword.
    /// </summary>
    /// <returns>
    /// An <see cref="ObjectInitializer"/> or a <see cref="ConstructorCall"/>
    /// </returns>
    private Expression AtomStartingWithNew()
    {
        Expression expr;
        Token first = Match(TokenID.KW_New), last = null;

        SkipComments();

        switch (token.TokenID)
        {
            case TokenID.LeftBrace:
            {
                Consume(1);
                expr = new ObjectInitializer(List(VariableSetter, true, Resources.DuplicatedProperty));
                last = Match(TokenID.RightBrace);
                break;
            }
            case TokenID.Identifier:
            {
                QualifiedName className = QualifiedName(ref first, ref last);
                Argument[] positionalArgs = null;
                Dictionary<string, Expression> namedArgs = null;
                VariableSetter[] fields = null;

                if (token.TokenID == TokenID.LeftParenthesis)
                {
                    Consume(1);
                    (positionalArgs, namedArgs) = FunctionArguments();
                    last = Match(TokenID.RightParenthesis);
                }

                if (TryMatch(TokenID.LeftBrace))
                {
                    Consume(1);
                    fields = List(VariableSetter, true, Resources.DuplicatedProperty);
                    last = Match(TokenID.RightBrace);
                }
                else if (last.TokenID != TokenID.RightParenthesis) // Require an empty pair of parenthesis when no initializer is supplied
                {
                    Match(TokenID.LeftParenthesis);
                    last = Match(TokenID.RightParenthesis);
                }

                expr = new ConstructorCall(className, positionalArgs, namedArgs, fields);
                break;
            }
            default:
                throw new SyntaxError(FileName, token, Resources.InvalidNewUsage);
        }

        expr.SetLocation(first.Start, last.End);

        return expr;
    }

    /// <summary>
    /// Recognizes expressions that start with the <b>typeof</b> keyword.
    /// </summary>
    /// <returns>A <see cref="TypeOfExpression"/></returns>
    private Expression AtomStartingWithTypeOf()
    {
        Token first = Match(TokenID.KW_TypeOf);
        Match(TokenID.LeftParenthesis);

        if (!TryMatchAny(TokenID.TypeName, TokenID.Identifier))
            throw new SyntaxError(FileName, token, Resources.TypeNameExpected);
        
        string typeName = token.ToString();
        Consume(1);
        Token last = Match(TokenID.RightParenthesis);

        var typeOf = new TypeOfExpression(typeName);
        typeOf.SetLocation(first.Start, last.End);
        return typeOf;
    }

    /// <summary>
    /// Recognizes expressions that start with a type's name.
    /// </summary>
    /// <returns>
    /// A <see cref="StaticMethodCall"/> or a <see cref="StaticPropertyRef"/>
    /// </returns>
    private Expression AtomStartingWithTypeName()
    {
        Expression expr;

        Token first = Match(TokenID.TypeName);
        Match(TokenID.DoubleColon);
        Token last = Match(TokenID.Identifier);
        var name = new QualifiedName(first.ToString(), last.ToString());

        if (TryMatch(TokenID.LeftParenthesis))
        {
            Consume(1);
            var (positionalArgs, namedArgs) = FunctionArguments();
            expr = new StaticMethodCall(name, positionalArgs, namedArgs);
            last = Match(TokenID.RightParenthesis);
        }
        else
            expr = new StaticPropertyRef(name);

        expr.SetLocation(first.Start, last.End);

        return expr;
    }

    /// <summary>
    /// Recognizes expressions that start with an identifier.
    /// </summary>
    /// <returns>
    /// A <see cref="VariableRef"/>, a <see cref="FunctionCall"/>,
    /// a <see cref="StaticPropertyRef"/> or a <see cref="StaticMethodCall"/>
    /// </returns>
    private Expression AtomStartingWithId()
    {
        Expression expr;
        Token first = null, last = null;
        QualifiedName name = QualifiedName(ref first, ref last);

        if (token.TokenID == TokenID.LeftParenthesis)
        {
            Consume(1);
            var (positionalArgs, namedArgs) = FunctionArguments();
            expr = name.IsIdentifier
                 ? new FunctionCall(name[0].Value, positionalArgs, namedArgs)
                 : new StaticMethodCall(name, positionalArgs, namedArgs);
            last = Match(TokenID.RightParenthesis);
        }
        else
            expr = name.IsIdentifier
                 ? new VariableRef(name[0].Value)
                 : new StaticPropertyRef(name);

        expr.SetLocation(first.Start, last.End);

        return expr;
    }

    /// <summary>
    /// Recognizes expressions that start with an opening parenthesis.
    /// </summary>
    /// <returns>
    /// A <see cref="Conversion"/>, a <see cref="TupleInitializer"/>
    /// or simply a parenthesized <see cref="Ast.Expressions.Expression"/>
    /// </returns>
    private Expression AtomStartingWithLParen()
    {
        Token first = Match(TokenID.LeftParenthesis);

        // Case of a conversion
        if (TryMatch(TokenID.TypeName) && LookAhead(TokenID.RightParenthesis, out int k))
        {
            string typeName = token.ToString();
            Consume(k);

            var converted = RequiredExpression();
            var conversion = new Conversion(converted, typeName);
            conversion.SetLocation(first.Start, converted.End);

            return conversion;
        }

        // Other cases: parenthesized expressions and tuple initializers
        List<Argument> listItems = [];
        Argument item = Argument();
        bool isTuple = false;

        while (item != null)
        {
            listItems.Add(item);
            if (!TryMatch(TokenID.Comma)) break;
            Consume(1);
            item = Argument();
            isTuple = true;
        }

        Token last = Match(TokenID.RightParenthesis);

        if (listItems.Count == 0) throw new SyntaxError(FileName, last);

        Expression expr;
        if (isTuple || listItems[0].Spread)
            expr = new TupleInitializer([.. listItems]);
        else
        {
            expr = listItems[0].Expression;
            expr.IsParenthesized = true;
        }

        expr.SetLocation(first.Start, last.End);
        return expr;
    }

    /// <summary>
    /// Recognizes expressions that start with a left brace.
    /// </summary>
    /// <returns>
    /// A <see cref="MapInitializer"/> or a <see cref="SetInitializer"/>
    /// </returns>
    private Expression AtomStartingWithLBrace()
    {
        List<MapEntry> mapEntries = [];
        List<Argument> setItems = [];

        Token first = Match(TokenID.LeftBrace);
        Argument firstItem = Argument();
        bool isSet = false;

        if (firstItem == null)
        {
            if (TryMatch(TokenID.Arrow))
                Consume(1);
            else
                isSet = true;
        }
        else
        {
            if (TryMatch(TokenID.Arrow))
            {
                if (firstItem.Spread) throw new SyntaxError(FileName, token);

                Consume(1);
                var value = RequiredExpression();
                var entry = new MapEntry(firstItem.Expression, value);
                entry.SetLocation(firstItem.Start, value.End);
                mapEntries.Add(entry);
            }
            else
            {
                setItems.Add(firstItem);
                isSet = true;
            }

            if (TryMatch(TokenID.Comma)) Consume(1);

            if (isSet)
                setItems.AddRange(List(Argument, false, null));
            else
                mapEntries.AddRange(List(MapEntry, false, null));
        }

        Token last = Match(TokenID.RightBrace);

        Expression initializer = isSet ? new SetInitializer([.. setItems]) : new MapInitializer([.. mapEntries]);
        initializer.SetLocation(first.Start, last.End);
        return initializer;
    }

    /// <summary>
    /// Recognizes a list's initializer.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.ListInitializer"/></returns>
    private Expression ListInitializer()
    {
        Token first = Match(TokenID.LeftBracket);
        Argument[] items = List(Argument, false, null);
        Token last = Match(TokenID.RightBracket);

        var initializer = new ListInitializer(items);
        initializer.SetLocation(first.Start, last.End);
        return initializer;
    }

    /// <summary>
    /// Recognizes a literal string with embedded expressions to interpolate.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.StringInterpolation"/></returns>
    private StringInterpolation StringInterpolation()
    {
        List<Expression> substitutions = [];
        StringBuilder patternBuffer = new ();
        string mutableString = token.ToString();
        int counter = 0, limit = mutableString.Length;
        char ch;

        while (counter < limit)
        {
            ch = mutableString[counter++];

            if (ch == '{')
            {
                patternBuffer.Append(ch);

                if (counter < limit && mutableString[counter] == ch)
                {
                    patternBuffer.Append(ch);
                    ++counter;
                }
                else
                {
                    var auxParser = new ExpressionParser(new Lexer(new StringReader(mutableString[counter..])))
                    {
                        CurrentClass = CurrentClass,
                        CurrentFunction = CurrentFunction,
                    };

                    Expression substitution = auxParser.RequiredExpression();
                    substitution.MoveRel(token, counter + 2); // Note: the + 2 is a bit artificial but it's useful!
                    patternBuffer.Append(substitutions.Count);
                    substitutions.Add(substitution);
                    counter += substitution.Length;

                    if (counter >= limit)
                        throw new ScriptError(FileName, substitution, Resources.MissingClosingBrace);

                    ch = mutableString[counter];

                    if (ch == ',' || ch == ':')
                    {
                        int j = counter + 1;
                        while (j < limit && mutableString[j] != '}') ++j;

                        if (j >= limit)
                            throw new ScriptError(FileName, substitution, Resources.MissingClosingBrace);

                        patternBuffer.Append(mutableString[counter..j]);
                        counter = j;
                    }
                    else if (ch != '}')
                        throw new ScriptError(FileName, substitution, Resources.MissingClosingBrace);
                }
            }
            else
                patternBuffer.Append(ch);
        }

        var stringInt = new StringInterpolation(patternBuffer.ToString(), [.. substitutions]);
        stringInt.CopyLocation(token);
        Consume(1);

        return stringInt;
    }

    /// <summary>
    /// Recognizes a <b>throw</b> statement used as an expresion.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.ThrowExpression"/></returns>
    private ThrowExpression ThrowExpression()
    {
        Token first = Match(TokenID.KW_Throw);
        Expression thrown = RequiredExpression();

        var _throw = new Throw(thrown);
        _throw.SetLocation(first.Start, thrown.End);
        return new ThrowExpression(_throw);
    }

    /// <summary>
    /// Recognizes an inline function's declaration.
    /// This method is not really defined here.
    /// Inline functions are only recognized by the full featured parser.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.InlineFunction"/></returns>
    protected virtual InlineFunction InlineFunction()
    {
        throw new NotSupportedException(Resources.UseParserForInlineFuncs);
    }

    /// <summary>
    /// Recognizes a lambda expression or a lambda statement.
    /// This method is not really defined here.
    /// Inline functions are only recognized by the full featured parser.
    /// </summary>
    /// <returns>An <see cref="InlineFunction"/></returns>
    protected virtual InlineFunction Lambda()
    {
        throw new NotSupportedException(Resources.UseParserForInlineFuncs);
    }

    /// <summary>
    /// Construct a chain of binary expressions associative to the left by combining a first operand
    /// with subsequent operands stored in a queue with corresponding operators.
    /// </summary>
    /// <param name="firstOperand">The first operand of the chain of binary expressions</param>
    /// <param name="moreOps">A <see cref="Queue{T}"/> of (BinaryOperator, Expression) pairs</param>
    /// <returns>A  <see cref="BinaryExpression"/></returns>
    private static Expression LeftAssociativeChain(Expression firstOperand, Queue<(BinaryOperator, Expression)> moreOps)
    {
        Expression chainExpr = firstOperand;

        while (moreOps.Count > 0)
        {
            var (_operator, operand) = moreOps.Dequeue();
            chainExpr = new BinaryExpression(_operator, chainExpr, operand);
        }

        return chainExpr;
    }

    /// <summary>
    /// Recognizes a qualified name.
    /// </summary>
    /// <param name="first">A <see cref="Token"/> marking the start of the qualified name</param>
    /// <param name="last">A <see cref="Token"/> marking the end of the qualified name</param>
    /// <returns>A <see cref="Ast.Expressions.QualifiedName"/></returns>
    protected QualifiedName QualifiedName(ref Token first, ref Token last)
    {
        List<NamePart> parts = [];
        string ident;
        int paramCount;

        while (TryMatch(TokenID.Identifier))
        {
            last = token;
            first ??= last;
            ident = last.ToString();
            paramCount = 0;

            Consume(1);

            if (TryMatch(TokenID.LeftBrace) && LookAhead(TokenID.LT_Integer, out int k))
            {
                Consume(k - 1); // To skip the left brace and all that precedes the literal integer
                paramCount = (int)token.Value;
                Consume(1); // To skip the literal integer
                Match(TokenID.RightBrace);
            }

            parts.Add(new NamePart(ident, paramCount));
            
            if (!TryMatch(TokenID.DoubleColon)) break;

            Consume(1);
        }

        return new QualifiedName([.. parts]);
    }

    /// <summary>
    /// Recognizes a property's initializer.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.VariableSetter"/></returns>
    protected VariableSetter VariableSetter()
    {
        if (!TryMatch(TokenID.Identifier)) return null;

        Token first = token;
        string fieldName = first.ToString();
        Consume(1);
        Match(TokenID.Equal);
        Expression fieldValue = RequiredExpression();
        
        var initializer = new VariableSetter(fieldName, fieldValue);
        initializer.SetLocation(first.Start, fieldValue.End);
        return initializer;
    }

    /// <summary>
    /// Recognizes a list or set item initializer.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.Argument"/></returns>
    private Argument Argument()
    {
        ScriptLocation start = null;
        Expression expr;
        bool spread;

        if (TryMatch(TokenID.DoubleDot))
        {
            start = token.Start;
            Consume(1);
            expr = RequiredExpression();
            spread = true;
        }
        else
        {
            expr = Expression();
            spread = false;
        }

        if (expr == null) return null;

        var item = new Argument(expr, spread);
        item.SetLocation(start ?? expr.Start, expr.End);
        return item;
    }

    /// <summary>
    /// Recognizes a map item initializer.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.MapEntry"/></returns>
    private MapEntry MapEntry()
    {
        Expression key = Expression();
        if (key == null) return null;

        Match(TokenID.Arrow);
        var value = RequiredExpression();

        var initializer = new MapEntry(key, value);
        initializer.SetLocation(key.Start, value.End);
        return initializer;
    }

    /// <summary>
    /// Recognizes the set of arguments passed to a function or method when it's called.
    /// </summary>
    /// <returns>A (ListItem[], Dictionary{string, Expression})" tuple</returns>
    protected (Argument[], Dictionary<string, Expression>) FunctionArguments()
    {
        List<Argument> positionalArgs = [];
        Dictionary<string, Expression> namedArgs = [];
        int section = 0; // 0 => positional arguments section

        while (section == 0)
        {
            if (TryMatch(TokenID.Identifier) && LookAhead(TokenID.Colon, out int k))
            {
                string argName = token.ToString();
                Consume(k);
                namedArgs.Add(argName, RequiredExpression());

                if (TryMatch(TokenID.Comma))
                {
                    Consume(1);
                    section = 1; // 1 => named arguments section
                }
                else
                    section = -1; // end of argument stream
            }
            else
            {
                Argument arg = Argument();
                
                if (arg != null)
                {
                    positionalArgs.Add(arg);

                    if (TryMatch(TokenID.Comma))
                        Consume(1);
                    else
                        section = -1;
                }
                else if (positionalArgs.Count > 0)
                    throw new SyntaxError(FileName, token, Resources.AbnormalListTermination);
                else
                    section = -1;
            }
        }

        while (section == 1)
        {
            Token argName = Match(TokenID.Identifier);
            Match(TokenID.Colon);

            try
            {
                namedArgs.Add(argName.ToString(), RequiredExpression());
            }
            catch (ArgumentException ex)
            {
                throw new SyntaxError(FileName, argName, ex);
            }

            if (TryMatch(TokenID.Comma))
                Consume(1);
            else
                section = -1;
        }

        return ([.. positionalArgs], namedArgs);
    }

    /// <summary>
    /// Recognizes a case in a <see cref="PatternMatching"/>
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.MatchCase"/></returns>
    private MatchCase MatchCase()
    {
        Pattern pattern = MatchCasePattern();
        if (pattern == null) return null;

        Expression guard = null;
        if (TryMatch(TokenID.KW_When))
        {
            Consume(1);
            guard = RequiredExpression();
        }

        Match(TokenID.Arrow);
        Expression expr = MatchCaseExpression();

        var matchCase = new MatchCase(pattern, expr) {Guard = guard};
        matchCase.SetLocation(pattern.Start, expr.End);
        return matchCase;
    }

    /// <summary>
    /// Recognizes the <see cref="Pattern"/> component of a <see cref="Ast.Expressions.MatchCase"/>.
    /// </summary>
    /// <returns>A <see cref="Pattern"/></returns>
    private Pattern MatchCasePattern()
    {
        Queue<(Pattern, bool)> pairs = [];
        bool inclusiveCmp = false; // inclusive composition ('and' instead of 'or')
        bool morePatterns; // look-up for more pattern => chaining

        do
        {
            SkipComments();

            Token first = token, last = first;
            bool negate = false;

            if (token.TokenID == TokenID.KW_Not)
            {
                negate = true;
                Consume(1);
                SkipComments();
            }

            Pattern pattern = MatchCaseSimplePattern(ref last);
            
            if (pattern != null)
            {
                if (negate) pattern = new NegativePattern(pattern);
                pattern.SetLocation(first.Start, last.End);
                pairs.Enqueue((pattern, inclusiveCmp));
            }
            else if (negate) // 'not' used without a following pattern
                throw new SyntaxError(FileName, first);

            SkipComments();

            if (token.TokenID is TokenID.KW_And or TokenID.KW_Or)
            {
                morePatterns = true;
                inclusiveCmp = token.TokenID == TokenID.KW_And;
                Consume(1);
            }
            else
                morePatterns = false;
        }
        while (morePatterns);

        if (pairs.Count == 0) return null;

        var (left, _) = pairs.Dequeue();
        while (pairs.Count > 0)
        {
            var (right, incl) = pairs.Dequeue();
            left = new CompositePattern(incl, left, right);
            left.SetLocation(left.Start, right.End);
        }
        return left;
    }

    /// <summary>
    /// Recognizes a simple pattern <see cref="Pattern"/>.
    /// One that does not include the <b>not</b>, <b>and</b>, <b>or</b> operators.
    /// Conditionally recognizes the <b>when</b> operator.
    /// </summary>
    /// <param name="predicateAllowed">Determines whether <b>when</b> is allowed or not</param>
    /// <param name="last">A reference to the last terminal symbol that should be returned</param>
    /// <returns>A <see cref="Pattern"/></returns>
    /// <exception cref="SyntaxError">If an invalid symbol is met</exception>
    private Pattern MatchCaseSimplePattern(ref Token last)
    {
        Pattern pattern = null;

        switch (token.TokenID)
        {
            case TokenID.LT_Null:
                pattern = new NullPattern();
                Consume(1);
                break;
            case TokenID.LT_Boolean or TokenID.LT_Integer or TokenID.LT_Long or TokenID.LT_Float or
                 TokenID.LT_Decimal or TokenID.LT_Date or TokenID.LT_String:
                pattern = MatchCaseValueOrRangePattern(true, false, ref last);
                break;
            case TokenID.DoubleDot:
                pattern = MatchCaseValueOrRangePattern(false, false, ref last);
                break;
            case TokenID.Plus or TokenID.Minus:
            {
                if (!LookAhead(t => t.IsNumeric, out int pos)) throw new SyntaxError(FileName, token);
                bool negateLBound = token.TokenID == TokenID.Minus;
                Consume(pos - 1);
                pattern = MatchCaseValueOrRangePattern(true, negateLBound, ref last);
                break;
            }
            case TokenID.TypeName or TokenID.Identifier:
            {
                string typeName = token.ToString();
                Consume(1);

                if (typeName == AlwaysPattern.Symbol)
                    pattern = new AlwaysPattern();
                else if (TryMatch(TokenID.LeftBrace))
                    pattern = MatchCaseObjectPattern(typeName, ref last);
                else
                    pattern = new TypePattern(typeName);
                break;
            }
            case TokenID.LeftBrace:
                pattern = MatchCaseObjectPattern(null, ref last);
                break;
            case TokenID.LeftParenthesis:
            {
                Token first = token;
                Consume(1);
                pattern = new GroupingPattern(MatchCasePattern());
                last = Match(TokenID.RightParenthesis);
                pattern.SetLocation(first.Start, last.End);
                break;
            }
        }

        return pattern;
    }

    /// <summary>
    /// Recognizes a <see cref="ValuePattern"/> or a <see cref="RangePattern"/>.
    /// </summary>
    /// <param name="withLBound">Determines if the lower bound of arange should be parsed</param>
    /// <param name="negateLBound">Determines if a negative signed was initially met for the lower bound</param>
    /// <param name="last">A reference to the last terminal symbol that should be returned</param>
    /// <returns>A <see cref="Pattern"/></returns>
    private Pattern MatchCaseValueOrRangePattern(bool withLBound, bool negateLBound, ref Token last)
    {
        Pattern pattern;
        DataItem lBound = null;
        TokenID? lBoundID = null;

        if (withLBound)
        {
            lBound = MatchCasePatternLiteralValue(negateLBound, ref last);
            lBoundID = last.TokenID;
        }

        if (TryMatch(TokenID.DoubleDot))
        {
            last = token;
            Consume(1);

            bool negateUBound = false;
            if (TryMatchAny(TokenID.Plus, TokenID.Minus) &&
                LookAhead(t => t.IsNumeric, out int pos))
            {
                negateUBound = token.TokenID == TokenID.Minus;
                Consume(pos - 1);
            }

            if (lBoundID == null || TryMatch(lBoundID.Value))
            {
                DataItem uBound = MatchCasePatternLiteralValue(negateUBound, ref last);
                pattern = new RangePattern(lBound, uBound);
            }
            else
                pattern = new RangePattern(lBound, null);
        }
        else
            pattern = new ValuePattern(lBound);

        return pattern;
    }

    /// <summary>
    /// Parses an object pattern from the input stream, matching the specified type name and updating the token
    /// reference to the last token consumed.
    /// </summary>
    /// <param name="typeName">The name of the type to associate with the parsed object pattern.</param>
    /// <param name="last">When this method returns, contains a reference to the last token consumed during parsing.</param>
    /// <returns>An ObjectPattern instance representing the parsed object pattern with the specified type name.</returns>
    private ObjectPattern MatchCaseObjectPattern(string typeName, ref Token last)
    {
        Token first = Match(TokenID.LeftBrace);
        PropertyMatcher[] matchers = List(ObjectPatternPropertyMatcher, false, null);
        last = Match(TokenID.RightBrace);

        var objPattern = new ObjectPattern(typeName, [.. matchers]);
        objPattern.SetLocation(first.Start, last.End);
        return objPattern;
    }

    /// <summary>
    /// Reads a literal value of one of the types which are allowed for patterns.
    /// </summary>
    /// <param name="negate">Determines if a negative signe was initially met for the value</param>
    /// <param name="last">A reference to the last terminal symbol that should be returned</param>
    /// <returns>A <see cref="DataItem"/></returns>
    private DataItem MatchCasePatternLiteralValue(bool negate, ref Token last)
    {
        last = MatchAny(TokenID.LT_Boolean, TokenID.LT_Integer, TokenID.LT_Long, TokenID.LT_Float,
                        TokenID.LT_Decimal, TokenID.LT_Date, TokenID.LT_String);

        DataItem literalValue = DataItemFactory.CreateDataItem(last.Value);
        if (negate) literalValue = literalValue.UnaryOperation(UnaryOperator.Minus);

        return literalValue;
    }

    /// <summary>
    /// Recognizes a <see cref="PropertyMatcher"/>.
    /// </summary>
    /// <returns>A <see cref="PropertyMatcher"/></returns>
    private PropertyMatcher ObjectPatternPropertyMatcher()
    {
        if (!TryMatch(TokenID.Identifier)) return null;

        Token first = token;
        string fieldName = first.ToString();
        Consume(1);
        Match(TokenID.Colon);
        Pattern fieldPattern = MatchCasePattern();

        var matcher = new PropertyMatcher(fieldName, fieldPattern);
        matcher.SetLocation(first.Start, fieldPattern.End);
        return matcher;
    }

    /// <summary>
    /// Recognizes the <see cref="Ast.Expressions.Expression"/> returned by a <see cref="Ast.Expressions.MatchCase"/>.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.Expression"/></returns>
    /// <remarks>
    /// This basic implementation only recognizes simple expressions.
    /// The full-featured parser overrides it to recognize a richer syntax with blocks.
    /// </remarks>
    protected virtual Expression MatchCaseExpression()
    {
        return TryMatch(TokenID.KW_Throw) ? ThrowExpression() : RequiredExpression();
    }
}