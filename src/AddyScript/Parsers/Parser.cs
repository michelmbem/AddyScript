using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Properties;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;
using Boolean = AddyScript.Runtime.DataItems.Boolean;
using Decimal = AddyScript.Runtime.DataItems.Decimal;
using String = AddyScript.Runtime.DataItems.String;
using Void = AddyScript.Runtime.DataItems.Void;


namespace AddyScript.Parsers
{
    /// <summary>
    /// The AddyScript full-featured parser.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the parser.
    /// </remarks>
    /// <param name="lexer">The bound lexer</param>
    public class Parser(Lexer lexer) : ExpressionParser(lexer)
    {

        /// <summary>
        /// Recognizes an entire script.
        /// </summary>
        /// <returns>A <see cref="Ast.Program"/></returns>
        public Program Program()
        {
            Statement[] statements = Asterisk(StatementWithLabels);
            Match(TokenID.EndOfFile);

            var labels = CurrentFunction.CurrentBlock.ConvertLabels(statements);
            var program = new Program(FileName, statements) {Labels = labels};
            if (statements.Length > 0)
                program.SetLocation(statements[0].Start, statements[^1].End);

            return program;
        }

        /// <summary>
        /// Recognizes a statement eventually prededed by labels.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.Statement"/></returns>
        protected Statement StatementWithLabels()
        {
            while (TryMatch(TokenID.Identifier) && LookAhead(TokenID.Colon, out int k))
            {
                var label = new ParseTimeLabel(token.ToString(), token.Start, Ll(k).End);
                CurrentFunction.CurrentBlock.Labels.Add(label);
                Consume(k);
            }

            return Statement();
        }

        /// <summary>
        /// Recognizes a statement.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.Statement"/></returns>
        protected Statement Statement()
        {
            // Skip empty statements
            while (TryMatch(TokenID.SemiColon))
                Consume(1);


            switch (token.TokenID)
            {
                case TokenID.LeftBracket:
                    return StatementWithAttributes();
                case TokenID.KW_Import:
                    return Import();
                case TokenID.Modifier:
                case TokenID.KW_Class:
                    return Class();
                case TokenID.KW_Function:
                    return Function();
                case TokenID.KW_Extern:
                    return ExternalFunction();
                case TokenID.KW_Const:
                    return ConstantDecl();
                case TokenID.KW_Var:
                    return VariableDecl();
                case TokenID.LeftBrace:
                    return Block();
                case TokenID.KW_If:
                    return IfElse();
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
                default:
                    {
                        Expression expr = Expression();
                        
                        if (expr != null)
                        {
                            Token last = Match(TokenID.SemiColon);
                            expr.SetLocation(expr.Start, last.End);
                        }

                        return expr;
                    }
            }
        }

        /// <summary>
        /// Recognizes a non-null statement.
        /// </summary>
        /// <returns>An <see cref="Ast.Statements.Statement"/></returns>
        public Statement RequiredStatement()
        {
            return Required(StatementWithLabels, string.Format(Resources.UnexpectedToken, token));
        }

        /// <summary>
        /// Recognizes a statement decorated with some attributes.
        /// </summary>
        /// <remarks>
        /// Most statements don't make usage of attributes.
        /// This is just a helpful way to attach additionnal informations to a statement.
        /// </remarks>
        /// <returns>An <see cref="Ast.Statements.StatementWithAttributes"/></returns>
        protected StatementWithAttributes StatementWithAttributes()
        {
            Token bookmark = Match(TokenID.LeftBracket);
            AttributeDecl[] attributes = List(Attribute, true, Resources.DuplicatedAttribute);
            Match(TokenID.RightBracket);

            Statement statement = Statement();
            if (statement is StatementWithAttributes stmtWA)
            {
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
                classModifier = (Modifier)first.Value;
                Consume(1);
            }

            Token maybeFirst = Match(TokenID.KW_Class);
            first ??= maybeFirst;
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
            ClassMemberDecl[] classMembers = Asterisk(ClassMember);

            ClassMethodDecl constructor = null;
            ClassPropertyDecl indexer = null;
            var classFields = new List<ClassFieldDecl>();
            var classProperties= new List<ClassPropertyDecl>();
            var classMethods = new List<ClassMethodDecl>();
            var classEvents = new List<ClassEventDecl>();
            
            foreach (var classMember in classMembers)
            {
                foreach (var otherMember in classMembers)
                    if (classMember != otherMember && classMember.Name == otherMember.Name)
                        throw new ScriptException(FileName, classMember, string.Format(Resources.MemberNameConfict, classMember.Name));
                
                if (classModifier == Modifier.Static &&
                    !(classMember.Modifier == Modifier.Static || classMember.Modifier == Modifier.StaticFinal))
                    throw new ScriptException(FileName, classMember, Resources.StaticClassMember);

                if (classModifier != Modifier.Abstract && classMember.Modifier == Modifier.Abstract)
                    throw new ScriptException(FileName, classMember, Resources.AbstractMethodInNonAbstractClass);

                if (classMember is ClassFieldDecl classField)
                {
                    switch (classField.Modifier)
                    {
                        case Modifier.Abstract:
                            throw new ScriptException(FileName, classField,
                                string.Format(Resources.InvalidFieldModifier, classField.Modifier));
                        case Modifier.StaticFinal:
                            if (classField.Initializer == null)
                                throw new ScriptException(FileName, classField, Resources.ConstantFieldShouldBeInitialized);
                            break;
                    }

                    classFields.Add(classField);
                }
                else
                {
                    if (classMember.Modifier == Modifier.StaticFinal)
                        throw new ScriptException(FileName, classMember, Resources.SpecificFieldModifier);

                    if (classMember is ClassPropertyDecl classProperty)
                    {
                        if (classProperty.IsIndexer)
                        {
                            if (indexer != null)
                                throw new ScriptException(FileName, classMember, Resources.SingleIndexer);

                            indexer = classProperty;
                        }
                        else
                            classProperties.Add(classProperty);
                    }
                    else if (classMember is ClassMethodDecl classMethod)
                    {
                        if (classMethod.Name == CurrentClass.Name)
                        {
                            if (constructor != null)
                                throw new ScriptException(FileName, classMember, Resources.SingleConstructor);

                            constructor = classMethod;
                        }
                        else
                            classMethods.Add(classMethod);
                    }
                    else if (classMember is ClassEventDecl classEvent)
                    {
                        if (!(classEvent.Modifier == Modifier.Default || classEvent.Modifier == Modifier.Static))
                            throw new ScriptException(FileName, classEvent,
                                string.Format(Resources.InvalidFieldModifier, classEvent.Modifier));

                        classEvents.Add(classEvent);
                    }
                }
            }

            PopClass();
            Token last = Match(TokenID.RightBrace);

            var classDef = new ClassDefinition(className, superClassName, classModifier, constructor, indexer,
                                               [.. classFields], [.. classProperties], [.. classMethods], [.. classEvents]);
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
            ParameterDecl[] parameters = ParameterList();
            Block body = FunctionBody(name, false, false, false);

            var fnDecl = new FunctionDecl(name, parameters, body);
            fnDecl.SetLocation(first.Start, body.End);
            return fnDecl;
        }

        /// <summary>
        /// Recognizes an inline function's declaration.
        /// </summary>
        /// <returns>An <see cref="Ast.Expressions.InlineFunction"/></returns>
        protected override InlineFunction InlineFunction()
        {
            Token first = Match(TokenID.KW_Function);
            ParameterDecl[] parameters = ParameterList();
            Block body = FunctionBody(null, true, false, false);

            var inlineFn = new InlineFunction(parameters, body);
            inlineFn.SetLocation(first.Start, body.End);
            return inlineFn;
        }

        /// <summary>
        /// Recognizes a lambda expression or a lambda statement.
        /// </summary>
        /// <returns>An <see cref="Ast.Expressions.InlineFunction"/></returns>
        protected override InlineFunction Lambda()
        {
            Token first = Match(TokenID.VerticalBar);
            ParameterDecl[] parameters = List(Parameter, true, Resources.DuplicatedParameter);
            Match(TokenID.VerticalBar);

            Match(TokenID.Arrow);

            Block body;
            if (TryMatch(TokenID.LeftBrace))
            {

                PushFunction(null, false, false, false);
                body = Block();
                body.Append(new Return());
                PopFunction();
            }
            else
            {
                var returned = RequiredExpression();
                body = Ast.Statements.Block.Return(returned);
                body.CopyLocation(returned);
            }

            var lambda = new InlineFunction(parameters, body);
            lambda.SetLocation(first.Start, body.End);
            return lambda;
        }

        /// <summary>
        /// Recognizes the expression returned by a <see cref="MatchCase"/>.
        /// </summary>
        /// <returns>An <see cref="Expression"/></returns>
        protected override Expression MatchCaseExpression()
        {
            if (TryMatch(TokenID.LeftBrace))
            {
                PushFunction(null, false, false, false);
                Block body = Block();
                body.Append(new Return());
                PopFunction();

                var anoCall = new AnonymousCall(new InlineFunction([], body), null, null);
                anoCall.CopyLocation(body);
                return anoCall;
            }
            
            if (TryMatch(TokenID.KW_Throw))
            {
                Token first = token;
                Consume(1);

                Expression thrown = RequiredExpression();

                var anoCall = new AnonymousCall(new InlineFunction([], new Block(new Throw(thrown))), null, null);
                anoCall.SetLocation(first.Start, thrown.End);
                return anoCall;
            }

            return base.MatchCaseExpression();
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
            ParameterDecl[] parameters = ParameterList();
            Token last = Match(TokenID.SemiColon);

            var efd = new ExternalFunctionDecl(name, parameters);
            efd.SetLocation(first.Start, last.End);
            return efd;
        }

        /// <summary>
        /// Recognizes a constant's declaration.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.ConstantDecl"/></returns>
        protected ConstantDecl ConstantDecl()
        {
            Token first = Match(TokenID.KW_Const);
            var initializers = List(PropertyInitializer, true, Resources.DuplicatedConstant);
            Token last = Match(TokenID.SemiColon);

            var constDecl = new ConstantDecl(initializers);
            constDecl.SetLocation(first.Start, last.Start);
            return constDecl;
        }

        /// <summary>
        /// Recognizes a variable's declaration.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.VariableDecl"/></returns>
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
                    varValue = RequiredExpression();
                }

                var initializer = new PropertyInitializer(varName, varValue);
                initializer.SetLocation(bookmark.Start, varValue?.End ?? bookmark.End);
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
        /// <returns>A <see cref="Ast.Statements.Block"/></returns>
        protected Block Block()
        {
            Token first = Match(TokenID.LeftBrace);
            
            CurrentFunction.PushBlock();
            Statement[] stmts = Asterisk(StatementWithLabels);
            var labels = CurrentFunction.CurrentBlock.ConvertLabels(stmts);
            CurrentFunction.PopBlock();

            Token last = Match(TokenID.RightBrace);

            var block = new Block(stmts) { Labels = labels };
            block.SetLocation(first.Start, last.End);
            return block;
        }

        /// <summary>
        /// Recognizes an if-else statement.
        /// </summary>
        /// <returns>An <see cref="Ast.Statements.IfElse"/></returns>
        protected IfElse IfElse()
        {
            Token first = Match(TokenID.KW_If);
            Match(TokenID.LeftParenthesis);
            var cond = RequiredExpression();
            Match(TokenID.RightParenthesis);

            Statement positiveAction = Required(Statement, Resources.StatementRequired);

            Statement negativeAction = null;
            if (TryMatch(TokenID.KW_Else))
            {
                Consume(1);
                negativeAction = Required(Statement, Resources.StatementRequired);
            }

            var ifElse = new IfElse(cond, positiveAction, negativeAction);
            ifElse.SetLocation(first.Start, (negativeAction ?? positiveAction).End);
            return ifElse;
        }

        /// <summary>
        /// Recognizes an switch block.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.SwitchBlock"/></returns>
        protected SwitchBlock SwitchBlock()
        {
            Token first = Match(TokenID.KW_Switch);
            Match(TokenID.LeftParenthesis);
            var expr = RequiredExpression();
            Match(TokenID.RightParenthesis);
            Match(TokenID.LeftBrace);

            var cases = new List<CaseLabel>();
            var stmtList = new List<Statement>();
            int address = 0, defCase = int.MaxValue;
            Dictionary<string, Label> labels;
            Statement[] stmts;

            CurrentFunction.PushBlock();
            ++CurrentFunction.SwitchBlocks;

            while (TryMatch(TokenID.KW_Case))
            {
                CaseLabel caseLabel = CaseLabel(address);

                if (cases.Contains(caseLabel))
                    throw new ScriptException(FileName, caseLabel, Resources.DuplicatedCaseLabel);

                cases.Add(caseLabel);
                stmtList.AddRange(stmts = Asterisk(StatementWithLabels));
                address += stmts.Length;
            }

            if (TryMatch(TokenID.KW_Default))
            {
                Consume(1);
                Match(TokenID.Colon);
                defCase = address;
                stmtList.AddRange(Asterisk(StatementWithLabels));
            }

            Token last = Match(TokenID.RightBrace);
            stmts = [.. stmtList];
            labels = CurrentFunction.CurrentBlock.ConvertLabels(stmts);

            --CurrentFunction.SwitchBlocks;
            CurrentFunction.PopBlock();

            if (cases.Count == 0 && defCase == int.MaxValue)
                throw new ParseException(FileName, first, Resources.CaseLabelRequired);
            
            foreach (CaseLabel caseLabel in cases)
                labels.Add(caseLabel.GetLabelName(), caseLabel);

            if (defCase < int.MaxValue)
                labels.Add(Ast.Statements.CaseLabel.GetDefaultLabelName(), new Label(defCase));

            var switchBlock = new SwitchBlock(expr, [.. cases], defCase, stmts) { Labels = labels };
            switchBlock.SetLocation(first.Start, last.End);
            return switchBlock;
        }

        /// <summary>
        /// Recognizes a for loop.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.ForLoop"/></returns>
        protected ForLoop ForLoop()
        {
            Token first = Match(TokenID.KW_For);
            Match(TokenID.LeftParenthesis);
            
            Statement[] initializers;
            if (TryMatch(TokenID.KW_Var))
                initializers = [VariableDecl()];
            else
            {
                initializers = List(Expression, false, null);
                Match(TokenID.SemiColon);
            }

            Expression guard = Expression();
            Match(TokenID.SemiColon);
            Expression[] updaters = List(Expression, false, null);
            Match(TokenID.RightParenthesis);

            ++CurrentFunction.Loops;
            var body = Required(Statement, Resources.StatementRequired);
            --CurrentFunction.Loops;

            var forLoop = new ForLoop(initializers, guard, updaters, body);
            forLoop.SetLocation(first.Start, body.End);
            return forLoop;
        }

        /// <summary>
        /// Recognizes a for-each loop.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.ForEachLoop"/></returns>
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
            var enumerated = RequiredExpression();
            Match(TokenID.RightParenthesis);

            ++CurrentFunction.Loops;
            var body = Required(Statement, Resources.StatementRequired);
            --CurrentFunction.Loops;

            var forEach = new ForEachLoop(keyName, valueName, enumerated, body);
            forEach.SetLocation(first.Start, body.End);
            return forEach;
        }

        /// <summary>
        /// Recognizes a while loop.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.WhileLoop"/></returns>
        protected WhileLoop WhileLoop()
        {
            Token first = Match(TokenID.KW_While);
            Match(TokenID.LeftParenthesis);
            var guard = RequiredExpression();
            Match(TokenID.RightParenthesis);

            ++CurrentFunction.Loops;
            var body = Required(Statement, Resources.StatementRequired);
            --CurrentFunction.Loops;

            var whileLoop = new WhileLoop(guard, body);
            whileLoop.SetLocation(first.Start, body.End);
            return whileLoop;
        }

        /// <summary>
        /// Recognizes a do-while loop.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.DoLoop"/></returns>
        protected DoLoop DoLoop()
        {
            Token first = Match(TokenID.KW_Do);

            ++CurrentFunction.Loops;
            var body = Required(Statement, Resources.StatementRequired);
            --CurrentFunction.Loops;

            Match(TokenID.KW_While);
            Match(TokenID.LeftParenthesis);
            var guard = RequiredExpression();
            Token last = Match(TokenID.RightParenthesis);

            var doLoop = new DoLoop(guard, body);
            doLoop.SetLocation(first.Start, last.End);
            return doLoop;
        }

        /// <summary>
        /// Recognizes a continue statement.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.Continue"/></returns>
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
        /// <returns>A <see cref="Ast.Statements.Break"/></returns>
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
        /// <returns>A <see cref="Ast.Statements.Goto"/></returns>
        protected Goto Goto()
        {
            string labelName;
            bool jumpToCaseLabel = false;

            Token first = Match(TokenID.KW_Goto);

            if (TryMatch(TokenID.Identifier))
                labelName = Match(TokenID.Identifier).ToString();
            else if (TryMatch(TokenID.KW_Case))
            {
                Consume(1);
                Token valueToken = MatchAny(TokenID.LT_Boolean, TokenID.LT_Integer, TokenID.LT_String);
                DataItem value = DataItemFactory.CreateDataItem(valueToken.Value);
                labelName = Ast.Statements.CaseLabel.GetLabelName(value);
                jumpToCaseLabel= true;
            }
            else
            {
                Match(TokenID.KW_Default);
                labelName = Ast.Statements.CaseLabel.GetDefaultLabelName();
                jumpToCaseLabel = true;
            }

            Token last = Match(TokenID.SemiColon);

            var _goto = new Goto(labelName);
            _goto.SetLocation(first.Start, last.End);

            if (jumpToCaseLabel && CurrentFunction.SwitchBlocks <= 0)
                throw new ScriptException(FileName, _goto, Resources.JumpToCaseLabelOutOfSwitchBlock);

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
        /// <returns>A <see cref="Ast.Statements.Throw"/></returns>
        private Throw Throw()
        {
            Token first = Match(TokenID.KW_Throw);
            var expr = RequiredExpression();
            Token last = Match(TokenID.SemiColon);

            var _throw = new Throw(expr);
            _throw.SetLocation(first.Start, last.End);
            return _throw;
        }

        /// <summary>
        /// Recognizes a try-catch-finally statement.
        /// </summary>
        /// <returns>A <see cref="Ast.Statements.TryCatchFinally"/></returns>
        protected TryCatchFinally TryCatchFinally()
        {
            Token first = Match(TokenID.KW_Try);

            Expression resource = null;
            if (TryMatch(TokenID.LeftParenthesis))
            {
                Consume(1);
                resource = RequiredExpression();
                Match(TokenID.RightParenthesis);
            }

            Block tryBlock = Block();

            string exceptionName = null;
            Block catchBlock = null;
            if (TryMatch(TokenID.KW_Catch))
            {
                Consume(1);

                Match(TokenID.LeftParenthesis);
                exceptionName = Match(TokenID.Identifier).ToString();
                Match(TokenID.RightParenthesis);

                catchBlock = Block();
            }

            Block finallyBlock = null;
            if (TryMatch(TokenID.KW_Finally))
            {
                Consume(1);
                ++CurrentFunction.FinallyBlocks;
                finallyBlock = Block();
                --CurrentFunction.FinallyBlocks;
            }

            Block lastBlock = finallyBlock ?? catchBlock;
            if (lastBlock == null)
            {
                if (resource == null)
                    throw new ParseException(FileName, first, Resources.CatchOrFinallyBlockRequired);
                
                lastBlock = tryBlock;
            }

            var tcf = new TryCatchFinally(tryBlock, exceptionName, catchBlock, finallyBlock) { Resource = resource };
            tcf.SetLocation(first.Start, lastBlock.End);
            return tcf;
        }

        /// <summary>
        /// Recognizes the body of a function.
        /// </summary>
        /// <param name="functionName">The function's name</param>
        /// <param name="isInline">Tells if the function is declared inline or not</param>
        /// <param name="isMethod">Tells if the function is a method or not</param>
        /// <param name="isStatic">Tells if the method is static or not</param>
        /// <returns>A <see cref="Ast.Statements.Block"/></returns>
        protected Block FunctionBody(string functionName, bool isInline, bool isMethod, bool isStatic)
        {

            Block body;

            PushFunction(functionName, isMethod, false, isStatic);

            if (TryMatch(TokenID.Arrow))
            {
                Consume(1);
                var returned = RequiredExpression();
                
                ScriptElement last = returned;
                if (!isInline) last = Match(TokenID.SemiColon);

                body = Ast.Statements.Block.Return(returned);
                body.SetLocation(returned.Start, last.End);
            }
            else
            {
                body = Block();
                body.Append(new Return());
            }

            PopFunction();

            return body;
        }

        /// <summary>
        /// Recognizes the definition of any class member.
        /// </summary>
        /// <returns>A <see cref="ClassMemberDecl"/></returns>
        protected ClassMemberDecl ClassMember()
        {
            Scope scope = Scope.Private;
            Modifier modifier = Modifier.Default;
            ClassMemberDecl classMember = null;
            AttributeDecl[] attributes = null;
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
                        scope = (Scope)token.Value;
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
                        attributes = List(Attribute, true, Resources.DuplicatedAttribute);
                        Match(TokenID.RightBracket);
                        consume = false;
                        gotAttribute = true;
                        break;
                    case TokenID.Identifier:
                        classMember = ClassField(scope, modifier);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Constructor:
                        if (modifier != Modifier.Default)
                            throw new ParseException(FileName, token, Resources.InvalidConstructorModifier);
                        classMember = Constructor(scope);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Property:
                        classMember = ClassProperty(scope, modifier);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Function:
                        classMember = ClassMethod(scope, modifier);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Operator:
                        if (modifier != Modifier.Default)
                            throw new ParseException(FileName, token, Resources.InvalidOperatorModifier);
                        classMember = ClassOperator(scope);
                        consume = false;
                        loop = false;
                        break;
                    case TokenID.KW_Event:
                        classMember = ClassEvent(scope, modifier);
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

            if (!(classMember == null || realFirst == null))
            {
                classMember.Attributes = attributes;
                classMember.SetLocation(realFirst.Start, classMember.End);
            }

            return classMember;
        }

        /// <summary>
        /// Recognizes the definition of a constructor.
        /// </summary>
        /// <param name="scope">The scope of this constructor</param>
        /// <returns>A <see cref="ClassMethodDecl"/></returns>
        protected ClassMethodDecl Constructor(Scope scope)
        {
            Token first = Match(TokenID.KW_Constructor);
            ParameterDecl[] parameters = ParameterList();

            ParentConstructorCall superCall = null;
            if (TryMatch(TokenID.Colon))
            {
                Consume(1);
                Token superStart = Match(TokenID.KW_Super);
                Match(TokenID.LeftParenthesis);
                var args = FunctionArguments();
                Token superEnd = Match(TokenID.RightParenthesis);

                superCall = new ParentConstructorCall(args.Item1, args.Item2);
                superCall.SetLocation(superStart.Start, superEnd.End);
            }

            PushFunction(CurrentClass.Name, true, true, false);
            Block body = Block();
            PopFunction();

            if (superCall != null) body.Insert(0, superCall);
            body.Append(new Return());

            var constructor = new ClassMethodDecl(CurrentClass.Name, scope, Modifier.Default, parameters, body);
            constructor.SetLocation(first.Start, body.End);
            return constructor;
        }

        /// <summary>
        /// Recognizes the definition of a field.
        /// </summary>
        /// <param name="scope">The scope of the field</param>
        /// <param name="modifier">The modifier of the field</param>
        /// <returns>A <see cref="ClassFieldDecl"/></returns>
        protected ClassFieldDecl ClassField(Scope scope, Modifier modifier)
        {
            Token first = Match(TokenID.Identifier);
            string name = first.ToString();

            Expression initialValue = null;
            if (TryMatch(TokenID.Equal))
            {
                Consume(1);
                initialValue = RequiredExpression();
            }

            Token last = Match(TokenID.SemiColon);

            var classField = new ClassFieldDecl(name, scope, modifier, initialValue);
            classField.SetLocation(first.Start, last.End);
            return classField;
        }

        /// <summary>
        /// Recognizes the definition of a property.
        /// </summary>
        /// <param name="scope">The scope of the property</param>
        /// <param name="modifier">The modifier of the property</param>
        /// <returns>A <see cref="ClassPropertyDecl"/></returns>
        protected ClassPropertyDecl ClassProperty(Scope scope, Modifier modifier)
        {
            Token first = Match(TokenID.KW_Property), last;

            string name;
            bool isIndexer = false;

            // Determine if its a simple property or an indexer
            if (TryMatch(TokenID.LeftBracket))
            {
                Consume(1); // To skip '['
                Match(TokenID.RightBracket);
                name = Runtime.OOP.ClassProperty.INDEXER_NAME;
                isIndexer = true;
            }
            else
                name = Match(TokenID.Identifier).ToString();

            // Indexers are not compatible with the "static" modifier
            if (isIndexer && modifier == Modifier.Static)
                throw new ParseException(FileName, first, Resources.IndexerCantBeStatic);

            PropertyAccess access = PropertyAccess.None;
            Block readerBody = null, writerBody = null;
            Scope readerScope = scope, writerScope = scope;
            bool isAuto = false;

            // Handle shorter syntaxes as well as the complete one
            if (TryMatch(TokenID.SemiColon))
            {
                // Shortest syntax, automatic read and write access, example: property name;
                last = token;
                Consume(1); // To skip ';'
                isAuto = modifier != Modifier.Abstract;
                access = PropertyAccess.ReadWrite;
            }
            else if (modifier != Modifier.Abstract && TryMatch(TokenID.Arrow))
            {
                // Compact syntax with a returned expression, readonly access, example: property name => expression;
                Consume(1); // To skip "=>"

                PushFunction(Runtime.OOP.ClassProperty.GetReaderName(name), true, false, modifier == Modifier.Static);
                var returned = RequiredExpression();
                PopFunction();
                
                last = Match(TokenID.SemiColon);
                
                readerBody = Ast.Statements.Block.Return(returned);
                readerBody.SetLocation(returned.Start, last.End);
            }
            else
            {
                // Expanded syntax with complete and compact variants, example: property name { read... write... }
                Match(TokenID.LeftBrace);

                bool loop = true, gotReader = false, gotWriter = false;

                while (loop)
                {
                    Scope accessorScope = scope;
                    if (TryMatch(TokenID.Scope))
                    {
                        accessorScope = (Scope)token.Value;
                        if (accessorScope >= scope)
                            throw new ParseException(FileName, token, Resources.AccessorScopeMustBeMoreRestrictive);
                        Consume(1);
                    }

                    if (TryMatch(t => t.TokenID == TokenID.Identifier && t.ToString() == "read"))
                    {
                        if (gotReader) throw new ParseException(FileName, token, Resources.DuplicatedReadAccessor);

                        Consume(1); // To skip the "read" word

                        if (isAuto || modifier == Modifier.Abstract)
                            Match(TokenID.SemiColon);
                        else if (!gotWriter && TryMatch(TokenID.SemiColon))
                        {
                            isAuto = true;
                            Consume(1);
                        }
                        else
                            readerBody = FunctionBody(Runtime.OOP.ClassProperty.GetReaderName(name),
                                                      false, true, modifier == Modifier.Static);

                        gotReader = true;
                        access |= PropertyAccess.Read;
                        readerScope = accessorScope;
                    }
                    else if (TryMatch(t => t.TokenID == TokenID.Identifier && t.ToString() == "write"))
                    {
                        if (gotWriter) throw new ParseException(FileName, token, Resources.DuplicatedWriteAccessor);

                        Consume(1); // To skip the "write" word

                        if (isAuto || modifier == Modifier.Abstract)
                            Match(TokenID.SemiColon);
                        else if (!gotReader && TryMatch(TokenID.SemiColon))
                        {
                            isAuto = true;
                            Consume(1);
                        }
                        else
                            writerBody = FunctionBody(Runtime.OOP.ClassProperty.GetWriterName(name),
                                                      false, true, modifier == Modifier.Static);

                        gotWriter = true;
                        access |= PropertyAccess.Write;
                        writerScope = accessorScope;
                    }
                    else
                        loop = false;
                }

                last = Match(TokenID.RightBrace);

                if (access == PropertyAccess.None)
                    throw new ParseException(FileName, first, Resources.NoEmptyProperty);

                if (!(readerScope == scope || writerScope == scope))
                    throw new ParseException(FileName, first, Resources.InvalidAccessorsScope);
            }

            // Validate that the user is not trying to define an automatic indexer
            if (isIndexer && isAuto) throw new ParseException(FileName, first, Resources.IndexerCantBeAuto);

            var classProperty = isAuto || modifier == Modifier.Abstract
                              ? new ClassPropertyDecl(name, scope, modifier, access)
                              : new ClassPropertyDecl(name, scope, modifier, readerBody, writerBody);

            classProperty.ReaderScope = readerScope;
            classProperty.WriterScope = writerScope;
            classProperty.SetLocation(first.Start, last.End);
            return classProperty;
        }

        /// <summary>
        /// Recognizes the definition of a method.
        /// </summary>
        /// <param name="scope">The scope of the method</param>
        /// <param name="modifier">The modifier of the method</param>
        /// <returns>A <see cref="ClassMethodDecl"/></returns>
        protected ClassMethodDecl ClassMethod(Scope scope, Modifier modifier)
        {
            Token first = Match(TokenID.KW_Function);
            string name = Match(TokenID.Identifier).ToString();
            ParameterDecl[] parameters = ParameterList();
            ScriptLocation end;
            Block body = null;

            if (modifier == Modifier.Abstract)
                end = Match(TokenID.SemiColon).End;
            else
            {
                body = FunctionBody(name, false, true, modifier == Modifier.Static);
                end = body.End;
            }

            var classMethod = new ClassMethodDecl(name, scope, modifier, parameters, body);
            classMethod.SetLocation(first.Start, end);
            return classMethod;
        }

        /// <summary>
        /// Recognizes an operator overloading.
        /// </summary>
        /// <param name="scope">The scope of the outcoming method</param>
        /// <returns>A <see cref="ClassMethodDecl"/></returns>
        protected ClassMethodDecl ClassOperator(Scope scope)
        {
            Token first = Match(TokenID.KW_Operator);
            Token _operator = MatchOverloadableOperator();

            ParameterDecl[] parameters = ParameterList();
            if (!IsValidOperandCount(_operator.TokenID, parameters.Length))
                throw new ParseException(FileName, first, string.Format(Resources.InvalidOperandCount, _operator));

            string name = IsUnaryOperator(_operator.TokenID, parameters.Length, out bool postfix)
                        ? Runtime.OOP.ClassMethod.GetMethodName(_operator.ToUnaryOperator(postfix))
                        : Runtime.OOP.ClassMethod.GetMethodName(_operator.ToBinaryOperator());

            Block body = FunctionBody(name, false, true, false);

            var classMethod = new ClassMethodDecl(name, scope, Modifier.Default, parameters, body);
            classMethod.SetLocation(first.Start, body.End);
            return classMethod;
        }

        /// <summary>
        /// Recognizes the definition of an _event.
        /// </summary>
        /// <param name="scope">The scope of the _event</param>
        /// <param name="modifier">The modifier of the _event</param>
        /// <returns>A <see cref="ClassEventDecl"/></returns>
        protected ClassEventDecl ClassEvent(Scope scope, Modifier modifier)
        {
            Token first = Match(TokenID.KW_Event);
            string name = Match(TokenID.Identifier).ToString();
            ParameterDecl[] parameters = ParameterList();
            ScriptLocation end = Match(TokenID.SemiColon).End;

            var classEvent = new ClassEventDecl(name, scope, modifier, parameters);
            classEvent.SetLocation(first.Start, end);
            return classEvent;
        }

        /// <summary>
        /// Recognizes a list of function's parameters.
        /// </summary>
        /// <returns>An array of <see cref="ParameterDecl"/>s</returns>
        protected ParameterDecl[] ParameterList()
        {
            Match(TokenID.LeftParenthesis);
            ParameterDecl[] parameters = List(Parameter, true, Resources.DuplicatedParameter);
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
        /// Recognizes the declaration of a function's parameter.
        /// </summary>
        /// <returns>A <see cref="ParameterDecl"/></returns>
        protected ParameterDecl Parameter()
        {
            string name = null, context = "initial";
            bool byRef = false, vaArgs = false;
            DataItem defaultValue = null;
            AttributeDecl[] attributes = null;
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
                                attributes = List(Attribute, true, Resources.DuplicatedAttribute);
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
                                defaultValue = Boolean.FromBool((bool)token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Integer:
                                defaultValue = new Integer((int)token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Long:
                                defaultValue = new Long((BigInteger)token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Float:
                                defaultValue = new Float((double)token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Decimal:
                                defaultValue = new Decimal((BigDecimal)token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_Date:
                                defaultValue = new Date((DateTime)token.Value);
                                context = "done";
                                break;
                            case TokenID.LT_String:
                                defaultValue = new String((string)token.Value);
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

            var param = new ParameterDecl(name, byRef, vaArgs, defaultValue) { Attributes = attributes };
            param.SetLocation(first.Start, last.End);
            return param;
        }

        /// <summary>
        /// Expects the next token to be an overloadable operator.
        /// </summary>
        /// <returns>A token</returns>
        protected Token MatchOverloadableOperator()
        {
            if (TryMatchAny(TokenID.Plus, TokenID.Minus, TokenID.DoublePlus, TokenID.DoubleMinus, TokenID.Tilda,
                            TokenID.Asterisk, TokenID.Slash, TokenID.Percent, TokenID.DoubleAsterisk, TokenID.DoubleLessThan,
                            TokenID.DoubleGreaterThan, TokenID.Ampersand, TokenID.VerticalBar, TokenID.Circumflex,
                            TokenID.DoubleEqual, TokenID.ExclamationEqual, TokenID.LessThan, TokenID.LessThanEqual,
                            TokenID.GreaterThan, TokenID.GreaterThanEqual, TokenID.KW_StartsWith, TokenID.KW_EndsWith,
                            TokenID.KW_Contains, TokenID.KW_Matches))
            {
                Token _operator = token;
                Consume(1);
                return _operator;
            }

            throw new ParseException(FileName, token, string.Format(Resources.UnoverloadableOperator, token));
        }

        /// <summary>
        /// Recognizes the declaration of an attribute (or annotation).
        /// </summary>
        /// <returns>An <see cref="AttributeDecl"/></returns>
        protected AttributeDecl Attribute()
        {
            Token first = Match(TokenID.Identifier), last = first;
            string typeName = first.ToString();
            var props = new List<PropertyInitializer>();

            if (TryMatch(TokenID.LeftParenthesis))
            {
                Consume(1);

                if (!(TryMatch(TokenID.Identifier) && LookAhead(TokenID.Equal, out int pos)))
                {
                    var valueProp = Expression();

                    if (valueProp != null)
                    {
                        props.Add(new PropertyInitializer(AttributeDecl.DEFAULT_FIELD_NAME, valueProp));

                        if (TryMatch(TokenID.Comma)) Consume(1);
                    }
                }

                var moreProps = List(PropertyInitializer, true, Resources.DuplicatedAttributeProperty);

                if (props.Count > 0)
                {
                    var otherValueProp = moreProps.FirstOrDefault(p => p.Name == AttributeDecl.DEFAULT_FIELD_NAME);
                    if (otherValueProp != null) throw new ScriptException(FileName, otherValueProp, Resources.DuplicatedProperty);
                }
                
                props.AddRange(moreProps);
                last = Match(TokenID.RightParenthesis);
            }

            var attribute = new AttributeDecl(typeName, props.ToArray());
            attribute.SetLocation(first.Start, last.End);
            return attribute;
        }

        /// <summary>
        /// Recognizes a case label in a switch block.
        /// </summary>
        /// <param name="address">The next statement's address</param>
        /// <returns>A <see cref="Ast.Statements.CaseLabel"/></returns>
        protected CaseLabel CaseLabel(int address)
        {
            Token first = Match(TokenID.KW_Case);
            SkipComments();

            DataItem value = token.TokenID switch
            {
                TokenID.LT_Boolean => Boolean.FromBool((bool)token.Value),
                TokenID.LT_Integer => new Integer((int)token.Value),
                TokenID.LT_String => new String((string)token.Value),
                _ => throw new ParseException(FileName, token, Resources.OnlyBoolIntOrString),
            };

            Consume(1);
            Token last = Match(TokenID.Colon);

            var caseLabel = new CaseLabel(address, value);
            caseLabel.SetLocation(first.Start, last.End);
            return caseLabel;
        }

        /// <summary>
        /// Gets if the given number of operands is valid for an operator.
        /// </summary>
        /// <param name="tokenID">The operator's TokenID</param>
        /// <param name="count">The given number of operands</param>
        /// <returns>A boolean</returns>
        protected static bool IsValidOperandCount(TokenID tokenID, int count)
        {
            switch (tokenID)
            {
                case TokenID.Plus:
                case TokenID.DoublePlus:
                case TokenID.Minus:
                case TokenID.DoubleMinus:
                    return count == 0 || count == 1;
                case TokenID.Tilda:
                    return count == 0;
                default:
                    return count == 1;
            }
        }

        /// <summary>
        /// Gets if a token represents a unary operator given its number of operands.
        /// </summary>
        /// <param name="tokenID">The operator's TokenID</param>
        /// <param name="count">The given number of operands</param>
        /// <param name="postfix">Tells whether the operator is the postfix variant or not</param>
        /// <returns>A boolean</returns>
        protected static bool IsUnaryOperator(TokenID tokenID, int count, out bool postfix)
        {
            switch (tokenID)
            {
                case TokenID.DoublePlus:
                case TokenID.DoubleMinus:
                    postfix = count == 1;
                    return true;
                default:
                    postfix = false;
                    return count == 0;
            }
        }
    }
}