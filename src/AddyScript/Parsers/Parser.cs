using System;
using System.Collections.Generic;
using System.Linq;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Properties;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;
using Boolean = AddyScript.Runtime.DataItems.Boolean;
using String = AddyScript.Runtime.DataItems.String;


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
        /// Recognizes a non-null statement.
        /// </summary>
        /// <returns>An <see cref="Ast.Statements.Statement"/></returns>
        public Statement RequiredStatement()
        {
            return Required(StatementWithLabels, string.Format(Resources.UnexpectedToken, token));
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


            return token.TokenID switch
            {
                TokenID.LeftBracket => StatementWithAttributes(),
                TokenID.KW_Import => Import(),
                TokenID.Modifier or TokenID.KW_Class => Class(),
                TokenID.KW_Function => Function(),
                TokenID.KW_Extern => ExternalFunction(),
                TokenID.KW_Const => ConstantDecl(),
                TokenID.KW_Var => VariableDecl(),
                TokenID.LeftBrace => Block(),
                TokenID.KW_If => IfElse(),
                TokenID.KW_Switch => SwitchBlock(),
                TokenID.KW_For => ForLoop(),
                TokenID.KW_ForEach => ForEachLoop(),
                TokenID.KW_While => WhileLoop(),
                TokenID.KW_Do => DoLoop(),
                TokenID.KW_Continue => Continue(),
                TokenID.KW_Break => Break(),
                TokenID.KW_Goto => Goto(),
                TokenID.KW_Return => Return(),
                TokenID.KW_Throw => Throw(),
                TokenID.KW_Try => TryCatchFinally(),
                _ => ExpressionAsStatement(),
            };
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
            Token first = Match(TokenID.LeftBracket);
            AttributeDecl[] attributes = List(Attribute, true, Resources.DuplicatedAttribute);
            Match(TokenID.RightBracket);

            SkipComments();

            StatementWithAttributes stmtWA = token.TokenID switch
            {
                TokenID.Modifier or TokenID.KW_Class => Class(),
                TokenID.KW_Function => Function(),
                TokenID.KW_Extern => ExternalFunction(),
                _ => throw new ParseException(FileName, token, Resources.AttributesNotSupported),
            };

            stmtWA.Attributes = attributes;
            stmtWA.SetLocation(first.Start, stmtWA.End);
            return stmtWA;

        }

        /// <summary>
        /// Reconizes an expression when it's used as a statement.
        /// </summary>
        /// <returns>An <see cref="Expression"/></returns>
        protected Expression ExpressionAsStatement()
        {
            Expression expr = Expression();

            if (expr != null)
            {
                Token last = Match(TokenID.SemiColon);
                expr.SetLocation(expr.Start, last.End);
            }

            return expr;
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

            Modifier modifier = Modifier.Default;
            if (TryMatch(TokenID.Modifier))
            {
                first = token;
                modifier = (Modifier)first.Value;
                Consume(1);
            }

            Match(TokenID.KW_Class);
            first ??= token;
            string className = Match(TokenID.Identifier).ToString();

            string superClassName = null;
            if (TryMatch(TokenID.Colon))
            {
                if (modifier == Modifier.Static)
                    throw new ParseException(FileName, token, Resources.StaticClassHasNoSuperClass);

                Consume(1);
                superClassName = Match(TokenID.Identifier).ToString();
            }

            Match(TokenID.LeftBrace);
            PushClass(modifier, className, superClassName);
            ClassMemberDecl[] members = Asterisk(ClassMember);

            ClassMethodDecl constructor = null;
            ClassPropertyDecl indexer = null;
            var fields = new List<ClassFieldDecl>();
            var properties= new List<ClassPropertyDecl>();
            var methods = new List<ClassMethodDecl>();
            var events = new List<ClassEventDecl>();
            
            foreach (var member in members)
            {
                foreach (var otherMember in members)
                    if (member != otherMember && member.Name == otherMember.Name)
                        throw new ScriptException(FileName, member, string.Format(Resources.MemberNameConfict, member.Name));
                
                if (modifier == Modifier.Static &&
                    !(member.Modifier == Modifier.Static || member.Modifier == Modifier.StaticFinal))
                    throw new ScriptException(FileName, member, Resources.StaticClassMember);

                if (modifier != Modifier.Abstract && member.Modifier == Modifier.Abstract)
                    throw new ScriptException(FileName, member, Resources.AbstractMethodInNonAbstractClass);

                if (member is ClassFieldDecl field)
                {
                    switch (field.Modifier)
                    {
                        case Modifier.Abstract:
                            throw new ScriptException(FileName, field,
                                string.Format(Resources.InvalidFieldModifier, field.Modifier));
                        case Modifier.StaticFinal:
                            if (field.Initializer == null)
                                throw new ScriptException(FileName, field, Resources.ConstantFieldShouldBeInitialized);
                            break;
                    }

                    fields.Add(field);
                }
                else
                {
                    if (member.Modifier == Modifier.StaticFinal)
                        throw new ScriptException(FileName, member, Resources.SpecificFieldModifier);

                    if (member is ClassPropertyDecl property)
                    {
                        if (property.IsIndexer)
                        {
                            if (indexer != null)
                                throw new ScriptException(FileName, member, Resources.SingleIndexer);

                            indexer = property;
                        }
                        else
                            properties.Add(property);
                    }
                    else if (member is ClassMethodDecl method)
                    {
                        if (method.Name == CurrentClass.Name)
                        {
                            if (constructor != null)
                                throw new ScriptException(FileName, member, Resources.SingleConstructor);

                            constructor = method;
                        }
                        else
                            methods.Add(method);
                    }
                    else if (member is ClassEventDecl _event)
                    {
                        if (!(_event.Modifier == Modifier.Default || _event.Modifier == Modifier.Static))
                            throw new ScriptException(FileName, _event,
                                string.Format(Resources.InvalidFieldModifier, _event.Modifier));

                        events.Add(_event);
                    }
                }
            }

            PopClass();
            Token last = Match(TokenID.RightBrace);

            var classDef = new ClassDefinition(className, superClassName, modifier, constructor, indexer,
                                               [.. fields], [.. properties], [.. methods], [.. events]);
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
        /// Recognizes the definition of a class member.
        /// </summary>
        /// <returns>A <see cref="ClassMemberDecl"/></returns>
        protected ClassMemberDecl ClassMember()
        {
            Tuple<Scope, Modifier, AttributeDecl[]> prefix = MemberPrefix(out Token first);

            SkipComments();

            ClassMemberDecl member;
            switch (token.TokenID)
            {
                case TokenID.Identifier:
                    member = ClassField(prefix.Item1, prefix.Item2);
                    break;
                case TokenID.KW_Constructor:
                    if (prefix.Item2 != Modifier.Default)
                        throw new ParseException(FileName, token, Resources.InvalidConstructorModifier);

                    member = Constructor(prefix.Item1);
                    break;
                case TokenID.KW_Property:
                    member = ClassProperty(prefix.Item1, prefix.Item2);
                    break;
                case TokenID.KW_Function:
                    member = ClassMethod(prefix.Item1, prefix.Item2);
                    break;
                case TokenID.KW_Operator:
                    if (prefix.Item2 != Modifier.Default)
                        throw new ParseException(FileName, token, Resources.InvalidOperatorModifier);

                    member = ClassOperator(prefix.Item1);
                    break;
                case TokenID.KW_Event:
                    member = ClassEvent(prefix.Item1, prefix.Item2);
                    break;
                default:
                    member = null;
                    break;
            }

            if (member != null)
            {
                member.Attributes = prefix.Item3;

                if (first != null)
                    member.SetLocation(first.Start, member.End);
            }

            return member;
        }

        /// <summary>
        /// Recognizes the scope, modifier and attributes of a class member.
        /// </summary>
        /// <param name="first">The initial <see cref="Token"/> of the member being recognized</param>
        /// <returns>A tuple of 3 items</returns>
        /// <throws></throws>
        /// <exception cref="ParseException">Malformed prefix</exception>
        protected Tuple<Scope, Modifier, AttributeDecl[]> MemberPrefix(out Token first)
        {
            Scope scope = Scope.Private;
            Modifier modifier = Modifier.Default;
            AttributeDecl[] attributes = null;
            bool gotScope = false, gotModifier = false, gotAttributes = false;
            bool loop = true;

            first = null;

            while (loop)
            {
                SkipComments();

                switch (token.TokenID)
                {
                    case TokenID.Scope:
                        if (gotScope) throw new ParseException(FileName, token);
                        
                        scope = (Scope)token.Value;
                        first = token;
                        Consume(1);
                        gotScope = true;
                        break;
                    case TokenID.Modifier:
                        if (gotModifier) throw new ParseException(FileName, token);
                        
                        modifier = (Modifier)token.Value;
                        first = token;
                        Consume(1);

                        if ((modifier == Modifier.Static &&
                             TryMatch(t => t.TokenID == TokenID.Modifier && t.Value.Equals(Modifier.Final))) ||
                            (modifier == Modifier.Final &&
                             TryMatch(t => t.TokenID == TokenID.Modifier && t.Value.Equals(Modifier.Static))))
                        {
                            modifier = Modifier.StaticFinal;
                            Consume(1);
                        }
                        
                        gotModifier = true;
                        break;
                    case TokenID.LeftBracket:
                        if (gotAttributes) throw new ParseException(FileName, token);

                        first = token;
                        Consume(1);
                        attributes = List(Attribute, true, Resources.DuplicatedAttribute);
                        Match(TokenID.RightBracket);
                        gotAttributes = true;
                        break;
                    default:
                        loop = false;
                        break;
                }
            }

            return new Tuple<Scope, Modifier, AttributeDecl[]>(scope, modifier, attributes);
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
            string name = null;
            bool byRef = false, vaArgs = false;
            DataItem defaultValue = null;
            AttributeDecl[] attributes = null;
            Token first = null, last = null;
            bool gotAttributes = false, gotPrefix = false, gotName = false;
            bool loop = true;

            while (loop)
            {
                SkipComments();

                switch (token.TokenID)
                {
                    case TokenID.LeftBracket:
                        if (gotAttributes || gotName) throw new ParseException(FileName, token);

                        first ??= token;
                        Consume(1);
                        attributes = List(Attribute, true, Resources.DuplicatedAttribute);
                        Match(TokenID.RightBracket);
                        gotAttributes = true;
                        break;
                    case TokenID.KW_Ref:
                        if (gotPrefix || gotName) throw new ParseException(FileName, token);

                        byRef = gotPrefix = true;
                        first ??= token;
                        Consume(1);
                        break;
                    case TokenID.KW_Params:
                        if (gotPrefix || gotName) throw new ParseException(FileName, token);

                        vaArgs = gotPrefix = true;
                        first ??= token;
                        Consume(1);
                        break;
                    case TokenID.Identifier:
                        if (gotName) throw new ParseException(FileName, token);

                        name = token.ToString();
                        gotName = true;
                        last = token;
                        first ??= last;
                        Consume(1);
                        break;
                    case TokenID.Equal:
                        if (gotPrefix || !gotName) throw new ParseException(FileName, token);

                        Consume(1);
                        last = Match(t => t.IsLiteral);
                        defaultValue = DataItemFactory.CreateDataItem(last.Value);
                        break;
                    default:
                        loop = false;
                        break;
                }
            }

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