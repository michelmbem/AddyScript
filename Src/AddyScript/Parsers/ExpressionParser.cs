#region 'using' Directives

using System;
using System.Collections.Generic;
using System.Numerics;

using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Properties;
using AddyScript.Runtime;
using AddyScript.Runtime.Dynamics;
using AddyScript.Runtime.NativeTypes;
using Boolean = AddyScript.Runtime.Dynamics.Boolean;
using Decimal = AddyScript.Runtime.Dynamics.Decimal;
using String = AddyScript.Runtime.Dynamics.String;

#endregion

namespace AddyScript.Parsers
{
    /// <summary>
    /// A parser for expressions only.
    /// </summary>
    public class ExpressionParser : BaseParser
    {
        /// <summary>
        /// Initializes a new instance of the parser
        /// </summary>
        /// <param name="lexer">The bound lexer</param>
        public ExpressionParser(Lexer lexer)
            : base(lexer)
        {
        }

        /// <summary>
        /// Recognizes a non-null expression.
        /// </summary>
        /// <returns>An <see cref="Expression"/></returns>
        public Expression RequiredExpression()
        {
            return Required<Expression>(Expression, Resources.ExpressionRequired);
        }

        /// <summary>
        /// Recognizes an expression.
        /// </summary>
        /// <returns>An <see cref="Expression"/></returns>
        public Expression Expression()
        {
            // Well, assignment operators have the lowest priority
            // So we start by parsing an assignment
            return Assignment();
        }

        /// <summary>
        /// Recognizes an assignment.
        /// </summary>
        /// <returns>An <see cref="Assignment"/></returns>
        protected Expression Assignment()
        {
            Expression expr = TernaryExpression();
            if (expr == null) return null;

            if (TryMatchAny(TokenID.Equal, TokenID.PlusEqual,
                            TokenID.MinusEqual, TokenID.AsteriskEqual,
                            TokenID.DoubleAsteriskEqual, TokenID.SlashEqual,
                            TokenID.PercentEqual, TokenID.AmpersandEqual,
                            TokenID.VerticalBarEqual, TokenID.CircumflexEqual,
                            TokenID.DoubleLessThanEqual, TokenID.DoubleGreaterThanEqual,
                            TokenID.DoubleQuestion))
            {
                BinaryOperator oper = token.ToBinaryOperator();
                Consume(1);
                var rvalue = Required<Expression>(Assignment, Resources.ExpressionRequired);
                expr = new Assignment(oper, expr, rvalue);
            }

            return expr;
        }

        /// <summary>
        /// Recognizes a ternary expression.
        /// </summary>
        /// <returns>A <see cref="TernaryExpression"/></returns>
        protected Expression TernaryExpression()
        {
            Expression test = Condition();
            if (test == null) return null;

            if (!TryMatch(TokenID.Question)) return test;
            Consume(1);

            var truePart = Required<Expression>(Expression, Resources.ExpressionRequired);
            Match(TokenID.Colon);
            var falsePart = Required<Expression>(Expression, Resources.ExpressionRequired);

            return new TernaryExpression(test, truePart, falsePart);
        }

        /// <summary>
        /// Recognizes a logical expression.
        /// </summary>
        /// <returns>A <see cref="BinaryExpression"/></returns>
        protected Expression Condition()
        {
            Expression expr = Relation();
            if (expr == null) return null;

            var parts = new Queue<BinaryExpressionPart>();

            while (TryMatchAny(TokenID.Ampersand, TokenID.DoubleAmpersand,
                               TokenID.VerticalBar, TokenID.DoubleVerticalBar,
                               TokenID.Circumflex))
            {
                BinaryOperator oper = token.ToBinaryOperator();
                Consume(1);
                var relation = Required<Expression>(Relation, Resources.ExpressionRequired);
                parts.Enqueue(new BinaryExpressionPart(oper, relation));
            }

            while (parts.Count > 0)
            {
                BinaryExpressionPart part = parts.Dequeue();
                expr = new BinaryExpression(part.Operator, expr, part.Operand);
            }

            return expr;
        }

        /// <summary>
        /// Recognizes a relational expession.
        /// </summary>
        /// <returns>
        /// A <see cref="BinaryExpression"/> or a <see cref="TypeVerification"/>
        /// </returns>
        protected Expression Relation()
        {
            Expression expr = Term();
            if (expr == null) return null;

            switch (token.TokenID)
            {
                case TokenID.DoubleEqual:
                case TokenID.ExclamationEqual:
                case TokenID.TripleEqual:
                case TokenID.ExclamationDoubleEqual:
                case TokenID.LessThan:
                case TokenID.LessThanEqual:
                case TokenID.GreaterThan:
                case TokenID.GreaterThanEqual:
                case TokenID.KW_StartsWith:
                case TokenID.KW_EndsWith:
                case TokenID.KW_Contains:
                case TokenID.KW_Matches:
                    BinaryOperator oper = token.ToBinaryOperator();
                    Consume(1);
                    var term = Required<Expression>(Term, Resources.ExpressionRequired);
                    expr = new BinaryExpression(oper, expr, term);
                    break;
                case TokenID.KW_Is:
                    Consume(1);

                    string typeName;
                    if (TryMatchAny(TokenID.TypeName, TokenID.Identifier))
                    {
                        typeName = token.ToString();
                        Consume(1);
                    }
                    else
                        throw new ParseException(FileName, token, Resources.TypeNameExpected);

                    Expression saved = expr;
                    expr = new TypeVerification(saved, typeName);
                    expr.SetLocation(saved.Start, token.End);
                    break;
            }

            return expr;
        }

        /// <summary>
        /// Recognizes an addition or a subtraction.
        /// </summary>
        /// <returns>A <see cref="BinaryExpression"/></returns>
        protected Expression Term()
        {
            Expression expr = Factor();
            if (expr == null) return null;

            var parts = new Queue<BinaryExpressionPart>();

            while (TryMatchAny(TokenID.Plus, TokenID.Minus))
            {
                BinaryOperator oper = token.ToBinaryOperator();
                Consume(1);
                var factor = Required<Expression>(Factor, Resources.ExpressionRequired);
                parts.Enqueue(new BinaryExpressionPart(oper, factor));
            }

            while (parts.Count > 0)
            {
                BinaryExpressionPart part = parts.Dequeue();
                expr = new BinaryExpression(part.Operator, expr, part.Operand);
            }

            return expr;
        }

        /// <summary>
        /// Recognizes a multiplication, a divison, a modulo or a shift.
        /// </summary>
        /// <returns>A <see cref="BinaryExpression"/></returns>
        protected Expression Factor()
        {
            Expression expr = Exponentiation();
            if (expr == null) return null;

            var parts = new Queue<BinaryExpressionPart>();

            while (TryMatchAny(TokenID.Asterisk, TokenID.Slash, TokenID.Percent,
                               TokenID.DoubleLessThan, TokenID.DoubleGreaterThan))
            {
                BinaryOperator oper = token.ToBinaryOperator();
                Consume(1);
                var expon = Required<Expression>(Exponentiation, Resources.ExpressionRequired);
                parts.Enqueue(new BinaryExpressionPart(oper, expon));
            }

            while (parts.Count > 0)
            {
                BinaryExpressionPart part = parts.Dequeue();
                expr = new BinaryExpression(part.Operator, expr, part.Operand);
            }

            return expr;
        }

        /// <summary>
        /// Recognizes an exponentiation.
        /// </summary>
        /// <returns>A <see cref="BinaryExpression"/></returns>
        protected Expression Exponentiation()
        {
            Expression expr = PostfixedUnaryExpression();
            if (expr == null) return null;

            if (TryMatch(TokenID.DoubleAsterisk))
            {
                BinaryOperator oper = token.ToBinaryOperator();
                Consume(1);
                var expon = Required<Expression>(Exponentiation, Resources.ExpressionRequired);
                expr = new BinaryExpression(oper, expr, expon);
            }

            return expr;
        }

        /// <summary>
        /// Recognizes a unary expression with a post-fixed operator.
        /// </summary>
        /// <returns>A <see cref="UnaryExpression"/></returns>
        protected Expression PostfixedUnaryExpression()
        {
            Expression expr = PrefixedUnaryExpression();
            if (expr == null) return null;

            while (true)
            {
                Expression operand = expr;

                SkipComments();
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
                    default:
                        return expr;
                }

                expr.SetLocation(operand.Start, last.End);
            }
        }

        /// <summary>
        /// Recognizes a unary expression with a prefixed operator.
        /// </summary>
        /// <returns>A <see cref="UnaryExpression"/></returns>
        protected Expression PrefixedUnaryExpression()
        {
            Expression expr;

            if (TryMatchAny(TokenID.Plus, TokenID.DoublePlus,
                            TokenID.Minus, TokenID.DoubleMinus,
                            TokenID.Exclamation, TokenID.Tilda))
            {
                Token first = token;
                UnaryOperator oper = first.ToUnaryOperator();
                Consume(1);
                var operand = Required<Expression>(PrefixedUnaryExpression, Resources.ExpressionRequired);
                expr = new UnaryExpression(oper, operand);
                expr.SetLocation(first.Start, operand.End);
            }
            else
                expr = Composite();

            return expr;
        }

        /// <summary>
        /// Recognizes composite expressions like those with a couple of brackets or a dot.
        /// </summary>
        /// <returns>An <see cref="ItemRef"/>, a <see cref="PropertyRef"/> or a <see cref="MethodCall"/></returns>
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
                    case TokenID.LeftBracket:
                        {
                            Consume(1);
                            var index = Required<Expression>(Expression, Resources.ExpressionRequired);
                            bookmark = Match(TokenID.RightBracket);

                            Expression owner = expr;
                            expr = new ItemRef(owner, index);
                            expr.SetLocation(owner.Start, bookmark.End);
                        }
                        break;
                    case TokenID.Dot:
                        {
                            Consume(1);
                            bookmark = Match(TokenID.Identifier);
                            string memberName = bookmark.ToString();

                            Expression owner = expr;
                            if (TryMatch(TokenID.LeftParenthesis))
                            {
                                Consume(1);
                                Expression[] args = List<Expression>(Expression, false, null);
                                bookmark = Match(TokenID.RightParenthesis);
                                expr = new MethodCall(owner, memberName, args);
                            }
                            else
                                expr = new PropertyRef(owner, memberName);

                            expr.SetLocation(owner.Start, bookmark.End);
                        }
                        break;
                    case TokenID.LeftParenthesis:
                        {
                            Consume(1);
                            Expression[] args = List<Expression>(Expression, false, null);
                            bookmark = Match(TokenID.RightParenthesis);
                            Expression callee = expr;
                            expr = new AnonymousCall(callee, args);
                            expr.SetLocation(callee.Start, bookmark.End);
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
        /// Recognizes atomic expressions like literals, initializers,
        /// simple references, simple calls, conversions, parenthesized
        /// expressions and the <b>typeof</b> expression.
        /// </summary>
        /// <returns>An <see cref="Expression"/></returns>
        protected Expression Atom()
        {
            SkipComments();
            switch (token.TokenID)
            {
                case TokenID.LT_Null:
                    return Literal(null);
                case TokenID.LT_Boolean:
                    return Literal(Boolean.FromBool((bool) token.Value));
                case TokenID.LT_Integer:
                    return Literal(new Integer((int) token.Value));
                case TokenID.LT_Long:
                    return Literal(new Long((BigInteger) token.Value));
                case TokenID.LT_Float:
                    return Literal(new Float((double) token.Value));
                case TokenID.LT_Decimal:
                    return Literal(new Decimal((BigDecimal) token.Value));
                case TokenID.LT_Date:
                    return Literal(new Date((DateTime) token.Value));
                case TokenID.LT_String:
                    return Literal(new String((string) token.Value));
                case TokenID.KW_This:
                    return ThisReference();
                case TokenID.KW_Super:
                    return AtomStartingWithSuper();
                case TokenID.KW_New:
                    return AtomStartingWithNew();
                case TokenID.KW_TypeOf:
                    return AtomStartingWithTypeOf();
                case TokenID.TypeName:
                    return AtomStartingWithTypeName();
                case TokenID.Identifier:
                    return AtomStartingWithId();
                case TokenID.LeftParenthesis:
                    return AtomStartingWithLParen();
                case TokenID.LeftBrace:
                    return AtomStartingWithLBrace();
                case TokenID.LeftBracket:
                    return ListInitializer();
                case TokenID.VerticalBar:
                    return Lambda();
                case TokenID.KW_Function:
                    return InlineFunction();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="Literal"/> with the given value.
        /// </summary>
        /// <param name="value">The value to embed in the outcoming literal</param>
        /// <returns>A <see cref="Literal"/></returns>
        protected Literal Literal(Dynamic value)
        {
            var literal = value == null ? new Literal() : new Literal(value);
            literal.CopyLocation(token);
            Consume(1);

            return literal;
        }

        /// <summary>
        /// Creates an instance of <see cref="ThisReference"/>.
        /// </summary>
        /// <returns>A <see cref="ThisReference"/></returns>
        protected ThisReference ThisReference()
        {
            if (CurrentFunction == null ||
                !CurrentFunction.IsMethod ||
                CurrentFunction.IsStatic)
                throw new ParseException(FileName, token, Resources.ThisUsedOutOfMethod);

            var thisRef = new ThisReference();
            thisRef.CopyLocation(token);
            Consume(1);

            return thisRef;
        }

        /// <summary>
        /// Recognizes expressions that start with the <b>super</b> keyword.
        /// </summary>
        /// <returns>A <see cref="ParentMethodCall"/></returns>
        protected Expression AtomStartingWithSuper()
        {
            Token first = Match(TokenID.KW_Super);
            if (CurrentFunction == null || !CurrentFunction.IsMethod)
                throw new ParseException(FileName, first, Resources.SuperUsedOutOfMethod);

            Match(TokenID.DoubleColon);
            string methodName = Match(TokenID.Identifier).ToString();

            Match(TokenID.LeftParenthesis);
            Expression[] args = List<Expression>(Expression, false, null);
            Token last = Match(TokenID.RightParenthesis);

            var pmc = new ParentMethodCall(methodName, args);
            pmc.SetLocation(first.Start, last.End);
            return pmc;
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
                        PropertyInitializer[] fields = List<PropertyInitializer>(
                            PropertyInitializer, true, Resources.DuplicatedProperty);
                        last = Match(TokenID.RightBrace);
                        expr = new ObjectInitializer(fields);
                    }
                    break;
                case TokenID.Identifier:
                    {
                        QualifiedName className = QualifiedName(ref first, ref last);
                        var args = new Expression[0];

                        if (token.TokenID == TokenID.LeftParenthesis)
                        {
                            Consume(1);
                            args = List<Expression>(Expression, false, null);
                            last = Match(TokenID.RightParenthesis);
                        }

                        if (TryMatch(TokenID.LeftBrace))
                        {
                            Consume(1);
                            PropertyInitializer[] fields = List<PropertyInitializer>(
                                PropertyInitializer, true, Resources.DuplicatedProperty);
                            last = Match(TokenID.RightBrace);
                            expr = new ConstructorCall(className, args, fields);
                        }
                        else
                        {
                            if (last == null) throw new ParseException(FileName, token);
                            expr = new ConstructorCall(className, args, null);
                        }
                    }
                    break;
                default:
                    throw new ParseException(FileName, token, Resources.InvalidNewUsage);
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
                throw new ParseException(FileName, token, Resources.TypeNameExpected);
            
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
                Expression[] args = List<Expression>(Expression, false, null);
                last = Match(TokenID.RightParenthesis);
                expr = new StaticMethodCall(name, args);
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
                Expression[] args = List<Expression>(Expression, false, null);
                last = Match(TokenID.RightParenthesis);
                expr = name.Length == 1
                     ? new FunctionCall(name[0], args)
                     : (Expression) new StaticMethodCall(name, args);
            }
            else
                expr = name.Length == 1
                     ? new VariableRef(name[0])
                     : (Expression) new StaticPropertyRef(name);

            expr.SetLocation(first.Start, last.End);

            return expr;
        }

        /// <summary>
        /// Recognizes expressions that start with an opening parenthesis.
        /// </summary>
        /// <returns>
        /// A <see cref="Conversion"/>, a <see cref="ComplexInitializer"/>
        /// or simply a parenthesized <see cref="Expression"/>
        /// </returns>
        protected Expression AtomStartingWithLParen()
        {
            Token first = Match(TokenID.LeftParenthesis);

            if (TryMatch(TokenID.TypeName))
            {
                int k;
                if (LookAhead(TokenID.RightParenthesis, out k))
                {
                    string typeName = token.ToString();
                    Consume(k);

                    var converted = Required<Expression>(Expression, Resources.ExpressionRequired);
                    var conversion = new Conversion(converted, typeName);
                    conversion.SetLocation(first.Start, converted.End);

                    return conversion;
                }
            }

            var expr = Required<Expression>(Expression, Resources.ExpressionRequired);

            SkipComments();
            Token last = token;
            switch (last.TokenID)
            {
                case TokenID.Comma:
                    Consume(1);
                    var expr1 = Required<Expression>(Expression, Resources.ExpressionRequired);
                    last = Match(TokenID.RightParenthesis);
                    expr = new ComplexInitializer(expr, expr1);
                    break;
                case TokenID.RightParenthesis:
                    Consume(1);
                    expr.IsParenthesized = true;
                    break;
                default:
                    throw new ParseException(FileName, last, Resources.MissingClosingParen);
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
            bool isSet = false;
            var mapItems = new MapItemInitializer[0];
            var setItems = new Expression[0];

            Token first = Match(TokenID.LeftBrace);

            Expression e1 = Expression();
            if (e1 == null)
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
                    Consume(1);
                    var e2 = Required<Expression>(Expression, Resources.ExpressionRequired);
                    var tmpList = new List<MapItemInitializer> { new MapItemInitializer(e1, e2) };
                    if (TryMatch(TokenID.Comma))
                    {
                        Consume(1);
                        tmpList.AddRange(List<MapItemInitializer>(MapItemInitializer, false, null));
                    }
                    mapItems = tmpList.ToArray();
                }
                else
                {
                    isSet = true;
                    var tmpList = new List<Expression> { e1 };
                    if (TryMatch(TokenID.Comma))
                    {
                        Consume(1);
                        tmpList.AddRange(List<Expression>(Expression, false, null));
                    }
                    setItems = tmpList.ToArray();
                }
            }

            Token last = Match(TokenID.RightBrace);

            Expression initializer = isSet
                                   ? new SetInitializer(setItems)
                                   : (Expression) new MapInitializer(mapItems);
            initializer.SetLocation(first.Start, last.End);
            return initializer;
        }

        /// <summary>
        /// Recognizes a list's initializer.
        /// </summary>
        /// <returns>An <see cref="ListInitializer"/></returns>
        protected Expression ListInitializer()
        {
            Token first = Match(TokenID.LeftBracket);
            Expression[] items = List<Expression>(Expression, false, null);
            Token last = Match(TokenID.RightBracket);

            var initializer = new ListInitializer(items);
            initializer.SetLocation(first.Start, last.End);
            return initializer;
        }

        /// <summary>
        /// Recognizes an inline function's declaration.
        /// This method is not really defined here.
        /// Inline functions are only recognized by the full featured parser.
        /// </summary>
        /// <returns>An <see cref="InlineFunction"/></returns>
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
        /// Recognizes a qualified name.
        /// </summary>
        /// <param name="first">A <see cref="Token"/></param>
        /// <param name="last">A <see cref="Token"/></param>
        /// <returns>A <see cref="QualifiedName"/></returns>
        protected QualifiedName QualifiedName(ref Token first, ref Token last)
        {
            var parts = new List<string>();

            while (TryMatch(TokenID.Identifier))
            {
                last = token;
                first = first ?? last;
                parts.Add(last.ToString());
                Consume(1);
                if (!TryMatch(TokenID.DoubleColon)) break;
                Consume(1);
            }

            return new QualifiedName(parts.ToArray());
        }

        /// <summary>
        /// Recognizes a property's initializer.
        /// </summary>
        /// <returns>A <see cref="PropertyInitializer"/></returns>
        protected PropertyInitializer PropertyInitializer()
        {
            if (!TryMatch(TokenID.Identifier)) return null;

            string fieldName = token.ToString();
            Token first = token;
            Consume(1);
            Match(TokenID.Equal);
            var fieldValue = Required<Expression>(Expression, Resources.ExpressionRequired);
            
            var initializer = new PropertyInitializer(fieldName, fieldValue);
            initializer.SetLocation(first.Start, fieldValue.End);
            return initializer;
        }

        /// <summary>
        /// Recognizes a map item's initializer.
        /// </summary>
        /// <returns>A <see cref="MapItemInitializer"/></returns>
        protected MapItemInitializer MapItemInitializer()
        {
            Expression key = Expression();
            if (key == null) return null;

            Match(TokenID.Arrow);
            var value = Required<Expression>(Expression, Resources.ExpressionRequired);

            var initializer = new MapItemInitializer(key, value);
            initializer.SetLocation(key.Start, value.End);
            return initializer;
        }

        #region Nested type : BinaryExpressionPart

        /// <summary>
        /// Represents the right part of a binary expression.<br/>
        /// That is : <i>the operator</i> + <i>the right operand</i>.
        /// </summary>
        protected class BinaryExpressionPart
        {
            /// <summary>
            /// Initializes a new instance of BinaryExpressionPart
            /// </summary>
            /// <param name="_operator"></param>
            /// <param name="operand"></param>
            public BinaryExpressionPart(BinaryOperator _operator, Expression operand)
            {
                Operator = _operator;
                Operand = operand;
            }

            /// <summary>
            /// The operator.
            /// </summary>
            public BinaryOperator Operator { get; private set; }

            /// <summary>
            /// The right operand.
            /// </summary>
            public Expression Operand { get; private set; }
        }

        #endregion
    }
}