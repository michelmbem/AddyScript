using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Properties;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;
using Boolean = AddyScript.Runtime.DataItems.Boolean;
using Decimal = AddyScript.Runtime.DataItems.Decimal;
using String = AddyScript.Runtime.DataItems.String;


namespace AddyScript.Parsers
{
    /// <summary>
    /// A parser for expressions only.
    /// </summary>
    public class ExpressionParser : BasicParser
    {
        /// <summary>
        /// Initializes a new instance of the parser
        /// </summary>
        /// <param name="lexer">The bound lexer</param>
        public ExpressionParser(Lexer lexer) : base(lexer)
        {
        }

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
            var falsePart = RequiredExpression();

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

            var moreRelations = new Queue<Tuple<BinaryOperator, Expression>>();

            while (TryMatchAny(TokenID.Ampersand, TokenID.DoubleAmpersand, TokenID.VerticalBar,
                               TokenID.DoubleVerticalBar, TokenID.Circumflex, TokenID.DoubleQuestion))
            {
                BinaryOperator oper = token.ToBinaryOperator();
                Consume(1);
                var relation = Required(Relation, Resources.ExpressionRequired);
                moreRelations.Enqueue(new Tuple<BinaryOperator, Expression>(oper, relation));
            }

            while (moreRelations.Count > 0)
            {
                var tuple = moreRelations.Dequeue();
                expr = new BinaryExpression(tuple.Item1, expr, tuple.Item2);
            }

            return expr;
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
                    var term = Required(Term, Resources.ExpressionRequired);
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
        /// <returns>A <see cref="BinaryExpression"/> with the + or - operator</returns>
        protected Expression Term()
        {
            Expression expr = Factor();
            if (expr == null) return null;

            var moreFactors = new Queue<Tuple<BinaryOperator, Expression>>();

            while (TryMatchAny(TokenID.Plus, TokenID.Minus))
            {
                BinaryOperator oper = token.ToBinaryOperator();
                Consume(1);
                var factor = Required(Factor, Resources.ExpressionRequired);
                moreFactors.Enqueue(new Tuple<BinaryOperator, Expression>(oper, factor));
            }

            while (moreFactors.Count > 0)
            {
                var tuple = moreFactors.Dequeue();
                expr = new BinaryExpression(tuple.Item1, expr, tuple.Item2);
            }

            return expr;
        }

        /// <summary>
        /// Recognizes a multiplication, a divison, a modulo or a shift.
        /// </summary>
        /// <returns>A <see cref="BinaryExpression"/> with the *, /, %, &lt;&lt; or &gt;&gt; operator</returns>
        protected Expression Factor()
        {
            Expression expr = Exponentiation();
            if (expr == null) return null;

            var moreExponentiations = new Queue<Tuple<BinaryOperator, Expression>>();

            while (TryMatchAny(TokenID.Asterisk, TokenID.Slash, TokenID.Percent,
                               TokenID.DoubleLessThan, TokenID.DoubleGreaterThan))
            {
                BinaryOperator oper = token.ToBinaryOperator();
                Consume(1);
                var expon = Required(Exponentiation, Resources.ExpressionRequired);
                moreExponentiations.Enqueue(new Tuple<BinaryOperator, Expression>(oper, expon));
            }

            while (moreExponentiations.Count > 0)
            {
                var tuple = moreExponentiations.Dequeue();
                expr = new BinaryExpression(tuple.Item1, expr, tuple.Item2);
            }

            return expr;
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
                    case TokenID.QuestionBracket:
                        {
                            bool optional = token.TokenID == TokenID.QuestionBracket;
                            Consume(1);

                            var index = RequiredExpression();
                            bookmark = Match(TokenID.RightBracket);

                            Expression owner = expr;
                            expr = new ItemRef(owner, index) { Optional = optional };
                            expr.SetLocation(owner.Start, bookmark.End);
                        }
                        break;
                    case TokenID.Dot:
                    case TokenID.QuestionDot:
                        {
                            bool optional = token.TokenID == TokenID.QuestionDot;
                            Consume(1);

                            bookmark = Match(TokenID.Identifier);
                            string memberName = bookmark.ToString();

                            Expression owner = expr;
                            if (TryMatch(TokenID.LeftParenthesis))
                            {
                                Consume(1);
                                var args = FunctionArguments();
                                bookmark = Match(TokenID.RightParenthesis);
                                expr = new MethodCall(owner, memberName, args.Item1, args.Item2) { Optional = optional };
                            }
                            else
                                expr = new PropertyRef(owner, memberName) { Optional = optional };

                            expr.SetLocation(owner.Start, bookmark.End);
                        }
                        break;
                    case TokenID.LeftParenthesis:
                        {
                            Consume(1);
                            var args = FunctionArguments();
                            bookmark = Match(TokenID.RightParenthesis);

                            Expression callee = expr;
                            expr = new AnonymousCall(callee, args.Item1, args.Item2);
                            expr.SetLocation(callee.Start, bookmark.End);
                        }
                        break;
                    case TokenID.KW_Switch:
                        {
                            Consume(1);
                            Match(TokenID.LeftBrace);
                            MatchCase[] cases = Asterisk(MatchCase);
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

            switch (token.TokenID)
            {
                case TokenID.LT_Null:
                    return Literal(null);
                case TokenID.LT_Boolean:
                    return Literal(Boolean.FromBool((bool)token.Value));
                case TokenID.LT_Integer:
                    return Literal(new Integer((int)token.Value));
                case TokenID.LT_Long:
                    return Literal(new Long((BigInteger)token.Value));
                case TokenID.LT_Float:
                    return Literal(new Float((double)token.Value));
                case TokenID.LT_Decimal:
                    return Literal(new Decimal((BigDecimal)token.Value));
                case TokenID.LT_Date:
                    return Literal(new Date((DateTime)token.Value));
                case TokenID.LT_String:
                    return Literal(new String((string)token.Value));
                case TokenID.KW_This:
                    return SelfReference();
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
                case TokenID.MutableString:
                    return StringInterpolation();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="Ast.Expressions.Literal"/> with the given value.
        /// </summary>
        /// <param name="value">The value to embed in the outcoming literalSegment</param>
        /// <returns>A <see cref="Ast.Expressions.Literal"/></returns>
        protected Literal Literal(DataItem value)
        {
            var literal = value == null ? new Literal() : new Literal(value);
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
            if (CurrentFunction == null || !CurrentFunction.IsMethod || CurrentFunction.IsStatic)
                throw new ParseException(FileName, token, Resources.ThisUsedOutOfMethod);

            var selfRef = new SelfReference();
            selfRef.CopyLocation(token);
            Consume(1);

            return selfRef;
        }

        /// <summary>
        /// Recognizes expressions that start with the <b>super</b> keyword.
        /// </summary>
        /// <returns>
        /// A <see cref="ParentMethodCall"/> or a <see cref="ParentPropertyRef"/> or a <see cref="ParentIndexerRef"/>
        /// </returns>
        protected Expression AtomStartingWithSuper()
        {
            Expression expr;
            Token first = Match(TokenID.KW_Super), last;

            if (CurrentFunction == null || !CurrentFunction.IsMethod)
                throw new ParseException(FileName, first, Resources.SuperUsedOutOfMethod);

            if (TryMatch(TokenID.DoubleColon))
            {
                Consume(1);
                string memberName = Match(TokenID.Identifier).ToString();

                if (TryMatch(TokenID.LeftParenthesis))
                {
                    Consume(1);
                    var args = FunctionArguments();
                    last = Match(TokenID.RightParenthesis);

                    expr = new ParentMethodCall(memberName, args.Item1, args.Item2);
                }
                else
                {
                    last = token;
                    expr = new ParentPropertyRef(memberName);
                }
            }
            else
            {
                Match(TokenID.LeftBracket);
                var index = RequiredExpression();
                last = Match(TokenID.RightBracket);

                expr = new ParentIndexerRef(index);
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
                        PropertyInitializer[] fields = List(PropertyInitializer, true, Resources.DuplicatedProperty);
                        last = Match(TokenID.RightBrace);
                        expr = new ObjectInitializer(fields);
                    }
                    break;
                case TokenID.Identifier:
                    {
                        QualifiedName className = QualifiedName(ref first, ref last);
                        Tuple<Expression[], Dictionary<string, Expression>> args;

                        if (token.TokenID == TokenID.LeftParenthesis)
                        {
                            Consume(1);
                            args = FunctionArguments();
                            last = Match(TokenID.RightParenthesis);
                        }
                        else
                            args = new Tuple<Expression[], Dictionary<string, Expression>>(null, null);

                        if (TryMatch(TokenID.LeftBrace))
                        {
                            Consume(1);
                            PropertyInitializer[] fields = List(PropertyInitializer, true, Resources.DuplicatedProperty);
                            last = Match(TokenID.RightBrace);
                            expr = new ConstructorCall(className, args.Item1, args.Item2, fields);
                        }
                        else
                        {
                            if (last == null) throw new ParseException(FileName, token);
                            expr = new ConstructorCall(className, args.Item1, args.Item2);
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
                var args = FunctionArguments();
                last = Match(TokenID.RightParenthesis);
                expr = new StaticMethodCall(name, args.Item1, args.Item2);
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
                var args = FunctionArguments();
                last = Match(TokenID.RightParenthesis);
                expr = name.IsIdentifier
                     ? new FunctionCall(name[0].Value, args.Item1, args.Item2)
                     : new StaticMethodCall(name, args.Item1, args.Item2);
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
        /// A <see cref="Conversion"/>, a <see cref="ComplexInitializer"/>
        /// or simply a parenthesized <see cref="Ast.Expressions.Expression"/>
        /// </returns>
        protected Expression AtomStartingWithLParen()
        {
            Token first = Match(TokenID.LeftParenthesis);

            if (TryMatch(TokenID.TypeName) && LookAhead(TokenID.RightParenthesis, out int k))
            {
                string typeName = token.ToString();
                Consume(k);

                var converted = RequiredExpression();
                var conversion = new Conversion(converted, typeName);
                conversion.SetLocation(first.Start, converted.End);

                return conversion;
            }

            var expr = RequiredExpression();

            SkipComments();
            Token last = token;

            switch (last.TokenID)
            {
                case TokenID.Comma:
                    Consume(1);
                    var otherExpr = RequiredExpression();
                    last = Match(TokenID.RightParenthesis);
                    expr = new ComplexInitializer(expr, otherExpr);
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
            MapItemInitializer[] mapItems = [];
            Expression[] setItems = [];

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
                    var e2 = RequiredExpression();
                    var tmpList = new List<MapItemInitializer> { new (e1, e2) };

                    if (TryMatch(TokenID.Comma))
                    {
                        Consume(1);
                        tmpList.AddRange(List(MapItemInitializer, false, null));
                    }

                    mapItems = [.. tmpList];
                }
                else
                {
                    isSet = true;
                    var tmpList = new List<Expression> { e1 };

                    if (TryMatch(TokenID.Comma))
                    {
                        Consume(1);
                        tmpList.AddRange(List(Expression, false, null));
                    }

                    setItems = [.. tmpList];
                }
            }

            Token last = Match(TokenID.RightBrace);

            Expression initializer = isSet ? new SetInitializer(setItems) : new MapInitializer(mapItems);
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
            Expression[] items = List(Expression, false, null);
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
            var substitutions = new List<Expression>();
            var patternBuffer = new StringBuilder();
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
                            throw new ScriptException(FileName, substitution, Resources.MissingClosingBrace);

                        ch = mutableString[counter];

                        if (ch == ',' || ch == ':')
                        {
                            int j = counter + 1;
                            while (j < limit && mutableString[j] != '}') ++j;

                            if (j >= limit)
                                throw new ScriptException(FileName, substitution, Resources.MissingClosingBrace);

                            patternBuffer.Append(mutableString[counter..j]);
                            counter = j;
                        }
                        else if (ch != '}')
                            throw new ScriptException(FileName, substitution, Resources.MissingClosingBrace);
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
        /// Recognizes a qualified name.
        /// </summary>
        /// <param name="first">A <see cref="Token"/> marking the start of the qualified name</param>
        /// <param name="last">A <see cref="Token"/> marking the end of the qualified name</param>
        /// <returns>A <see cref="Ast.Expressions.QualifiedName"/></returns>
        protected QualifiedName QualifiedName(ref Token first, ref Token last)
        {
            var parts = new List<NamePart>();
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

            return new QualifiedName(parts.ToArray());
        }

        /// <summary>
        /// Recognizes a property's initializer.
        /// </summary>
        /// <returns>A <see cref="Ast.Expressions.PropertyInitializer"/></returns>
        protected PropertyInitializer PropertyInitializer()
        {
            if (!TryMatch(TokenID.Identifier)) return null;

            string fieldName = token.ToString();
            Token first = token;
            Consume(1);
            Match(TokenID.Equal);
            var fieldValue = RequiredExpression();
            
            var initializer = new PropertyInitializer(fieldName, fieldValue);
            initializer.SetLocation(first.Start, fieldValue.End);
            return initializer;
        }

        /// <summary>
        /// Recognizes a map item's initializer.
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
        /// <returns>A <see cref="Tuple{Expression[], Dictionary{string, Expression}}"/></returns>
        protected Tuple<Expression[], Dictionary<string, Expression>> FunctionArguments()
        {
            var positionalArgs = new List<Expression>();
            var namedArgs = new Dictionary<string, Expression>();
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
                    Expression arg = Expression();
                    
                    if (arg != null)
                    {
                        positionalArgs.Add(arg);

                        if (TryMatch(TokenID.Comma))
                            Consume(1);
                        else
                            section = -1;
                    }
                    else if (positionalArgs.Count > 0)
                        throw new ParseException(FileName, token, Resources.ExpressionRequired);
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
                    throw new ParseException(FileName, argName, ex);
                }

                if (TryMatch(TokenID.Comma))
                    Consume(1);
                else
                    section = -1;
            }

            return new Tuple<Expression[], Dictionary<string, Expression>>([.. positionalArgs], namedArgs);
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

                switch (first.TokenID)
                {
                    case TokenID.LT_Null:
                        pattern = new NullPattern();
                        Consume(1);
                        break;
                    case TokenID.LT_Boolean:
                    case TokenID.LT_Integer:
                    case TokenID.LT_Long:
                    case TokenID.LT_Float:
                    case TokenID.LT_Decimal:
                    case TokenID.LT_Date:
                    case TokenID.LT_String:
                        DataItem lBound = DataItemFactory.CreateDataItem(first.Value);
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

                            if (TryMatch(first.TokenID))
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
                        string typeName = first.ToString();
                        Consume(1);

                        if (typeName == AlwaysPattern.Symbol)
                            pattern = new AlwaysPattern();
                        else if (TryMatch(TokenID.LeftBrace))
                            pattern = new ObjectPattern(typeName, MatchCaseObjectPattern(ref last));
                        else
                            pattern = new TypePattern(typeName);

                        break;
                    case TokenID.Identifier:
                        if (LookAhead(TokenID.Colon, out pos))
                        {
                            string parameterName = first.ToString();
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
                    case TokenID.Plus:
                    case TokenID.Minus:
                        if (!LookAhead(t => t.IsNumeric, out pos)) throw new ParseException(FileName, first);

                        negative = first.TokenID == TokenID.Minus;
                        Consume(pos - 1);

                        goto case TokenID.LT_Boolean;
                    case TokenID.Comma:
                        if (patterns.Count > 0)
                            Consume(1);
                        else
                            throw new ParseException(FileName, first);
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

            var composite = new CompositePattern(patterns.ToArray());
            composite.SetLocation(patterns[0].Start, patterns[patterns.Count - 1].End);
            return composite;
        }

        /// <summary>
        /// Recognizes the <see cref="ObjectPattern.Example"/> member.
        /// </summary>
        /// <returns>An <see cref="Object"/> with literal property values</returns>
        protected DataItem MatchCaseObjectPattern(ref Token last)
        {
            var fieldBag = new Dictionary<string, DataItem>();
            bool loop = true;

            Match(TokenID.LeftBrace);

            while (loop)
            {
                string fieldName = Match(TokenID.Identifier).ToString();
                Match(TokenID.Equal);
                Token fieldValue = MatchAny(TokenID.LT_Integer, TokenID.LT_Long, TokenID.LT_Float,
                                            TokenID.LT_Decimal, TokenID.LT_Date, TokenID.LT_String);
                
                fieldBag.Add(fieldName, DataItemFactory.CreateDataItem(fieldValue.Value));

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
        /// The full featured parser overrides it to recognize a reacher syntax with blocks.
        /// </remarks>
        protected virtual Expression MatchCaseExpression()
        {
            Expression expr = RequiredExpression();
            Token last = Match(TokenID.SemiColon);
            expr.SetLocation(expr.Start, last.End);

            return expr;
        }
    }
}