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


namespace AddyScript.Parsers;


/// <summary>
/// The AddyScript full-featured parser.
/// </summary>
/// <remarks>
/// Initializes a new instance of the parser.
/// </remarks>
/// <param name="lexer">The bound lexer</param>
public class Parser(Lexer lexer) : ExpressionParser(lexer)
{
    public const string PROPERTY_READER_START = "read";
    public const string PROPERTY_WRITER_START = "write";

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
        if (statements.Length > 0) program.SetLocation(statements[0].Start, statements[^1].End);

        return program;
    }

    /// <summary>
    /// Recognizes a non-null statement.
    /// </summary>
    /// <returns>An <see cref="Ast.Statements.Statement"/></returns>
    public Statement RequiredStatement()
    {
        return Required(Statement, Resources.StatementRequired);
    }

    /// <summary>
    /// Recognizes a statement eventually preceded by labels.
    /// </summary>
    /// <returns>A <see cref="Ast.Statements.Statement"/></returns>
    private Statement StatementWithLabels()
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
    private Statement Statement()
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
            TokenID.KW_Let => AssignmentWithLet(),
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
            TokenID.KW_Yield => Yield(),
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
    private StatementWithAttributes StatementWithAttributes()
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
            _ => throw new SyntaxError(FileName, token, Resources.AttributesNotSupported),
        };

        stmtWA.Attributes = attributes;
        stmtWA.SetLocation(first.Start, stmtWA.End);
        return stmtWA;
    }

    /// <summary>
    /// Reconizes an expression when it's used as a statement.
    /// </summary>
    /// <returns>An <see cref="Expression"/></returns>
    private Expression ExpressionAsStatement()
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
    private ImportDirective Import()
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
    private ClassDefinition Class()
    {
        Token first = null, last;

        Modifier modifier = Modifier.Default;
        if (TryMatch(TokenID.Modifier))
        {
            first = token;
            modifier = (Modifier)first.Value;
            Consume(1);
        }

        last = Match(TokenID.KW_Class);
        first ??= last;

        string className = Match(TokenID.Identifier).ToString();
        
        string superClassName = null;
        if (TryMatch(TokenID.Colon))
        {
            if (modifier == Modifier.Static)
                throw new SyntaxError(FileName, token, Resources.StaticClassHasNoSuperClass);

            Consume(1);
            superClassName = Match(TokenID.Identifier).ToString();
        }

        Match(TokenID.LeftBrace);
        PushClass(modifier, className, superClassName);
        var (constructor, indexer, fields, properties, methods, events) =
            IdentifiyClassMembers(modifier, Asterisk(ClassMember));
        PopClass();
        last = Match(TokenID.RightBrace);

        var classDef = new ClassDefinition(className, superClassName, modifier, constructor, indexer,
                                           [.. fields], [.. properties], [.. methods], [.. events]);
        classDef.SetLocation(first.Start, last.End);
        return classDef;
    }

    /// <summary>
    /// Recognizes a function's declaration.
    /// </summary>
    /// <returns>A <see cref="FunctionDecl"/></returns>
    private FunctionDecl Function()
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
        Block body = FunctionBody(null, true, CurrentFunction.IsMethod, CurrentFunction.IsStatic);

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
        
        PushFunction(null, CurrentFunction.IsMethod, false, CurrentFunction.IsStatic);
        
        Block body;
        if (TryMatch(TokenID.LeftBrace))
            body = BlockBody();
        else
        {
            var returned = RequiredExpression();
            body = Ast.Statements.Block.WithReturn(returned);
            body.CopyLocation(returned);
        }
        
        PopFunction();

        var lambda = new InlineFunction(parameters, body);
        lambda.SetLocation(first.Start, body.End);
        return lambda;
    }

    /// <summary>
    /// Recognizes the expression returned by a <see cref="MatchCase"/>.
    /// </summary>
    /// <returns>An <see cref="Expression"/></returns>
    protected override Expression MatchCaseExpression() =>
        TryMatch(TokenID.LeftBrace)
            ? new BlockAsExpression(Block(true))
            : base.MatchCaseExpression();

    /// <summary>
    /// Recognizes an external function's declaration.
    /// </summary>
    /// <returns>An <see cref="ExternalFunctionDecl"/></returns>
    private ExternalFunctionDecl ExternalFunction()
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
    /// Recognizes the declaration of one or many constants.
    /// </summary>
    /// <returns>A <see cref="Ast.Statements.ConstantDecl"/></returns>
    private ConstantDecl ConstantDecl()
    {
        Token first = Match(TokenID.KW_Const);
        var initializers = List(VariableSetter, true, Resources.DuplicatedConstant);
        Token last = Match(TokenID.SemiColon);

        var constDecl = new ConstantDecl(initializers);
        constDecl.SetLocation(first.Start, last.Start);
        return constDecl;
    }

    /// <summary>
    /// Recognizes the declaration of one or many variables.
    /// </summary>
    /// <returns>A <see cref="Ast.Statements.VariableDecl"/></returns>
    private VariableDecl VariableDecl()
    {
        List<VariableSetter> initializers = [];
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

            var initializer = new VariableSetter(varName, varValue);
            initializer.SetLocation(bookmark.Start, varValue?.End ?? bookmark.End);
            initializers.Add(initializer);

            if (TryMatch(TokenID.Comma)) Consume(1);
        }

        Token last = Match(TokenID.SemiColon);

        var varDecl = new VariableDecl([.. initializers]);
        varDecl.SetLocation(first.Start, last.Start);
        return varDecl;
    }

    /// <summary>
    /// Recognizes an assignment starting with the <b>let</b> keyword.
    /// </summary>
    /// <returns>An <see cref="Assignment"/></returns>
    private Assignment AssignmentWithLet()
    {
        Token first = Match(TokenID.KW_Let);
        Expression lValue = Reference();
        Match(TokenID.Equal);
        Expression rvalue = RequiredExpression();
        Token last = Match(TokenID.SemiColon);

        var expr = new Assignment(lValue, rvalue);
        expr.SetLocation(first.Start, last.End);
        return expr;
    }

    /// <summary>
    /// Recognizes a block of statements.
    /// </summary>
    /// <param name="asExpression">
    /// Tells if the block is recognized as an expression.
    /// </param>
    /// <returns>A <see cref="Ast.Statements.Block"/></returns>
    private Block Block(bool asExpression = false)
    {
        Token first = Match(TokenID.LeftBrace);
        
        CurrentFunction.PushBlock(asExpression);
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
    private IfElse IfElse()
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
    private SwitchBlock SwitchBlock()
    {
        Token first = Match(TokenID.KW_Switch);
        Match(TokenID.LeftParenthesis);
        var expr = RequiredExpression();
        Match(TokenID.RightParenthesis);
        Match(TokenID.LeftBrace);

        List<CaseLabel> cases = [];
        List<Statement> stmtList = [];
        int address = 0, defCase = int.MaxValue;
        Dictionary<string, Label> labels;
        Statement[] stmts;

        CurrentFunction.PushBlock(false);
        ++CurrentFunction.SwitchBlocks;

        while (TryMatch(TokenID.KW_Case))
        {
            CaseLabel caseLabel = CaseLabel(address);

            if (cases.Contains(caseLabel))
                throw new ScriptError(FileName, caseLabel, Resources.DuplicatedCaseLabel);

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
            throw new SyntaxError(FileName, first, Resources.CaseLabelRequired);
        
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
    private ForLoop ForLoop()
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
    private ForEachLoop ForEachLoop()
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
    private WhileLoop WhileLoop()
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
    private DoLoop DoLoop()
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
    private Continue Continue()
    {
        Token first = Match(TokenID.KW_Continue);
        if (CurrentFunction.Loops <= 0)
            throw new SyntaxError(FileName, first, Resources.NoContinueOutOfLoop);

        Token last = Match(TokenID.SemiColon);

        var _continue = new Continue();
        _continue.SetLocation(first.Start, last.End);
        return _continue;
    }

    /// <summary>
    /// Recognizes a break statement.
    /// </summary>
    /// <returns>A <see cref="Ast.Statements.Break"/></returns>
    private Break Break()
    {
        Token first = Match(TokenID.KW_Break);
        if (CurrentFunction.Loops <= 0 && CurrentFunction.SwitchBlocks <= 0)
            throw new SyntaxError(FileName, first, Resources.NoBreakOutOfLoop);

        Token last = Match(TokenID.SemiColon);

        var _break = new Break();
        _break.SetLocation(first.Start, last.End);
        return _break;
    }

    /// <summary>
    /// Recognizes a goto statement.
    /// </summary>
    /// <returns>A <see cref="Ast.Statements.Goto"/></returns>
    private Goto Goto()
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
            throw new ScriptError(FileName, _goto, Resources.JumpToCaseLabelOutOfSwitchBlock);

        return _goto;
    }

    /// <summary>
    /// Recognizes a yield statement.
    /// </summary>
    /// <returns>A <see cref="Ast.Statements.Yield"/></returns>
    private Yield Yield()
    {
        Token first = Match(TokenID.KW_Yield);
        if (!(CurrentFunction.IsIterator || CurrentFunction.CurrentBlock.CanYield))
            throw new SyntaxError(FileName, first, Resources.YieldUsedOutOfIterator);

        var expr = RequiredExpression();
        Token last = Match(TokenID.SemiColon);

        var yield = new Yield(expr);
        yield.SetLocation(first.Start, last.End);
        return yield;
    }

    /// <summary>
    /// Recognizes a return statement.
    /// </summary>
    /// <returns><see cref="Return"/></returns>
    private Return Return()
    {
        Token first = Match(TokenID.KW_Return);
        if (CurrentFunction.FinallyBlocks > 0)
            throw new SyntaxError(FileName, first, Resources.CannotReturnFromFinallyBlock);

        Expression expr = Expression();
        if (expr != null)
        {
            if (CurrentFunction.IsMain)
                throw new ScriptError(FileName, expr, Resources.ScriptCannotReturnValue);

            if (CurrentFunction.IsContructor)
                throw new ScriptError(FileName, expr, Resources.ConstructorCantReturnValue);

            if (CurrentFunction.IsIterator)
                throw new ScriptError(FileName, expr, Resources.IteratorCantReturnValue);
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
    private TryCatchFinally TryCatchFinally()
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
                throw new SyntaxError(FileName, first, Resources.CatchOrFinallyBlockRequired);
            
            lastBlock = tryBlock;
        }

        var tcf = new TryCatchFinally(tryBlock, exceptionName, catchBlock, finallyBlock) { Resource = resource };
        tcf.SetLocation(first.Start, lastBlock.End);
        return tcf;
    }

    /// <summary>
    /// Recognizes the definition of the body of a function.
    /// </summary>
    /// <param name="functionName">The function's name</param>
    /// <param name="isInline">Tells if the function is declared inline or not</param>
    /// <param name="isMethod">Tells if the function is a method or not</param>
    /// <param name="isStatic">Tells if the method is static or not</param>
    /// <returns>A <see cref="Ast.Statements.Block"/></returns>
    private Block FunctionBody(string functionName, bool isInline, bool isMethod, bool isStatic)
    {
        PushFunction(functionName, isMethod, false /* Not for constructors */, isStatic);
        Block body = TryMatch(TokenID.Arrow) ? ExpressionBody(isInline) : BlockBody();
        PopFunction();
        return body;
    }

    private Block ExpressionBody(bool isInline)
    {
        Consume(1); // Skip the arrow (=>)
        var returned = base.MatchCaseExpression();

        ScriptElement last = returned;
        if (!isInline) last = Match(TokenID.SemiColon);

        Block body = Ast.Statements.Block.WithReturn(returned);
        body.SetLocation(returned.Start, last.End);
        return body;
    }

    private Block BlockBody()
    {
        Block body = Block();
        body.Append(new Return());
        return body;
    }

    /// <summary>
    /// Recognizes the definition of a class member.
    /// </summary>
    /// <returns>A <see cref="ClassMemberDecl"/></returns>
    private ClassMemberDecl ClassMember()
    {
        var (attributes, scope, modifier) = ClassMemberPrefix(out Token first);

        SkipComments();

        ClassMemberDecl member = token.TokenID switch
        {
            TokenID.Identifier => Field(scope, modifier),
            TokenID.KW_Constructor => modifier == Modifier.Default
                                    ? Constructor(scope)
                                    : throw new SyntaxError(FileName, token, Resources.InvalidConstructorModifier),
            TokenID.KW_Property => Property(scope, modifier),
            TokenID.KW_Function => Method(scope, modifier),
            TokenID.KW_Operator => modifier == Modifier.Default
                                 ? Operator(scope)
                                 : throw new SyntaxError(FileName, token, Resources.InvalidOperatorModifier),
            TokenID.KW_Event => Event(scope, modifier),
            _ => null,
        };

        if (member != null)
        {
            member.Attributes = attributes;
            if (first != null) member.SetLocation(first.Start, member.End);
        }

        return member;
    }

    /// <summary>
    /// Recognizes the scope, modifier and attributes of a class member.
    /// </summary>
    /// <param name="first">The initial <see cref="Token"/> of the member being recognized</param>
    /// <returns>A (AttributeDecl[], Scope, Modifier) tuple</returns>
    /// <exception cref="SyntaxError">Malformed prefix</exception>
    private (AttributeDecl[], Scope, Modifier) ClassMemberPrefix(out Token first)
    {
        AttributeDecl[] attributes = null;
        Scope? scope = null;
        Modifier? modifier = null;
        first = null;

        if (TryMatch(TokenID.LeftBracket))
        {
            first = token;
            Consume(1);
            attributes = List(Attribute, true, Resources.DuplicatedAttribute);
            Match(TokenID.RightBracket);
        }

        switch (token.TokenID)
        {
            case TokenID.Scope:
                scope = (Scope)token.Value;
                Consume(1);
                if (TryMatch(TokenID.Modifier))
                    modifier = ClassMemberModifier();
                break;
            case TokenID.Modifier:
                modifier = ClassMemberModifier();
                Consume(1);
                if (TryMatch(TokenID.Scope))
                {
                    scope = (Scope)token.Value;
                    Consume(1);
                }
                break;
        }

        return (attributes, scope ?? Scope.Private, modifier ?? Modifier.Default);
    }

    /// <summary>
    /// Determines the modifier for a class member based on the current token and advances the token stream as needed.
    /// </summary>
    /// <remarks>
    /// This method combines static and final modifiers into a single <see cref="Modifier.StaticFinal"/> value
    /// when both are detected in sequence. The token stream is advanced to reflect consumed modifiers.
    /// </remarks>
    /// <returns>
    /// A <see cref="Modifier"/> value representing the detected modifier for the class member.
    /// Returns <see cref="Modifier.StaticFinal"/> if both static and final modifiers are present.
    /// </returns>
    private Modifier ClassMemberModifier()
    {
        var modifier = (Modifier)token.Value;
        Consume(1);

        if ((modifier == Modifier.Static && TryMatchValue(TokenID.Modifier, Modifier.Final)) ||
            (modifier == Modifier.Final && TryMatchValue(TokenID.Modifier, Modifier.Static)))
        {
            modifier = Modifier.StaticFinal;
            Consume(1);
        }

        return modifier;
    }

    /// <summary>
    /// Recognizes the definition of a constructor.
    /// </summary>
    /// <param name="scope">The scope of this constructor</param>
    /// <returns>A <see cref="ClassMethodDecl"/></returns>
    private ClassMethodDecl Constructor(Scope scope)
    {
        Token first = Match(TokenID.KW_Constructor);
        ParameterDecl[] parameters = ParameterList();

        ParentConstructorCall superCall = null;
        if (TryMatch(TokenID.Colon))
        {
            Consume(1);

            Token superStart = Match(TokenID.KW_Super);
            Match(TokenID.LeftParenthesis);
            var (positionalArgs, namedArgs) = FunctionArguments();
            Token superEnd = Match(TokenID.RightParenthesis);

            superCall = new ParentConstructorCall(positionalArgs, namedArgs);
            superCall.SetLocation(superStart.Start, superEnd.End);
        }

        PushFunction(CurrentClass.Name, true, true, false);
        Block body = BlockBody();
        PopFunction();

        if (superCall != null) body.Insert(0, superCall);

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
    private ClassFieldDecl Field(Scope scope, Modifier modifier)
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
    private ClassPropertyDecl Property(Scope scope, Modifier modifier)
    {
        Token first = Match(TokenID.KW_Property);

        string name;
        bool isIndexer;

        if (TryMatch(TokenID.LeftBracket))
        {
            Consume(1); // To skip '['
            Match(TokenID.RightBracket);
            name = ClassProperty.INDEXER_NAME;
            isIndexer = true;
        }
        else
        {
            name = Match(TokenID.Identifier).ToString();
            isIndexer = false;
        }

        var (access, readerScope, readerBody, writerScope, writerBody) =
            ProperyBody(name, scope, modifier, out ScriptElement last);

        if (isIndexer)
            switch (modifier)
            {
                case Modifier.Static:
                    // Indexers are not compatible with the "static" modifier
                    throw new SyntaxError(FileName, first, Resources.IndexerCantBeStatic);
                case not Modifier.Abstract when readerBody == null && writerBody == null:
                    // Validate that the user is not trying to define an automatic indexer
                    throw new SyntaxError(FileName, first, Resources.IndexerCantBeAuto);
            }

        var classProperty = new ClassPropertyDecl(name, scope, modifier, access, readerScope, readerBody, writerScope, writerBody);
        classProperty.SetLocation(first.Start, last.End);
        return classProperty;
    }

    /// <summary>
    /// Parses the body of a property declaration and returns the access type, reader and writer scopes, and their
    /// corresponding bodies.
    /// </summary>
    /// <remarks>
    /// The method supports multiple property declaration syntaxes, including automatic,
    /// expression-bodied, and expanded forms. The returned scopes and bodies correspond to the reader and writer
    /// accessors as defined in the property declaration.
    /// </remarks>
    /// <param name="name">The name of the property being parsed.</param>
    /// <param name="scope">The scope in which the property is declared.</param>
    /// <param name="modifier">The modifier applied to the property, such as static or abstract.</param>
    /// <param name="last">When this method returns, contains the last script element processed during parsing.</param>
    /// <returns>
    /// A tuple containing the property access type, the reader scope and body, and the writer scope and body. If the
    /// property is read-only or write-only, the corresponding body or scope may be null.
    /// </returns>
    /// <exception cref="SyntaxError">Thrown if an abstract property is declared with a body, which is not allowed.</exception>
    private (PropertyAccess access, Scope readerScope, Block readerBody, Scope writerScope, Block writerBody)
        ProperyBody(string name, Scope scope, Modifier modifier, out ScriptElement last)
    {
        PropertyAccess access = PropertyAccess.None;
        Scope readerScope = scope;
        Scope writerScope = scope;
        Block readerBody = null;
        Block writerBody = null;
        last = null;

        SkipComments();

        switch (token.TokenID)
        {
            case TokenID.SemiColon: // Shortest syntax, automatic read and write access
                access = PropertyAccess.ReadWrite;
                last = token;
                Consume(1);
                break;
            case TokenID.Arrow: // Compact syntax with a returned expression, readonly access
                if (modifier == Modifier.Abstract)
                    throw new SyntaxError(FileName, token, Resources.AbstractMemberCantHaveBody);

                PushFunction(ClassProperty.GetReaderName(name), true, false, modifier == Modifier.Static);
                last = readerBody = ExpressionBody(false);
                PopFunction();
                break;
            case TokenID.LeftBrace: // Expanded form
                (access, readerScope, readerBody, writerScope, writerBody) =
                    PropertyAccessors(name, scope, modifier, ref last);
                break;
        }

        return (access, readerScope, readerBody, writerScope, writerBody);
    }

    /// <summary>
    /// Parses property accessor blocks for the specified property name and scope, determining the read and write
    /// accessors and their associated scopes and bodies.
    /// </summary>
    /// <remarks>
    /// The method supports parsing both read and write accessors, including cases where only one
    /// accessor is present. The returned scopes may differ depending on the order and presence of accessor
    /// definitions.
    /// </remarks>
    /// <param name="name">The name of the property for which accessors are being parsed.</param>
    /// <param name="scope">The parent scope in which the property is defined. Used as the base for accessor scopes.</param>
    /// <param name="modifier">The modifier to apply to the accessor methods, such as visibility or other attributes.</param>
    /// <param name="last">A reference to the last parsed script element. Updated to reflect the last token processed during parsing.</param>
    /// <returns>
    /// A tuple containing the property access type, the scope and body for the reader accessor, and the scope and body
    /// for the writer accessor. If an accessor is not defined, its body will be null.
    /// </returns>
    /// <exception cref="SyntaxError">Thrown if the accessor block is malformed, missing required definitions, or contains invalid syntax.</exception>
    private (PropertyAccess access, Scope readerScope, Block readerBody, Scope writerScope, Block writerBody)
        PropertyAccessors(string name, Scope scope, Modifier modifier, ref ScriptElement last)
    {
        PropertyAccess access;
        Scope? readerScope;
        Scope? writerScope;
        Block readerBody = null;
        Block writerBody = null;

        Match(TokenID.LeftBrace);
        Scope? tempScope = AccessorScope(scope, null);

        if (TryMatchWord(PROPERTY_READER_START))
        {
            Consume(1); // To skip the 'read' word
            access = PropertyAccess.Read;
            readerBody = AccessorBody(ClassProperty.GetReaderName(name), modifier);
            readerScope = tempScope;
            writerScope = AccessorScope(scope, readerScope);

            if (TryMatchWord(PROPERTY_WRITER_START))
            {
                Consume(1); // To skip the 'write' word
                access = PropertyAccess.ReadWrite;
                writerBody = AccessorBody(ClassProperty.GetWriterName(name), modifier);
            }
            else if (writerScope.HasValue)
                throw new SyntaxError(FileName, token); // A definition should follow any accessor scope
        }
        else if (TryMatchWord(PROPERTY_WRITER_START))
        {
            Consume(1); // To skip the 'write' word
            access = PropertyAccess.Write;
            writerBody = AccessorBody(ClassProperty.GetWriterName(name), modifier);
            writerScope = tempScope;
            readerScope = AccessorScope(scope, writerScope);

            if (TryMatchWord(PROPERTY_READER_START))
            {
                Consume(1); // To skip the 'read' word
                access = PropertyAccess.ReadWrite;
                readerBody = AccessorBody(ClassProperty.GetReaderName(name), modifier);
            }
            else if (readerScope.HasValue)
                throw new SyntaxError(FileName, token); // A definition should follow any accessor scope
        }
        else
            throw new SyntaxError(FileName, token, Resources.NoEmptyProperty);

        last = Match(TokenID.RightBrace);

        return (access, readerScope ?? scope, readerBody, writerScope ?? scope, writerBody);
    }

    /// <summary>
    /// Determines the scope for an accessor based on the specified property scope and the scope of the other accessor, if present.
    /// </summary>
    /// <param name="propertyScope">
    /// The scope defined for the property to which the accessor belongs.
    /// Used as a reference to ensure the accessor scope is more restrictive.
    /// </param>
    /// <param name="otherAccessorScope">The scope of the other accessor, if already defined. If specified, both accessors cannot redefine the scope.</param>
    /// <returns>A nullable value indicating the scope of the accessor if explicitly specified; otherwise, null if no scope is defined.</returns>
    /// <exception cref="SyntaxError">
    /// Thrown if both accessors attempt to redefine the scope, or if the specified accessor scope is not more restrictive than the property scope.
    /// </exception>
    private Scope? AccessorScope(Scope propertyScope, Scope? otherAccessorScope)
    {
        Scope? accessorScope = null;
        
        if (TryMatch(TokenID.Scope))
        {
            if (otherAccessorScope.HasValue)
                throw new SyntaxError(FileName, token, Resources.BothAccessorsCantRedefineScope);
            accessorScope = (Scope)token.Value;
            if (accessorScope.Value >= propertyScope)
                throw new SyntaxError(FileName, token, Resources.AccessorScopeMustBeMoreRestrictive);
            Consume(1);
        }

        return accessorScope;
    }

    /// <summary>
    /// Parses and returns the body of an accessor for the specified member, or null if the accessor is declared without a body.
    /// </summary>
    /// <param name="name">The name of the member whose accessor body is being parsed.</param>
    /// <param name="modifier">The modifier applied to the accessor. Must not be <see cref="Modifier.Abstract"/>.</param>
    /// <returns>
    /// A <see cref="Block"/> representing the parsed accessor body, or null if the accessor is declared with a semicolon and no body.
    /// </returns>
    /// <exception cref="SyntaxError">
    /// Thrown if <paramref name="modifier"/> is <see cref="Modifier.Abstract"/>, as abstract members cannot have a body.
    /// </exception>
    private Block AccessorBody(string name, Modifier modifier)
    {
        if (TryMatch(TokenID.SemiColon))
        {
            Consume(1); // Skip ';'
            return null;
        }
        else if (modifier == Modifier.Abstract)
            throw new SyntaxError(FileName, token, Resources.AbstractMemberCantHaveBody);

        return FunctionBody(name, false, true, modifier == Modifier.Static);
    }

    /// <summary>
    /// Recognizes the definition of a method.
    /// </summary>
    /// <param name="scope">The scope of the method</param>
    /// <param name="modifier">The modifier of the method</param>
    /// <returns>A <see cref="ClassMethodDecl"/></returns>
    private ClassMethodDecl Method(Scope scope, Modifier modifier)
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
    private ClassMethodDecl Operator(Scope scope)
    {
        Token first = Match(TokenID.KW_Operator);
        Token _operator = MatchOverloadableOperator();

        ParameterDecl[] parameters = ParameterList();
        if (!IsValidOperandCount(_operator.TokenID, parameters.Length))
            throw new SyntaxError(FileName, first, string.Format(Resources.InvalidOperandCount, _operator));

        string name = IsUnaryOperator(_operator.TokenID, parameters.Length, out bool postfix)
                    ? ClassMethod.GetMethodName(_operator.ToUnaryOperator(postfix))
                    : ClassMethod.GetMethodName(_operator.ToBinaryOperator());

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
    private ClassEventDecl Event(Scope scope, Modifier modifier)
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
    /// Identifies the members of a class definition and group them by category.
    /// </summary>
    /// <param name="modifier">The modifier of the class</param>
    /// <param name="members">The list of all members defined for the target class</param>
    /// <returns>A tuple of all class members grouped by category</returns>
    /// <exception cref="ScriptError">If a rule of syntax is violated</exception>
    private (ClassMethodDecl, ClassPropertyDecl, List<ClassFieldDecl>,
             List<ClassPropertyDecl>, List<ClassMethodDecl>, List<ClassEventDecl>)
        IdentifiyClassMembers(Modifier modifier, ClassMemberDecl[] members)
    {
        ClassMethodDecl constructor = null;
        ClassPropertyDecl indexer = null;
        List<ClassFieldDecl> fields = [];
        List<ClassPropertyDecl> properties = [];
        List<ClassMethodDecl> methods = [];
        List<ClassEventDecl> events = [];

        foreach (ClassMemberDecl member in members)
        {
            // Check that each member has a unique name
            if (members.Any(m => m != member && m.Name == member.Name))
                throw new ScriptError(FileName, member, string.Format(Resources.MemberNameConfict, member.Name));

            // Check that each member of a static class is also static
            if (modifier == Modifier.Static && !member.IsStatic)
                throw new ScriptError(FileName, member, Resources.StaticClassMember);

            // Check that there is no abstract member in a non-abstract class
            if (modifier != Modifier.Abstract && member.Modifier == Modifier.Abstract)
                throw new ScriptError(FileName, member, Resources.AbstractMemberInNonAbstractClass);

            if (member is ClassFieldDecl field)
            {
                switch (field.Modifier)
                {
                    case Modifier.Abstract: // A field cannot be abstract
                        throw new ScriptError(FileName, field, string.Format(Resources.InvalidFieldModifier, field.Modifier));
                    case Modifier.StaticFinal when field.Initializer == null:
                        // A static final field (i.e. a class constant) should have a default value
                        throw new ScriptError(FileName, field, Resources.ConstantFieldShouldBeInitialized);
                }

                fields.Add(field);
            }
            else
            {
                // Only fields can be class constants
                if (member.Modifier == Modifier.StaticFinal)
                    throw new ScriptError(FileName, member, Resources.SpecificFieldModifier);

                if (member is ClassPropertyDecl property)
                {
                    if (property.IsIndexer)
                    {
                        // The indexer has to be unique
                        if (indexer != null)
                            throw new ScriptError(FileName, member, Resources.SingleIndexer);

                        indexer = property;
                    }
                    else
                        properties.Add(property);
                }
                else if (member is ClassMethodDecl method)
                {
                    if (method.Name == CurrentClass.Name)
                    {
                        // The constructor also has to be unique
                        if (constructor != null)
                            throw new ScriptError(FileName, member, Resources.SingleConstructor);

                        constructor = method;
                    }
                    else
                        methods.Add(method);
                }
                else if (member is ClassEventDecl evt)
                {
                    // Events cannot be abstract
                    if (evt.Modifier == Modifier.Abstract)
                        throw new ScriptError(FileName, evt, string.Format(Resources.InvalidFieldModifier, evt.Modifier));

                    events.Add(evt);
                }
            }
        }

        return (constructor, indexer, fields, properties, methods, events);
    }

    /// <summary>
    /// Recognizes a list of function's parameters.
    /// </summary>
    /// <returns>An array of <see cref="ParameterDecl"/>s</returns>
    private ParameterDecl[] ParameterList()
    {
        Match(TokenID.LeftParenthesis);
        ParameterDecl[] parameters = List(Parameter, true, Resources.DuplicatedParameter);
        Match(TokenID.RightParenthesis);

        for (int i = 0; i < parameters.Length - 1; ++i)
        {
            if (parameters[i].VaList)
                throw new ScriptError(FileName, parameters[i], Resources.VaArgsMustBeTheLast);

            if (parameters[i].DefaultValue != null)
                for (int j = i + 1; j < parameters.Length; ++j)
                    if (!parameters[j].VaList && parameters[j].DefaultValue == null)
                        throw new ScriptError(FileName, parameters[j], Resources.MandatoryParamsPrecede);
        }

        return parameters;
    }

    /// <summary>
    /// Recognizes the declaration of a function's parameter.
    /// </summary>
    /// <returns>A <see cref="ParameterDecl"/></returns>
    private ParameterDecl Parameter()
    {
        string name;
        AttributeDecl[] attributes = null;
        bool isByRef = false, isVaList = false, canBeEmpty = true;
        DataItem defaultValue = null;
        Token first = null;
        ScriptElement last;

        if (TryMatch(TokenID.LeftBracket))
        {
            first = token;
            Consume(1);
            attributes = List(Attribute, true, Resources.DuplicatedAttribute);
            Match(TokenID.RightBracket);
        }

        SkipComments();
        first ??= token;

        switch (token.TokenID)
        {
            case TokenID.Ampersand:
                isByRef = true;
                Consume(1);
                last = Match(TokenID.Identifier);
                break;
            case TokenID.DoubleDot:
                isVaList = true;
                Consume(1);
                last = Match(TokenID.Identifier);
                break;
            case TokenID.Identifier:
                last = token;
                Consume(1);
                break;
            case { } when attributes is not null:
                // At least the name should be present when attributes are defined
                throw new SyntaxError(FileName, token, Resources.ParameterNameExpected);
            default:
                return null;
        }

        name = last.ToString();

        if (TryMatch(TokenID.Exclamation))
        {
            canBeEmpty = false;
            last = token;
            Consume(1);
        }

        if (TryMatch(TokenID.Equal))
        {
            Consume(1);
            last = RequiredExpression();
            if (last is not Literal literal)
                throw new ScriptError(FileName, last, Resources.LiteralRequired);
            defaultValue = literal.Value;
        }

        var param = new ParameterDecl(name, isByRef, isVaList, defaultValue, canBeEmpty) { Attributes = attributes };
        param.SetLocation(first.Start, last.End);
        return param;
    }

    /// <summary>
    /// Tests if the next <see cref="Token"/> that is not a comment has the given <see cref="TokenID"/> and value.
    /// </summary>
    /// <param name="requiredID">The <see cref="TokenID"/> we may want to match</param>
    /// <param name="expectedValue">The value that we want to match</param>
    /// <returns>
    /// <b>true</b> if the token's ID is <paramref name="requiredID"/> and that its value is <paramref name="expectedValue"/>;
    /// <b>false</b> otherwise
    /// </returns>
    private bool TryMatchValue(TokenID requiredID, object expectedValue) =>
        TryMatch(t => t.TokenID == requiredID && t.Value.Equals(expectedValue));

    /// <summary>
    /// Tests if the next <see cref="Token"/> that is not a comment is the <paramref name="word"/> identifier.
    /// </summary>
    /// <param name="word">The identifier to match</param>
    /// <returns><b>true</b> if the token is the <paramref name="word"/> identifier; <b>false</b> otherwise</returns>
    private bool TryMatchWord(string word) => TryMatchValue(TokenID.Identifier, word);

    /// <summary>
    /// Expects the next token to be an overloadable operator.
    /// </summary>
    /// <returns>A token</returns>
    private Token MatchOverloadableOperator()
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

        throw new SyntaxError(FileName, token, string.Format(Resources.UnoverloadableOperator, token));
    }

    /// <summary>
    /// Recognizes the declaration of an attribute (or annotation).
    /// </summary>
    /// <returns>An <see cref="AttributeDecl"/></returns>
    private AttributeDecl Attribute()
    {
        Token first = Match(TokenID.Identifier), last = first;
        string name = first.ToString();
        List<VariableSetter> fields = [];

        if (TryMatch(TokenID.LeftParenthesis))
        {
            Consume(1); // Skip '('

            // If the opening parenthesis is not followed by an identifier and equal sign,
            if (!(TryMatch(TokenID.Identifier) && LookAhead(TokenID.Equal, out var _)))
            {
                var value = Expression();
                if (value != null)
                {
                    fields.Add(new (AttributeDecl.DEFAULT_FIELD_NAME, value));
                    if (TryMatch(TokenID.Comma)) Consume(1);
                }
            }

            var moreFields = List(VariableSetter, true, Resources.DuplicatedAttributeField);
            if (fields.Count > 0 && moreFields.Any(p => p.Name == AttributeDecl.DEFAULT_FIELD_NAME))
                throw new ScriptError(FileName, fields[0], Resources.DuplicatedAttributeValue);
            
            fields.AddRange(moreFields);
            last = Match(TokenID.RightParenthesis);
        }

        var attribute = new AttributeDecl(name, [.. fields]);
        attribute.SetLocation(first.Start, last.End);
        return attribute;
    }

    /// <summary>
    /// Recognizes a case label in a switch block.
    /// </summary>
    /// <param name="address">The next statement's address</param>
    /// <returns>A <see cref="Ast.Statements.CaseLabel"/></returns>
    private CaseLabel CaseLabel(int address)
    {
        Token first = Match(TokenID.KW_Case);
        SkipComments();

        DataItem value = token.TokenID switch
        {
            TokenID.LT_Boolean => Boolean.FromBool((bool)token.Value),
            TokenID.LT_Integer => new Integer((int)token.Value),
            TokenID.LT_String => new String((string)token.Value),
            _ => throw new SyntaxError(FileName, token, Resources.OnlyBoolIntOrString),
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
    private static bool IsValidOperandCount(TokenID tokenID, int count)
    {
        return tokenID switch
        {
            TokenID.Plus or TokenID.Minus or TokenID.DoublePlus or TokenID.DoubleMinus => count is 0 or 1,
            TokenID.Tilda => count == 0,
            _ => count == 1,
        };
    }

    /// <summary>
    /// Gets if a token represents a unary operator given its number of operands.
    /// </summary>
    /// <param name="tokenID">The operator's TokenID</param>
    /// <param name="count">The given number of operands</param>
    /// <param name="postfix">Tells whether the operator is the postfix variant or not</param>
    /// <returns>A boolean</returns>
    private static bool IsUnaryOperator(TokenID tokenID, int count, out bool postfix)
    {
        if (tokenID is TokenID.DoublePlus or TokenID.DoubleMinus)
        {
            postfix = count == 1;
            return true;
        }

        postfix = false;
        return count == 0;
    }
}