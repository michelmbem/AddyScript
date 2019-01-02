#region 'using' Directives

using System;
using System.Collections.Generic;
using System.Numerics;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Properties;
using AddyScript.Runtime;
using AddyScript.Runtime.Dynamics;
using AddyScript.Runtime.NativeTypes;
using AstBlock = AddyScript.Ast.Statements.Block;
using Attribute = AddyScript.Runtime.Attribute;
using Boolean = AddyScript.Runtime.Dynamics.Boolean;
using Decimal = AddyScript.Runtime.Dynamics.Decimal;
using String = AddyScript.Runtime.Dynamics.String;
using Void = AddyScript.Runtime.Dynamics.Void;

#endregion

namespace AddyScript.Parsers
{
    /// <summary>
    /// The AddyScript's full-featured parser.
    /// </summary>
    public class Parser : ExpressionParser
    {
        /// <summary>
        /// Initializes a new instance of the parser.
        /// </summary>
        /// <param name="lexer">The bound lexer</param>
        public Parser(Lexer lexer) : base(lexer)
        {
        }

        /// <summary>
        /// Recognizes an entire script.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Program"/></returns>
        public Program Program()
        {
            Statement[] statements = Asterisk<Statement>(Statement);
            var labels = CurrentFunction.CurrentBlock.ConvertLabels(statements);
            Match(TokenID.EndOfFile);

            var program = new Program(FileName, statements) {Labels = labels};
            if (statements.Length > 0) program.SetLocation(statements[0].Start, statements[statements.Length - 1].End);

            return program;
        }

        /// <summary>
        /// Recognizes a statement.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.Statement"/></returns>
        protected Statement Statement()
        {
            while (true)
            {
                SkipComments();
                switch (token.TokenID)
                {
                    case TokenID.KW_Import:
                        return Import();
                    case TokenID.Modifier:
                    case TokenID.KW_Class:
                        return Class();
                    case TokenID.KW_Function:
                        return Function();
                    case TokenID.KW_Extern:
                        return ExternalFunction();
                    case TokenID.LeftBracket:
                        return StatementWithAttributes();
                    case TokenID.KW_Const:
                        return ConstantDecl();
                    case TokenID.KW_Var:
                        return VariableDecl();
                    case TokenID.LeftBrace:
                        return Block();
                    case TokenID.KW_If:
                        return IfThenElse();
                    case TokenID.KW_Switch:
                        return SwitchBlock();
                    case TokenID.KW_For:
                        return ForLoop();
                    case TokenID.KW_ForEach:
                        return ForEachLoop();
                    case TokenID.KW_While:
                        return WhileLoop();
                    case TokenID.KW_Do:
                        return DoLoop();
                    case TokenID.KW_Continue:
                        return Continue();
                    case TokenID.KW_Break:
                        return Break();
                    case TokenID.KW_Goto:
                        return Goto();
                    case TokenID.KW_Return:
                        return Return();
                    case TokenID.KW_Throw:
                        return Throw();
                    case TokenID.KW_Try:
                        return TryCatchFinally();
                    case TokenID.Identifier:
                        if (GotLabel()) break;
                        goto case TokenID.LT_Null;
                    case TokenID.LT_Null:
                    case TokenID.LT_Boolean:
                    case TokenID.LT_Integer:
                    case TokenID.LT_Long:
                    case TokenID.LT_Float:
                    case TokenID.LT_Decimal:
                    case TokenID.LT_Date:
                    case TokenID.LT_String:
                    case TokenID.Plus:
                    case TokenID.DoublePlus:
                    case TokenID.Minus:
                    case TokenID.DoubleMinus:
                    case TokenID.Exclamation:
                    case TokenID.Tilda:
                    case TokenID.LeftParenthesis:
                    case TokenID.VerticalBar:
                    case TokenID.KW_New:
                    case TokenID.KW_This:
                    case TokenID.KW_Super:
                    case TokenID.KW_TypeOf:
                    case TokenID.TypeName:
                        Expression expr = Expression();
                        Token last = Match(TokenID.SemiColon);
                        expr.SetLocation(expr.Start, last.End);
                        return expr;
                    case TokenID.SemiColon:
                        var empty = new Statement();
                        empty.CopyLocation(token);
                        Consume(1);
                        return empty;
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Recognizes a non-null statement.
        /// </summary>
        /// <returns>An <see cref="AddyScript.Ast.Statements.Statement"/></returns>
        public Statement RequiredStatement()
        {
            return Required<Statement>(Statement, string.Format(Resources.UnexpectedToken, token));
        }

        /// <summary>
        /// Recognizes a statement decorated with some attributes.
        /// </summary>
        /// <remarks>
        /// Most statements don't make usage of attributes.
        /// This is just a helpful way to attach additionnal informations to a statement.
        /// </remarks>
        /// <returns>An <see cref="AddyScript.Ast.Statements.StatementWithAttributes"/></returns>
        protected StatementWithAttributes StatementWithAttributes()
        {
            Token bookmark = Match(TokenID.LeftBracket);
            Attribute[] attributes = List<Attribute>(Attribute, true, Resources.DuplicatedAttribute);
            Match(TokenID.RightBracket);

            Statement statement = Statement();
            if (statement is StatementWithAttributes)
            {
                var stmtWA = (StatementWithAttributes)statement;
                stmtWA.Attributes = attributes;
                stmtWA.SetLocation(bookmark.Start, stmtWA.End);
                return stmtWA;
            }

            throw new ScriptException(FileName, statement, Resources.AttributesNotSupported);
        }

        /// <summary>
        /// Recognizes an import directive.
        /// </summary>
        /// <returns>A reference to <see cref="ImportDirective"/></returns>
        protected ImportDirective Import()
        {
            Token first = Match(TokenID.KW_Import), last = first;
            QualifiedName moduleName = QualifiedName(ref first, ref last);

            string alias = null;
            if (TryMatch(TokenID.KW_As))
            {
                Consume(1);
                alias = Match(TokenID.Identifier).ToString();
            }

            last = Match(TokenID.SemiColon);

            var import = new ImportDirective(moduleName, alias);
            import.SetLocation(first.Start, last.End);
            return import;
        }

        /// <summary>
        /// Recognizes a class definition.
        /// </summary>
        /// <returns>A <see cref="ClassDefinition"/></returns>
        protected ClassDefinition Class()
        {
            Token first = null;

            Modifier classModifier = Modifier.Default;
            if (TryMatch(TokenID.Modifier))
            {
                first = token;
                classModifier = (Modifier) first.Value;
                Consume(1);
            }

            Token maybeFirst = Match(TokenID.KW_Class);
            first = first ?? maybeFirst;
            string className = Match(TokenID.Identifier).ToString();

            string superClassName = null;
            if (TryMatch(TokenID.Colon))
            {
                if (classModifier == Modifier.Static)
                    throw new ParseException(FileName, token, Resources.StaticClassHasNoSuperClass);

                Consume(1);
                superClassName = Match(TokenID.Identifier).ToString();
            }

            Match(TokenID.LeftBrace);
            PushClass(classModifier, className, superClassName);
            ClassMember[] members = Asterisk<ClassMember>(Member);

            ClassMethod constructor = null;
            var fields = new List<ClassField>();
            var properties= new List<ClassProperty>();
            var methods = new List<ClassMethod>();
            var events = new List<ClassEvent>();
            
            foreach (ClassMember member in members)
            {
                foreach (ClassMember other in members)
                    if (member != other && member.Name == other.Name)
                        throw new ScriptException(FileName, member, string.Format(Resources.MemberNameConfict, member.Name));

                if (classModifier == Modifier.Static && member.Modifier != Modifier.Static && member.Modifier != Modifier.StaticFinal)
                    throw new ScriptException(FileName, member, Resources.StaticClassMember);

                if (classModifier != Modifier.Abstract && member.Modifier == Modifier.Abstract)
                    throw new ScriptException(FileName, member, Resources.AbstractMethodInNonAbstractClass);

                if (member is ClassField)
                {
                    switch (member.Modifier)
                    {
                        case Modifier.Abstract:
                            throw new ScriptException(FileName, member, string.Format(Resources.InvalidFieldModifier, member.Modifier));
                        case Modifier.StaticFinal:
                            if (((ClassField)member).Initializer == null)
                                throw new ScriptException(FileName, member, Resources.ConstantFieldShouldBeInitialized);
                            break;
                    }

                    fields.Add((ClassField)member);
                }
                else
                {
                    if (member.Modifier == Modifier.StaticFinal)
                        throw new ScriptException(FileName, member, Resources.SpecificFieldModifier);

                    if (member is ClassProperty)
                        properties.Add((ClassProperty)member);
                    else if (member is ClassMethod)
                    {
                        if (member.Name == CurrentClass.Name)
                        {
                            if (constructor != null)
                                throw new ScriptException(FileName, member, Resources.SingleConstructor);

                            constructor = (ClassMethod)member;
                        }
                        else
                            methods.Add((ClassMethod)member);
                    }
                    else if (member is ClassEvent)
                    {
                        if (member.Modifier != Modifier.Default && member.Modifier != Modifier.Static)
                            throw new ScriptException(FileName, member, string.Format(Resources.InvalidFieldModifier, member.Modifier));

                        events.Add((ClassEvent)member);
                    }
                }
            }

            Token last = Match(TokenID.RightBrace);
            PopClass();

            var classDef = new ClassDefinition(className, superClassName, classModifier,
                                               constructor, fields.ToArray(),
                                               properties.ToArray(), methods.ToArray(),
                                               events.ToArray());
            classDef.SetLocation(first.Start, last.End);
            return classDef;
        }

        /// <summary>
        /// Recognizes a function's declaration.
        /// </summary>
        /// <returns>A <see cref="FunctionDecl"/></returns>
        protected FunctionDecl Function()
        {
            Token first = Match(TokenID.KW_Function);
            string name = Match(TokenID.Identifier).ToString();
            Parameter[] parameters = ParameterList();

            PushFunction(name, false, false, false);
            Block block = Block();
            PopFunction();
            block.Append(new Return());

            var fnDecl = new FunctionDecl(name, new Function(parameters, block));
            fnDecl.SetLocation(first.Start, block.End);
            return fnDecl;
        }

        /// <summary>
        /// Recognizes an inline function's declaration.
        /// </summary>
        /// <returns>An <see cref="AddyScript.Ast.Expressions.InlineFunction"/></returns>
        protected override InlineFunction InlineFunction()
        {
            Token first = Match(TokenID.KW_Function);
            Parameter[] parameters = ParameterList();

            PushFunction(null, false, false, false);
            Block block = Block();
            PopFunction();
            block.Append(new Return());

            var inlineFn = new InlineFunction(new Function(parameters, block));
            inlineFn.SetLocation(first.Start, block.End);
            return inlineFn;
        }

        /// <summary>
        /// Recognizes a lambda expression or a lambda statement.
        /// </summary>
        /// <returns>An <see cref="InlineFunction"/></returns>
        protected override InlineFunction Lambda()
        {
            var parameters = new List<Parameter>();

            Token first = Match(TokenID.VerticalBar);
            while (TryMatch(TokenID.Identifier))
            {
                parameters.Add(new Parameter(token.Value.ToString()));
                Consume(1);
                if (TryMatch(TokenID.Comma)) Consume(1);
            }
            Match(TokenID.VerticalBar);

            Match(TokenID.Arrow);

            Function function;
            if (TryMatch(TokenID.LeftBrace))
            {
                var body = Block();
                function = new Function(parameters.ToArray(), body);
            }
            else
            {
                var returned = Required<Expression>(Expression, Resources.ExpressionRequired);
                function = new Function(parameters.ToArray(), AstBlock.Return(returned));
            }

            var lambda = new InlineFunction(function);
            lambda.SetLocation(first.Start, function.Body.End);
            return lambda;
        }

        /// <summary>
        /// Recognizes an external function's declaration.
        /// </summary>
        /// <returns>An <see cref="ExternalFunctionDecl"/></returns>
        protected ExternalFunctionDecl ExternalFunction()
        {
            Token first = Match(TokenID.KW_Extern);
            Match(TokenID.KW_Function);
            string name = Match(TokenID.Identifier).ToString();
            Parameter[] parameters = ParameterList();
            Token last = Match(TokenID.SemiColon);

            var efd = new ExternalFunctionDecl(name, parameters);
            efd.SetLocation(first.Start, last.End);
            return efd;
        }

        /// <summary>
        /// Recognizes a constant's declaration.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.ConstantDecl"/></returns>
        protected ConstantDecl ConstantDecl()
        {
            Token first = Match(TokenID.KW_Const);
            var initializers = List<PropertyInitializer>(
                PropertyInitializer, true, Resources.DuplicatedConstant);
            Token last = Match(TokenID.SemiColon);

            var constDecl = new ConstantDecl(initializers);
            constDecl.SetLocation(first.Start, last.Start);
            return constDecl;
        }

        /// <summary>
        /// Recognizes a variable's declaration.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.VariableDecl"/></returns>
        protected VariableDecl VariableDecl()
        {
            var initializers = new List<PropertyInitializer>();
            Token first = Match(TokenID.KW_Var);

            while (TryMatch(TokenID.Identifier))
            {
                Token bookmark = token;
                string varName = bookmark.ToString();
                Consume(1);

                Expression varValue = null;
                if (TryMatch(TokenID.Equal))
                {
                    Consume(1);
                    varValue = Expression();
                }

                var initializer = new PropertyInitializer(varName, varValue);
                initializer.SetLocation(bookmark.Start, varValue != null ? varValue.End : bookmark.End);
                initializers.Add(initializer);

                if (TryMatch(TokenID.Comma)) Consume(1);
            }

            Token last = Match(TokenID.SemiColon);

            var varDecl = new VariableDecl(initializers.ToArray());
            varDecl.SetLocation(first.Start, last.Start);
            return varDecl;
        }

        /// <summary>
        /// Recognizes a block of statements.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.Block"/></returns>
        protected Block Block()
        {
            Token first = Match(TokenID.LeftBrace);
            CurrentFunction.PushBlock();
            Statement[] stmts = Asterisk<Statement>(Statement);
            var labels = CurrentFunction.CurrentBlock.ConvertLabels(stmts);
            CurrentFunction.PopBlock();
            Token last = Match(TokenID.RightBrace);

            var block = new Block(stmts) { Labels = labels };
            block.SetLocation(first.Start, last.End);
            return block;
        }

        /// <summary>
        /// Recognizes an if-then-else statement.
        /// </summary>
        /// <returns>An <see cref="AddyScript.Ast.Statements.IfThenElse"/></returns>
        protected IfThenElse IfThenElse()
        {
            Token first = Match(TokenID.KW_If);
            Match(TokenID.LeftParenthesis);
            var cond = Required<Expression>(Expression, Resources.ExpressionRequired);
            Match(TokenID.RightParenthesis);
            var ifBlock = Required<Statement>(Statement, Resources.SimpleStatementRequired);
            Statement elseBlock = null;

            if (TryMatch(TokenID.KW_Else))
            {
                Consume(1);
                elseBlock = Required<Statement>(Statement, Resources.SimpleStatementRequired);
            }

            var ifThenElse = new IfThenElse(cond, ifBlock, elseBlock);
            ifThenElse.SetLocation(first.Start, (elseBlock ?? ifBlock).End);
            return ifThenElse;
        }

        /// <summary>
        /// Recognizes an switch block.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.SwitchBlock"/></returns>
        protected SwitchBlock SwitchBlock()
        {
            Token first = Match(TokenID.KW_Switch), last = first;
            Match(TokenID.LeftParenthesis);
            var cond = Required<Expression>(Expression, Resources.ExpressionRequired);
            Match(TokenID.RightParenthesis);
            Match(TokenID.LeftBrace);
            CurrentFunction.PushBlock();

            var cases = new List<CaseLabel>();
            var stmtList = new List<Statement>();
            Statement[] stmts = null;
            Dictionary<string, Label> labels = null;
            int address = 0;
            int defCase = int.MaxValue;
            bool loop = true;

            ++CurrentFunction.SwitchBlocks;
            do
            {
                SkipComments();
                switch (token.TokenID)
                {
                    case TokenID.KW_Case:
                        {
                            if (defCase < int.MaxValue)
                                throw new ParseException(FileName, token, Resources.NoCaseAfterDefault);

                            CaseLabel caseLabel = CaseLabel(address);
                            if (cases.Contains(caseLabel))
                                throw new ScriptException(FileName, caseLabel, Resources.DuplicatedCaseLabel);

                            cases.Add(caseLabel);
                        }
                        break;
                    case TokenID.KW_Default:
                        if (defCase < int.MaxValue)
                            throw new ParseException(FileName, token, Resources.DuplicatedCaseLabel);

                        Consume(1);
                        Match(TokenID.Colon);
                        defCase = address;
                        break;
                    case TokenID.RightBrace:
                        last = token;
                        Consume(1);
                        stmts = stmtList.ToArray();
                        labels = CurrentFunction.CurrentBlock.ConvertLabels(stmts);
                        CurrentFunction.PopBlock();
                        loop = false;
                        break;
                    case TokenID.Identifier:
                        if (GotLabel()) break;
                        goto default;
                    default:
                        stmtList.Add(Required<Statement>(Statement, Resources.SimpleStatementRequired));
                        ++address;
                        break;
                }
            } while (loop);
            --CurrentFunction.SwitchBlocks;

            if (cases.Count == 0 && defCase == int.MaxValue)
                throw new ParseException(FileName, first, Resources.CaseLabelRequired);

            var switchBlock = new SwitchBlock(cond, cases.ToArray(), defCase, stmts) { Labels = labels };
            switchBlock.SetLocation(first.Start, last.End);
            return switchBlock;
        }

        /// <summary>
        /// Recognizes a for loop.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.ForLoop"/></returns>
        protected ForLoop ForLoop()
        {
            Token first = Match(TokenID.KW_For);
            Match(TokenID.LeftParenthesis);
            
            Statement[] initializers;
            if (TryMatch(TokenID.KW_Var))
                initializers = new Statement[] { VariableDecl() };
            else
            {
                initializers = List<Expression>(Expression, false, null);
                Match(TokenID.SemiColon);
            }

            Expression guard = Expression();
            Match(TokenID.SemiColon);
            Expression[] updaters = List<Expression>(Expression, false, null);
            Match(TokenID.RightParenthesis);

            ++CurrentFunction.Loops;
            var body = Required<Statement>(Statement, Resources.SimpleStatementRequired);
            --CurrentFunction.Loops;

            var forLoop = new ForLoop(initializers, guard, updaters, body);
            forLoop.SetLocation(first.Start, body.End);
            return forLoop;
        }

        /// <summary>
        /// Recognizes a for-each loop.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.ForEachLoop"/></returns>
        protected ForEachLoop ForEachLoop()
        {
            Token first = Match(TokenID.KW_ForEach);
            Match(TokenID.LeftParenthesis);
            string keyName = Ast.Statements.ForEachLoop.DEFAULT_KEY_NAME;
            string valueName = Match(TokenID.Identifier).ToString();

            if (TryMatch(TokenID.Arrow))
            {
                Consume(1);
                keyName = valueName;
                valueName = Match(TokenID.Identifier).ToString();
            }

            Match(TokenID.KW_In);
            var enumerated = Required<Expression>(Expression, Resources.ExpressionRequired);
            Match(TokenID.RightParenthesis);

            ++CurrentFunction.Loops;
            var body = Required<Statement>(Statement, Resources.SimpleStatementRequired);
            --CurrentFunction.Loops;

            var forEach = new ForEachLoop(keyName, valueName, enumerated, body);
            forEach.SetLocation(first.Start, body.End);
            return forEach;
        }

        /// <summary>
        /// Recognizes a while loop.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.WhileLoop"/></returns>
        protected WhileLoop WhileLoop()
        {
            Token first = Match(TokenID.KW_While);
            Match(TokenID.LeftParenthesis);
            var guard = Required<Expression>(Expression, Resources.ExpressionRequired);
            Match(TokenID.RightParenthesis);

            ++CurrentFunction.Loops;
            var body = Required<Statement>(Statement, Resources.SimpleStatementRequired);
            --CurrentFunction.Loops;

            var whileLoop = new WhileLoop(guard, body);
            whileLoop.SetLocation(first.Start, body.End);
            return whileLoop;
        }

        /// <summary>
        /// Recognizes a do-while loop.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.DoLoop"/></returns>
        protected DoLoop DoLoop()
        {
            Token first = Match(TokenID.KW_Do);

            ++CurrentFunction.Loops;
            var body = Required<Statement>(Statement, Resources.SimpleStatementRequired);
            --CurrentFunction.Loops;

            Match(TokenID.KW_While);
            Match(TokenID.LeftParenthesis);
            var guard = Required<Expression>(Expression, Resources.ExpressionRequired);
            Token last = Match(TokenID.RightParenthesis);

            var doLoop = new DoLoop(guard, body);
            doLoop.SetLocation(first.Start, last.End);
            return doLoop;
        }

        /// <summary>
        /// Recognizes a continue statement.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.Continue"/></returns>
        protected Continue Continue()
        {
            Token first = Match(TokenID.KW_Continue);
            if (CurrentFunction.Loops <= 0)
                throw new ParseException(FileName, first, Resources.NoContinueOutOfLoop);

            Token last = Match(TokenID.SemiColon);

            var _continue = new Continue();
            _continue.SetLocation(first.Start, last.End);
            return _continue;
        }

        /// <summary>
        /// Recognizes a break statement.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.Break"/></returns>
        protected Break Break()
        {
            Token first = Match(TokenID.KW_Break);
            if (CurrentFunction.Loops <= 0 && CurrentFunction.SwitchBlocks <= 0)
                throw new ParseException(FileName, first, Resources.NoBreakOutOfLoop);

            Token last = Match(TokenID.SemiColon);

            var _break = new Break();
            _break.SetLocation(first.Start, last.End);
            return _break;
        }

        /// <summary>
        /// Recognizes a goto statement.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.Goto"/></returns>
        protected Goto Goto()
        {
            Token first = Match(TokenID.KW_Goto);
            string labelName = Match(TokenID.Identifier).ToString();
            Token last = Match(TokenID.SemiColon);

            var _goto = new Goto(labelName);
            _goto.SetLocation(first.Start, last.End);
            return _goto;
        }

        /// <summary>
        /// Recognizes a return statement.
        /// </summary>
        /// <returns><see cref="Return"/></returns>
        protected Return Return()
        {
            Token first = Match(TokenID.KW_Return);
            if (CurrentFunction.FinallyBlocks > 0)
                throw new ParseException(FileName, first, Resources.CannotReturnFromFinallyBlock);

            Expression expr = Expression();
            if (expr != null)
            {
                if (CurrentFunction.IsMain)
                    throw new ScriptException(FileName, expr, Resources.ScriptCannotReturnValue);

                if (CurrentFunction.IsContructor)
                    throw new ScriptException(FileName, expr, Resources.ConstructorCantReturnValue);
            }

            Token last = Match(TokenID.SemiColon);

            var _return = new Return(expr);
            _return.SetLocation(first.Start, last.Start);
            return _return;
        }

        /// <summary>
        /// Recognizes a throw statement.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.Throw"/></returns>
        private Throw Throw()
        {
            Token first = Match(TokenID.KW_Throw);
            var expr = Required<Expression>(Expression, Resources.ExpressionRequired);
            Token last = Match(TokenID.SemiColon);

            var _throw = new Throw(expr);
            _throw.SetLocation(first.Start, last.End);
            return _throw;
        }

        /// <summary>
        /// Recognizes a try-catch-finally statement.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Ast.Statements.TryCatchFinally"/></returns>
        protected TryCatchFinally TryCatchFinally()
        {
            Token first = Match(TokenID.KW_Try);
            Block tryBlock = Block();

            Match(TokenID.KW_Catch);
            Match(TokenID.LeftParenthesis);
            string exception = Match(TokenID.Identifier).ToString();
            Match(TokenID.RightParenthesis);
            Block catchBlock = Block();

            Block finallyBlock = null;
            if (TryMatch(TokenID.KW_Finally))
            {
                Consume(1);
                ++CurrentFunction.FinallyBlocks;
                finallyBlock = Block();
                --CurrentFunction.FinallyBlocks;
            }

            var tcf = new TryCatchFinally(tryBlock, exception, catchBlock, finallyBlock);
            tcf.SetLocation(first.Start, (finallyBlock ?? catchBlock).End);
            return tcf;
        }

        /// <summary>
        /// Recognizes the definition of any class member.
        /// </summary>
        /// <returns>A <see cref="ClassMember"/></returns>
        protected ClassMember Member()
        {
            ClassMember member = null;
            Scope scope = Scope.Private;
            Modifier modifier = Modifier.Default;
            Attribute[] attributes = null;
            Token realFirst = null;
            bool gotScope = false, gotModifier = false, gotAttribute = false;
            bool loop = true;

            while (loop)
            {
                bool consume = true;
                SkipComments();

                switch (token.TokenID)
                {
                    case TokenID.Scope:
                        if (gotScope) throw new ParseException(FileName, token);
                        scope = (Scope) token.Value;
                        realFirst = token;
                        gotScope = true;
                        break;
                    case TokenID.Modifier:
                        if (gotModifier)
                            switch (modifier)
                            {
                                case Modifier.Static:
                                    if (token.Value.Equals(Modifier.Final))
                                        modifier = Modifier.StaticFinal;
                                    else
                                        throw new ParseException(FileName, token);
                                    break;
                                case Modifier.Final:
                                    if (token.Value.Equals(Modifier.Static))
                                        modifier = Modifier.StaticFinal;
                                    else
                                        throw new ParseException(FileName, token);
                                    break;
                                default:
                                    throw new ParseException(FileName, token);
                            }
                        else
                        {
                            modifier = (Modifier)token.Value;
                            realFirst = token;
                            gotModifier = true;
                        }
                        break;
                    case TokenID.LeftBracket:
                        if (gotAttribute) throw new ParseException(FileName, token);
                        Consume(1);
                        attributes = List<Attribute>(Attribute, true, Resources.DuplicatedAttribute);
                        Match(TokenID.RightBracket);
                        consume = false;
                        gotAttribute = true;
                        break;
                    case TokenID.Identifier:
                        member = Field(scope, modifier);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Constructor:
                        if (modifier != Modifier.Default)
                            throw new ParseException(FileName, token, Resources.InvalidConstructorModifier);
                        member = Constructor(scope);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Property:
                        member = Property(scope, modifier);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Function:
                        member = Method(scope, modifier);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Operator:
                        if (modifier != Modifier.Default)
                            throw new ParseException(FileName, token, Resources.InvalidOperatorModifier);
                        member = Operator(scope);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Event:
                        member = Event(scope, modifier);
                        consume = false;
                        loop = false;
                        break;
                    default:
                        consume = false;
                        loop = false;
                        break;
                }

                if (consume) Consume(1);
            }

            if (member != null && realFirst != null)
            {
                member.Attributes = attributes;
                member.SetLocation(realFirst.Start, member.End);
            }

            return member;
        }

        /// <summary>
        /// Recognizes the definition of a constructor.
        /// </summary>
        /// <param name="scope">The scope of this constructor</param>
        /// <returns>A <see cref="AddyScript.Runtime.ClassMethod"/></returns>
        protected ClassMethod Constructor(Scope scope)
        {
            Token first = Match(TokenID.KW_Constructor);
            Parameter[] parameters = ParameterList();

            ParentConstructorCall superCall = null;
            if (TryMatch(TokenID.Colon))
            {
                Consume(1);
                Token superStart = Match(TokenID.KW_Super);
                Match(TokenID.LeftParenthesis);
                Expression[] superArgs = List<Expression>(Expression, false, null);
                Token superEnd = Match(TokenID.RightParenthesis);

                superCall = new ParentConstructorCall(superArgs);
                superCall.SetLocation(superStart.Start, superEnd.End);
            }

            PushFunction(CurrentClass.Name, true, true, false);
            Block block = Block();
            PopFunction();
            block.Append(new Return());
            if (superCall != null) block.Insert(0, superCall);

            var constructor = new ClassMethod(CurrentClass.Name, scope, Modifier.Default, new Function(parameters, block));
            constructor.SetLocation(first.Start, block.End);
            return constructor;
        }

        /// <summary>
        /// Recognizes the definition of a field.
        /// </summary>
        /// <param name="scope">The scope of the field</param>
        /// <param name="modifier">The modifier of the field</param>
        /// <returns>A <see cref="AddyScript.Runtime.ClassField"/></returns>
        protected ClassField Field(Scope scope, Modifier modifier)
        {
            Token first = Match(TokenID.Identifier);
            string name = first.ToString();

            Expression initialValue = null;
            if (TryMatch(TokenID.Equal))
            {
                Consume(1);
                initialValue = Required<Expression>(Expression, Resources.ExpressionRequired);
            }

            Token last = Match(TokenID.SemiColon);

            var field = new ClassField(name, scope, modifier, initialValue);
            field.SetLocation(first.Start, last.End);
            return field;
        }

        /// <summary>
        /// Recognizes the definition of a property.
        /// </summary>
        /// <param name="scope">The scope of the property</param>
        /// <param name="modifier">The modifier of the property</param>
        /// <returns>A <see cref="AddyScript.Runtime.ClassField"/></returns>
        protected ClassProperty Property(Scope scope, Modifier modifier)
        {
            Token first = Match(TokenID.KW_Property);
            string name = Match(TokenID.Identifier).ToString();
            Match(TokenID.LeftBrace);

            ClassProperty property;
            PropertyAccess access = PropertyAccess.None;
            ClassMethod reader = null, writer = null;
            Block readBlock = null, writeBlock = null;
            Scope readerScope = scope, writerScope = scope;
            bool gotRead = false, gotWrite = false, isAuto = false;
            bool loop = true;

            while (loop)
            {
                Scope accessorScope = scope;
                if (TryMatch(TokenID.Scope))
                {
                    accessorScope = (Scope) token.Value;
                    if (accessorScope >= scope)
                        throw new ParseException(FileName, token, Resources.AccessorScopeMustBeMoreRestrictive);
                    Consume(1);
                }

                if (TryMatch(TokenID.Identifier))
                {
                    string word = token.ToString();
                    switch (word)
                    {
                        case "read":
                            if (gotRead) throw new ParseException(FileName, token, Resources.DuplicatedReadAccessor);
                            Consume(1);

                            if ((modifier == Modifier.Abstract) || (gotWrite && isAuto))
                                Match(TokenID.SemiColon);
                            else if (!gotWrite && TryMatch(TokenID.SemiColon))
                            {
                                Consume(1);
                                isAuto = true;
                            }
                            else
                            {
                                PushFunction(ClassProperty.GetReaderName(name), true, false, modifier == Modifier.Static);
                                readBlock = Block();
                                readBlock.Append(new Return());
                                PopFunction();
                            }

                            gotRead = true;
                            access |= PropertyAccess.Read;
                            readerScope = accessorScope;
                            break;
                        case "write":
                            if (gotWrite) throw new ParseException(FileName, token, Resources.DuplicatedWriteAccessor);
                            Consume(1);

                            if ((modifier == Modifier.Abstract) || (gotRead && isAuto))
                                Match(TokenID.SemiColon);
                            else if (!gotRead && TryMatch(TokenID.SemiColon))
                            {
                                Consume(1);
                                isAuto = true;
                            }
                            else
                            {
                                PushFunction(ClassProperty.GetWriterName(name), true, false, modifier == Modifier.Static);
                                writeBlock = Block();
                                writeBlock.Append(new Return());
                                PopFunction();
                            }

                            gotWrite = true;
                            access |= PropertyAccess.Write;
                            writerScope = accessorScope;
                            break;
                        default:
                            loop = false;
                            break;
                    }
                }
                else
                    loop = false;
            }

            Token last = Match(TokenID.RightBrace);

            if (access == PropertyAccess.None)
                throw new ParseException(FileName, first, Resources.NoEmptyProperty);

            if (readerScope != scope && writerScope != scope)
                throw new ParseException(FileName, first, Resources.InvalidAccessorsScope);

            if ((modifier == Modifier.Abstract) || isAuto)
                property = new ClassProperty(name, scope, modifier, access, readerScope, writerScope);
            else
            {
                if (readBlock != null)
                {
                    var readFunc = new Function(Runtime.Parameter.EmptyArray, readBlock);
                    reader = new ClassMethod(ClassProperty.GetReaderName(name), readerScope, modifier, readFunc);
                    reader.CopyLocation(readBlock);
                }

                if (writeBlock != null)
                {
                    var writeFunc = new Function(new[] {new Parameter(ClassProperty.WRITER_PARAMETER_NAME)}, writeBlock);
                    writer = new ClassMethod(ClassProperty.GetWriterName(name), writerScope, modifier, writeFunc);
                    writer.CopyLocation(writeBlock);
                }

                property = new ClassProperty(name, scope, modifier, reader, writer);
            }

            property.SetLocation(first.Start, last.End);
            return property;
        }

        /// <summary>
        /// Recognizes the definition of a method.
        /// </summary>
        /// <param name="scope">The scope of the method</param>
        /// <param name="modifier">The modifier of the method</param>
        /// <returns>A <see cref="AddyScript.Runtime.ClassMethod"/></returns>
        protected ClassMethod Method(Scope scope, Modifier modifier)
        {
            Token first = Match(TokenID.KW_Function);
            string name = Match(TokenID.Identifier).ToString();
            Parameter[] parameters = ParameterList();
            ScriptLocation end;
            Block block = null;

            if (modifier == Modifier.Abstract)
                end = Match(TokenID.SemiColon).End;
            else
            {
                PushFunction(name, true, false, modifier == Modifier.Static);
                block = Block();
                PopFunction();
                block.Append(new Return());
                end = block.End;
            }

            var method = new ClassMethod(name, scope, modifier, new Function(parameters, block));
            method.SetLocation(first.Start, end);
            return method;
        }

        /// <summary>
        /// Recognizes an operator overloading.
        /// </summary>
        /// <param name="scope">The scope of the outcoming method</param>
        /// <returns>A <see cref="AddyScript.Runtime.ClassMethod"/></returns>
        protected ClassMethod Operator(Scope scope)
        {
            Token first = Match(TokenID.KW_Operator);
            Token _operator = OverloadableOperator();
            
            Parameter[] parameters = ParameterList();
            if (!IsValidOperandCount(_operator.TokenID, parameters.Length))
                throw new ParseException(FileName, first, string.Format(Resources.InvalidOperandCount, _operator));

            string name = parameters.Length == 0
                        ? ClassMethod.GetMethodName(_operator.ToUnaryOperator())
                        : ClassMethod.GetMethodName(_operator.ToBinaryOperator());

            PushFunction(name, true, false, false);
            Block block = Block();
            PopFunction();
            block.Append(new Return());

            var method = new ClassMethod(name, scope, Modifier.Default, new Function(parameters, block));
            method.SetLocation(first.Start, block.End);
            return method;
        }

        /// <summary>
        /// Recognizes the definition of an event.
        /// </summary>
        /// <param name="scope">The scope of the event</param>
        /// <param name="modifier">The modifier of the event</param>
        /// <returns>A <see cref="AddyScript.Runtime.ClassEvent"/></returns>
        protected ClassEvent Event(Scope scope, Modifier modifier)
        {
            Token first = Match(TokenID.KW_Event);
            string name = Match(TokenID.Identifier).ToString();
            Parameter[] parameters = ParameterList();
            ScriptLocation end = Match(TokenID.SemiColon).End;

            var _event = new ClassEvent(name, scope, modifier, parameters);
            _event.SetLocation(first.Start, end);
            return _event;
        }

        /// <summary>
        /// Recognizes a list of function's parameters.
        /// </summary>
        /// <returns>An array of <see cref="AddyScript.Runtime.Parameter"/>s</returns>
        protected Parameter[] ParameterList()
        {
            Match(TokenID.LeftParenthesis);
            Parameter[] parameters = List<Parameter>(
                Parameter, true, Resources.DuplicatedParameter);
            Match(TokenID.RightParenthesis);

            for (int i = 0; i < parameters.Length - 1; ++i)
            {
                if (parameters[i].VaArgs)
                    throw new ScriptException(FileName, parameters[i], Resources.VaArgsMustBeTheLast);

                if (parameters[i].DefaultValue != null)
                    for (int j = i + 1; j < parameters.Length; ++j)
                        if (!parameters[j].VaArgs && parameters[j].DefaultValue == null)
                            throw new ScriptException(FileName, parameters[j], Resources.MandatoryParamsPrecede);
            }

            return parameters;
        }

        /// <summary>
        /// Recognizes a function's parameter.
        /// </summary>
        /// <returns>A <see cref="AddyScript.Runtime.Parameter"/></returns>
        protected Parameter Parameter()
        {
            string name = null, context = "initial";
            bool byRef = false, vaArgs = false;
            Dynamic defaultValue = null;
            Attribute[] attributes = null;
            Token first = token, last = first;

            do
            {
                bool consume = true;
                SkipComments();

                switch (context)
                {
                    case "initial":
                        first = token;
                        switch (first.TokenID)
                        {
                            case TokenID.KW_Ref:
                                byRef = true;
                                context = "prefix";
                                break;
                            case TokenID.KW_Params:
                                vaArgs = true;
                                context = "prefix";
                                break;
                            case TokenID.Identifier:
                                name = token.ToString();
                                context = "name";
                                break;
                            case TokenID.LeftBracket:
                                Consume(1);
                                attributes = List<Attribute>(
                                    Attribute, true, Resources.DuplicatedAttribute);
                                Match(TokenID.RightBracket);
                                consume = false;
                                context = "attribute";
                                break;
                            default:
                                consume = false;
                                context = "done";
                                break;
                        }
                        break;
                    case "attribute":
                        switch (token.TokenID)
                        {
                            case TokenID.KW_Ref:
                                byRef = true;
                                context = "prefix";
                                break;
                            case TokenID.KW_Params:
                                vaArgs = true;
                                context = "prefix";
                                break;
                            case TokenID.Identifier:
                                name = token.ToString();
                                context = "name";
                                break;
                            default:
                                throw new ParseException(FileName, token);
                        }
                        break;
                    case "prefix":
                        switch (token.TokenID)
                        {
                            case TokenID.Identifier:
                                name = token.ToString();
                                context = "done";
                                break;
                            default:
                                throw new ParseException(FileName, token);
                        }
                        break;
                    case "name":
                        switch (token.TokenID)
                        {
                            case TokenID.Equal:
                                context = "assign";
                                break;
                            default:
                                consume = false;
                                context = "done";
                                break;
                        }
                        break;
                    case "assign":
                        switch (token.TokenID)
                        {
                            case TokenID.LT_Null:
                                defaultValue = Void.Value;
                                context = "done";
                                break;
                            case TokenID.LT_Boolean:
                                defaultValue = Boolean.FromBool((bool) token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Integer:
                                defaultValue = new Integer((int) token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Long:
                                defaultValue = new Long((BigInteger) token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Float:
                                defaultValue = new Float((double) token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Decimal:
                                defaultValue = new Decimal((BigDecimal) token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Date:
                                defaultValue = new Date((DateTime) token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_String:
                                defaultValue = new String((string) token.Value);
                                context = "done";
                                break;
                            default:
                                throw new ParseException(FileName, token, Resources.LiteralRequired);
                        }
                        break;
                }

                if (consume)
                {
                    last = token;
                    Consume(1);
                }
            } while (context != "done");

            if (name == null) return null;

            var param = new Parameter(name, byRef, vaArgs, defaultValue) { Attributes = attributes };
            param.SetLocation(first.Start, last.End);
            return param;
        }

        /// <summary>
        /// Expects the next token to be an overloadable operator.
        /// </summary>
        /// <returns>A token</returns>
        protected Token OverloadableOperator()
        {
            if (TryMatchAny(TokenID.Plus, TokenID.Minus, TokenID.DoublePlus,
                            TokenID.DoubleMinus, TokenID.Tilda, TokenID.Asterisk,
                            TokenID.Slash, TokenID.Percent, TokenID.DoubleAsterisk,
                            TokenID.DoubleLessThan, TokenID.DoubleGreaterThan,
                            TokenID.Ampersand, TokenID.VerticalBar, TokenID.Circumflex,
                            TokenID.DoubleEqual, TokenID.ExclamationEqual, TokenID.LessThan,
                            TokenID.LessThanEqual, TokenID.GreaterThan, TokenID.GreaterThanEqual,
                            TokenID.KW_StartsWith, TokenID.KW_EndsWith, TokenID.KW_Contains,
                            TokenID.KW_Matches))
            {
                Token _operator = token;
                Consume(1);
                return _operator;
            }

            throw new ParseException(FileName, token, string.Format(Resources.UnoverloadableOperator, token));
        }

        /// <summary>
        /// Gets if the given number of parameters is valid for an operator.
        /// </summary>
        /// <param name="tokenID">The operator's TokenID</param>
        /// <param name="count">The given number of parameters</param>
        /// <returns>A boolean</returns>
        protected bool IsValidOperandCount(TokenID tokenID, int count)
        {
            switch (tokenID)
            {
                case TokenID.Plus:
                case TokenID.Minus:
                    return count == 0 || count == 1;
                case TokenID.DoublePlus:
                case TokenID.DoubleMinus:
                case TokenID.Tilda:
                    return count == 0;
                case TokenID.Asterisk:
                case TokenID.Slash:
                case TokenID.Percent:
                case TokenID.DoubleAsterisk:
                case TokenID.DoubleLessThan:
                case TokenID.DoubleGreaterThan:
                case TokenID.Ampersand:
                case TokenID.VerticalBar:
                case TokenID.Circumflex:
                case TokenID.DoubleEqual:
                case TokenID.ExclamationEqual:
                case TokenID.LessThan:
                case TokenID.LessThanEqual:
                case TokenID.GreaterThan:
                case TokenID.GreaterThanEqual:
                case TokenID.KW_StartsWith:
                case TokenID.KW_EndsWith:
                case TokenID.KW_Contains:
                case TokenID.KW_Matches:
                    return count == 1;
                default:
                    throw new ParseException(FileName, token,
                        string.Format(Resources.UnoverloadableOperator, Token.ToString(tokenID)));
            }
        }

        /// <summary>
        /// Recognizes an attribute (or annotation).
        /// </summary>
        /// <returns>An <see cref="AddyScript.Runtime.Attribute"/></returns>
        protected Attribute Attribute()
        {
            Token first = Match(TokenID.Identifier), last = first;
            string name = first.ToString();
            var props = new AttributeProperty[] { };

            if (TryMatch(TokenID.LeftParenthesis))
            {
                Consume(1);
                props = List<AttributeProperty>(AttributeProperty, true, Resources.DuplicatedAttributeProperty);
                last = Match(TokenID.RightParenthesis);
            }

            var attribute = new Attribute(name, props);
            attribute.SetLocation(first.Start, last.End);
            return attribute;
        }

        /// <summary>
        /// Recognizes the definition of an attribute's property.
        /// </summary>
        /// <returns>An <see cref="AddyScript.Runtime.AttributeProperty"/></returns>
        protected AttributeProperty AttributeProperty()
        {
            SkipComments();
            if (token.TokenID != TokenID.Identifier) return null;

            string name = token.ToString();
            Token first = token;
            Consume(1);

            Dynamic value;
            Match(TokenID.Equal);

            SkipComments();
            switch (token.TokenID)
            {
                case TokenID.LT_Boolean:
                    value = Boolean.FromBool((bool) token.Value);
                    break;
                case TokenID.LT_Integer:
                    value = new Integer((int) token.Value);
                    break;
                case TokenID.LT_Long:
                    value = new Long((BigInteger) token.Value);
                    break;
                case TokenID.LT_Float:
                    value = new Float((double) token.Value);
                    break;
                case TokenID.LT_Decimal:
                    value = new Decimal((BigDecimal) token.Value);
                    break;
                case TokenID.LT_Date:
                    value = new Date((DateTime) token.Value);
                    break;
                case TokenID.LT_String:
                    value = new String((string) token.Value);
                    break;
                case TokenID.LT_Null:
                    value = Void.Value;
                    break;
                default:
                    throw new ParseException(FileName, token, Resources.LiteralRequired);
            }

            Token last = token;
            Consume(1);

            var prop = new AttributeProperty(name, value);
            prop.SetLocation(first.Start, last.End);
            return prop;
        }

        /// <summary>
        /// Recognizes a case label.
        /// </summary>
        /// <param name="address">The address of the statement that will follow the parsed case</param>
        /// <returns>A <see cref="AddyScript.Ast.Statements.CaseLabel"/></returns>
        protected CaseLabel CaseLabel(int address)
        {
            Token first = Match(TokenID.KW_Case);
            Dynamic value;

            SkipComments();
            switch (token.TokenID)
            {
                case TokenID.LT_Boolean:
                    value = Boolean.FromBool((bool) token.Value);
                    break;
                case TokenID.LT_Integer:
                    value = new Integer((int) token.Value);
                    break;
                case TokenID.LT_String:
                    value = new String((string) token.Value);
                    break;
                default:
                    throw new ParseException(FileName, token, Resources.OnlyBoolIntOrString);
            }

            Consume(1);
            Token last = Match(TokenID.Colon);

            var caze = new CaseLabel(address, value);
            caze.SetLocation(first.Start, last.End);
            return caze;
        }

        /// <summary>
        /// Recognize a label.
        /// </summary>
        /// <returns><b>true</b> if a label is encountered; <b>false</b> otherwise</returns>
        protected bool GotLabel()
        {
            int k;
            if (LookAhead(TokenID.Colon, out k))
            {
                var label = new ParseTimeLabel(token.ToString(), token.Start, Ll(k).End);
                CurrentFunction.CurrentBlock.Labels.Add(label);
                Consume(k);

                return true;
            }

            return false;
        }
    }
}