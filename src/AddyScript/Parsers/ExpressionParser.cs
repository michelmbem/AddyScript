using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Properties;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;
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
    /// Recognizes an assignment.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.Assignment"/></returns>
    protected Expression Assignment()
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
    protected Expression TernaryExpression()
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
    protected Expression Condition()
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
    /// A <see cref="BinaryExpression"/> with the ==, !=, ===, !==, <, <=, >, >=,
    /// 'startswith', 'endswith', 'contains' or 'matches' operator;
    /// or a <see cref="TypeVerification"/> (like in <i>expr</i> <b>is</b> <i>typeName</i>)
    /// </returns>
    protected Expression Relation()
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
            case TokenID.KW_Is:
                {
                    Consume(1);

                    bool negate = false;
                    if (TryMatch(TokenID.KW_Not))
                    {
                        Consume(1);
                        negate = true;
                    }

                    if (!TryMatchAny(TokenID.TypeName, TokenID.Identifier))
                        throw new SyntaxError(FileName, token, Resources.TypeNameExpected);

                    Token typeName = token;
                    Consume(1);
                    Expression _checked = expr;
                    expr = new TypeVerification(_checked, typeName.ToString());
                    if (negate) expr = new UnaryExpression(UnaryOperator.Not, expr);
                    expr.SetLocation(_checked.Start, typeName.End);
                    break;
                }
        }

        return expr;
    }

    /// <summary>
    /// Recognizes an addition or a subtraction.
    /// </summary>
    /// <returns>A <see cref="BinaryExpression"/> with the + or - operator</returns>
    protected Expression Term()
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
    protected Expression Factor()
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
    protected Expression Exponentiation()
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
    protected Expression PostfixUnaryExpression()
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
    protected Expression PrefixUnaryExpression()
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
    /// a <see cref="PatternMatching"/> or an <see cref="AlteredCopy"/>
    /// </returns>
    protected Expression Composite()
    {
        Expression expr = Atom();
        if (expr == null) return null;

        Token bookmark;
        bool loop = true;

        while (loop)
        {
            SkipComments();

            switch (token.TokenID)
            {
                case TokenID.LeftBracket or TokenID.QuestionBracket:
                    {
                        bool optional = token.TokenID == TokenID.QuestionBracket;
                        Consume(1);

                        Expression lBound = null, uBound = null;
                        bool isSlice = false;

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

                        bookmark = Match(TokenID.RightBracket);

                        Expression owner = expr;
                        expr = isSlice
                             ? new SliceRef(owner, lBound, uBound) { Optional = optional }
                             : new ItemRef(owner, lBound) { Optional = optional };
                        expr.SetLocation(owner.Start, bookmark.End);
                    }
                    break;
                case TokenID.Dot or TokenID.QuestionDot:
                    {
                        bool optional = token.TokenID == TokenID.QuestionDot;
                        Consume(1);

                        bookmark = Match(TokenID.Identifier);
                        string memberName = bookmark.ToString();

                        Expression owner = expr;
                        if (TryMatch(TokenID.LeftParenthesis))
                        {
                            Consume(1);
                            (var positionalArgs, var namedArgs) = FunctionArguments();
                            bookmark = Match(TokenID.RightParenthesis);
                            expr = new MethodCall(owner, memberName, positionalArgs, namedArgs) { Optional = optional };
                        }
                        else
                            expr = new PropertyRef(owner, memberName) { Optional = optional };

                        expr.SetLocation(owner.Start, bookmark.End);
                    }
                    break;
                case TokenID.LeftParenthesis:
                    {
                        Consume(1);
                        (var positionalArgs, var namedArgs) = FunctionArguments();
                        bookmark = Match(TokenID.RightParenthesis);

                        Expression callee = expr;
                        expr = new AnonymousCall(callee, positionalArgs, namedArgs);
                        expr.SetLocation(callee.Start, bookmark.End);
                    }
                    break;
                case TokenID.KW_Switch:
                    {
                        Consume(1);
                        Match(TokenID.LeftBrace);
                        MatchCase[] cases = List(MatchCase, false, null);
                        Token last = Match(TokenID.RightBrace);

                        Expression expr2match = expr;
                        expr = new PatternMatching(expr2match, cases);
                        expr.SetLocation(expr2match.Start, last.End);
                    }
                    break;
                case TokenID.KW_With:
                    {
                        Consume(1);
                        Match(TokenID.LeftBrace);
                        PropertyInitializer[] fields = List(PropertyInitializer, true, Resources.DuplicatedProperty);
                        Token last = Match(TokenID.RightBrace);

                        Expression original = expr;
                        expr = new AlteredCopy(original, fields);
                        expr.SetLocation(original.Start, last.End);
                    }
                    break;
                default:
                    loop = false;
                    break;
            }
        }

        return expr;
    }

    /// <summary>
    /// Recognizes atomic expressions like literal values, collection initializers, simple references,
    /// simple calls, conversions, parenthesized expressions and the <b>typeof</b> expression.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.Expression"/></returns>
    protected Expression Atom()
    {
        SkipComments();

        return token.TokenID switch
        {
            TokenID.LT_Null => Literal(null),
            TokenID.LT_Boolean => Literal(Boolean.FromBool((bool)token.Value)),
            TokenID.LT_Integer => Literal(new Integer((int)token.Value)),
            TokenID.LT_Long => Literal(new Long((BigInteger)token.Value)),
            TokenID.LT_Float => Literal(new Float((double)token.Value)),
            TokenID.LT_Complex => Literal(new Complex((Complex64)token.Value)),
            TokenID.LT_Decimal => Literal(new Decimal((BigDecimal)token.Value)),
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
    protected Literal Literal(DataItem value)
    {
        Literal literal = value != null ? new(value) : new();
        literal.CopyLocation(token);
        Consume(1);

        return literal;
    }

    /// <summary>
    /// Creates an instance of <see cref="Ast.Expressions.SelfReference"/>.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.SelfReference"/></returns>
    protected SelfReference SelfReference()
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
    protected Expression AtomStartingWithSuper()
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
                (var positionalArgs, var namedArgs) = FunctionArguments();
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
    protected Expression AtomStartingWithNew()
    {
        Expression expr;
        Token first = Match(TokenID.KW_New), last = null;

        SkipComments();

        switch (token.TokenID)
        {
            case TokenID.LeftBrace:
                {
                    Consume(1);
                    expr = new ObjectInitializer(List(PropertyInitializer, true, Resources.DuplicatedProperty));
                    last = Match(TokenID.RightBrace);
                }
                break;
            case TokenID.Identifier:
                {
                    QualifiedName className = QualifiedName(ref first, ref last);
                    ListItem[] positionalArgs = null;
                    Dictionary<string, Expression> namedArgs = null;
                    PropertyInitializer[] fields = null;

                    if (token.TokenID == TokenID.LeftParenthesis)
                    {
                        Consume(1);
                        (positionalArgs, namedArgs) = FunctionArguments();
                        last = Match(TokenID.RightParenthesis);
                    }

                    if (TryMatch(TokenID.LeftBrace))
                    {
                        Consume(1);
                        fields = List(PropertyInitializer, true, Resources.DuplicatedProperty);
                        last = Match(TokenID.RightBrace);
                    }
                    else if (last.TokenID != TokenID.RightParenthesis)
                    {
                        Match(TokenID.LeftParenthesis);
                        last = Match(TokenID.RightParenthesis);
                    }

                    expr = new ConstructorCall(className, positionalArgs, namedArgs, fields);
                }
                break;
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
    protected Expression AtomStartingWithTypeOf()
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
    protected Expression AtomStartingWithTypeName()
    {
        Expression expr;

        Token first = Match(TokenID.TypeName);
        Match(TokenID.DoubleColon);
        Token last = Match(TokenID.Identifier);
        var name = new QualifiedName(first.ToString(), last.ToString());

        if (TryMatch(TokenID.LeftParenthesis))
        {
            Consume(1);
            (var positionalArgs, var namedArgs) = FunctionArguments();
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
    protected Expression AtomStartingWithId()
    {
        Expression expr;
        Token first = null, last = null;
        QualifiedName name = QualifiedName(ref first, ref last);

        if (token.TokenID == TokenID.LeftParenthesis)
        {
            Consume(1);
            (var positionalArgs, var namedArgs) = FunctionArguments();
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
    protected Expression AtomStartingWithLParen()
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
        List<ListItem> listItems = [];
        ListItem item = ListItem();
        bool isTuple = false;

        while (item != null)
        {
            listItems.Add(item);
            if (!TryMatch(TokenID.Comma)) break;
            Consume(1);
            item = ListItem();
            isTuple = true;
        }

        Token last = Match(TokenID.RightParenthesis);

        if (listItems.Count <= 0) throw new SyntaxError(FileName, last);

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
    protected Expression AtomStartingWithLBrace()
    {
        List<MapItemInitializer> mapItems = [];
        List<ListItem> setItems = [];

        Token first = Match(TokenID.LeftBrace);
        ListItem firstItem = ListItem();
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
                var mapItem = new MapItemInitializer(firstItem.Expression, value);
                mapItem.SetLocation(firstItem.Start, value.End);
                mapItems.Add(mapItem);
            }
            else
            {
                setItems.Add(firstItem);
                isSet = true;
            }

            if (TryMatch(TokenID.Comma)) Consume(1);

            if (isSet)
                setItems.AddRange(List(ListItem, false, null));
            else
                mapItems.AddRange(List(MapItemInitializer, false, null));
        }

        Token last = Match(TokenID.RightBrace);

        Expression initializer = isSet ? new SetInitializer([.. setItems]) : new MapInitializer([.. mapItems]);
        initializer.SetLocation(first.Start, last.End);
        return initializer;
    }

    /// <summary>
    /// Recognizes a list's initializer.
    /// </summary>
    /// <returns>An <see cref="Ast.Expressions.ListInitializer"/></returns>
    protected Expression ListInitializer()
    {
        Token first = Match(TokenID.LeftBracket);
        ListItem[] items = List(ListItem, false, null);
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
        StringBuilder patternBuffer = new();
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
    /// Recognizes a <b>throw</b> statement being used as an expresion.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.ThrowExpression"/></returns>
    protected ThrowExpression ThrowExpression()
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
    /// <param name="moreOps">A <see cref="Queue{T}"/> of <see cref="(BinaryOperator, Expression)"/> pairs</param>
    /// <returns>A  <see cref="BinaryExpression"/></returns>
    protected static Expression LeftAssociativeChain(Expression firstOperand, Queue<(BinaryOperator, Expression)> moreOps)
    {
        Expression chainExpr = firstOperand;

        while (moreOps.Count > 0)
        {
            (BinaryOperator _operator, Expression operand) = moreOps.Dequeue();
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
    /// <returns>A <see cref="Ast.Expressions.PropertyInitializer"/></returns>
    protected PropertyInitializer PropertyInitializer()
    {
        if (!TryMatch(TokenID.Identifier)) return null;

        Token first = token;
        string fieldName = first.ToString();
        Consume(1);
        Match(TokenID.Equal);
        Expression fieldValue = RequiredExpression();
        
        var initializer = new PropertyInitializer(fieldName, fieldValue);
        initializer.SetLocation(first.Start, fieldValue.End);
        return initializer;
    }

    /// <summary>
    /// Recognizes a list or set item initializer.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.ListItem"/></returns>
    protected ListItem ListItem()
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

        var item = new ListItem(expr, spread);
        item.SetLocation(start ?? expr.Start, expr.End);
        return item;
    }

    /// <summary>
    /// Recognizes a map item initializer.
    /// </summary>
    /// <returns>A <see cref="Ast.Expressions.MapItemInitializer"/></returns>
    protected MapItemInitializer MapItemInitializer()
    {
        Expression key = Expression();
        if (key == null) return null;

        Match(TokenID.Arrow);
        var value = RequiredExpression();

        var initializer = new MapItemInitializer(key, value);
        initializer.SetLocation(key.Start, value.End);
        return initializer;
    }

    /// <summary>
    /// Recognizes the set of arguments passed to a function or method when it's called.
    /// </summary>
    /// <returns>A <see cref="(ListItem[], Dictionary{string, Expression})"/> tuple</returns>
    protected (ListItem[], Dictionary<string, Expression>) FunctionArguments()
    {
        List<ListItem> positionalArgs = [];
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
                ListItem arg = ListItem();
                
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
    protected MatchCase MatchCase()
    {
        Pattern pattern = MatchCasePattern();
        if (pattern == null) return null;

        Match(TokenID.Arrow);
        Expression expr = MatchCaseExpression();

        var matchCase = new MatchCase(pattern, expr);
        matchCase.SetLocation(pattern.Start, expr.End);
        return matchCase;
    }

    /// <summary>
    /// Recognizes the <see cref="Pattern"/> component of a <see cref="Ast.Expressions.MatchCase"/>.
    /// </summary>
    /// <returns>A <see cref="Pattern"/></returns>
    protected Pattern MatchCasePattern()
    {
        var patterns = new List<Pattern>();
        bool loop = true;
        int pos;

        while (loop)
        {
            SkipComments();

            Token first = token, last = first;
            Pattern pattern = null;
            bool negative = false;

            switch (token.TokenID)
            {
                case TokenID.LT_Null:
                    pattern = new NullPattern();
                    Consume(1);
                    break;
                case TokenID.LT_Boolean or TokenID.LT_Integer or TokenID.LT_Long or TokenID.LT_Float or
                     TokenID.LT_Decimal or TokenID.LT_Date or TokenID.LT_String:
                literal_value:
                    {
                        TokenID lBoundID = token.TokenID;
                        DataItem lBound = DataItemFactory.CreateDataItem(token.Value);
                        if (negative) lBound = lBound.UnaryOperation(UnaryOperator.Minus);
                        Consume(1);

                        if (TryMatch(TokenID.DoubleDot))
                        {
                            last = token;
                            Consume(1);

                            if (TryMatchAny(TokenID.Plus, TokenID.Minus) && LookAhead(t => t.IsNumeric, out pos))
                            {
                                negative = token.TokenID == TokenID.Minus;
                                Consume(pos - 1);
                            }
                            else
                                negative = false;

                            if (TryMatch(lBoundID))
                            {
                                last = token;
                                DataItem uBound = DataItemFactory.CreateDataItem(last.Value);
                                if (negative) uBound = uBound.UnaryOperation(UnaryOperator.Minus);
                                pattern = new RangePattern(lBound, uBound);
                                Consume(1);
                            }
                            else
                                pattern = new RangePattern(lBound, null);
                        }
                        else
                            pattern = new ValuePattern(lBound);
                    }
                    break;
                case TokenID.DoubleDot:
                    {
                        Consume(1);

                        if (TryMatchAny(TokenID.Plus, TokenID.Minus) && LookAhead(t => t.IsNumeric, out pos))
                        {
                            negative = token.TokenID == TokenID.Minus;
                            Consume(pos - 1);
                        }

                        last = MatchAny(TokenID.LT_Boolean, TokenID.LT_Integer, TokenID.LT_Long, TokenID.LT_Float,
                                        TokenID.LT_Decimal, TokenID.LT_Date, TokenID.LT_String);

                        DataItem uBound = DataItemFactory.CreateDataItem(last.Value);
                        if (negative) uBound = uBound.UnaryOperation(UnaryOperator.Minus);
                        pattern = new RangePattern(null, uBound);
                    }
                    break;
                case TokenID.TypeName:
                    {
                        string typeName = token.ToString();
                        Consume(1);

                        if (typeName == AlwaysPattern.Symbol)
                            pattern = new AlwaysPattern();
                        else if (TryMatch(TokenID.LeftBrace))
                            pattern = new ObjectPattern(typeName, MatchCaseObjectPattern(ref last));
                        else
                            pattern = new TypePattern(typeName);
                    }
                    break;
                case TokenID.Identifier:
                    if (LookAhead(TokenID.Colon, out pos))
                    {
                        string parameterName = token.ToString();
                        Consume(pos);
                        Expression predicate = RequiredExpression();
                        pattern = new PredicatePattern(parameterName, predicate);
                        last.CopyLocation(predicate);
                        break;
                    }

                    goto case TokenID.TypeName;
                case TokenID.LeftBrace:
                    pattern = new ObjectPattern(Class.Object.Name, MatchCaseObjectPattern(ref last));
                    break;
                case TokenID.Plus or TokenID.Minus:
                    if (!LookAhead(t => t.IsNumeric, out pos)) throw new SyntaxError(FileName, token);

                    negative = token.TokenID == TokenID.Minus;
                    Consume(pos - 1);

                    goto literal_value;
                case TokenID.Comma:
                    if (patterns.Count > 0)
                        Consume(1);
                    else
                        throw new SyntaxError(FileName, token);
                    break;
                default:
                    loop = false;
                    break;
            }

            if (pattern != null)
            {
                pattern.SetLocation(first.Start, last.End);
                patterns.Add(pattern);
            }
        }

        if (patterns.Count <= 0) return null;
        if (patterns.Count == 1) return patterns[0];

        var composite = new CompositePattern([.. patterns]);
        composite.SetLocation(patterns[0].Start, patterns[^1].End);
        return composite;
    }

    /// <summary>
    /// Recognizes the <see cref="ObjectPattern.Example"/> member.
    /// </summary>
    /// <returns>An <see cref="Object"/> with literal property values</returns>
    protected DataItem MatchCaseObjectPattern(ref Token last)
    {
        var fieldBag = new Dictionary<string, DataItem>();
        bool negative = false, loop = true;
        Token fieldValueToken;

        Match(TokenID.LeftBrace);

        while (loop)
        {
            string fieldName = Match(TokenID.Identifier).ToString();
            Match(TokenID.Equal);

            if (TryMatchAny(TokenID.Plus, TokenID.Minus) && LookAhead(t => t.IsNumeric, out int pos))
            {
                negative = token.TokenID == TokenID.Minus;
                Consume(pos - 1);
                fieldValueToken = token;
                Consume(1);
            }
            else
                fieldValueToken = MatchAny(TokenID.LT_Integer, TokenID.LT_Long, TokenID.LT_Float,
                                           TokenID.LT_Decimal, TokenID.LT_Date, TokenID.LT_String);
            
            DataItem fieldValue = DataItemFactory.CreateDataItem(fieldValueToken.Value);
            if (negative) fieldValue = fieldValue.UnaryOperation(UnaryOperator.Minus);
            fieldBag.Add(fieldName, fieldValue);

            if (TryMatch(TokenID.Comma))
                Consume(1);
            else
                loop = false;
        }

        last = Match(TokenID.RightBrace);

        return new Runtime.DataItems.Object(fieldBag);
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