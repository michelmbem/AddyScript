using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;


namespace AddyScript.Compilers
{
    public interface ICompiler
    {
        void CompileProgram(Program program);
        void CompileImportDirective(ImportDirective import);
        void CompileClassDefinition(ClassDefinition classDef);
        void CompileFunctionDecl(FunctionDecl fnDecl);
        void CompileExternalFunctionDecl(ExternalFunctionDecl extDecl);
        void CompileConstantDecl(ConstantDecl cstDecl);
        void CompileVariableDecl(VariableDecl varDecl);
        void CompileBlock(Block block);
        void CompileAssignment(Assignment assignment);
        void CompileTernaryExpression(TernaryExpression terExpr);
        void CompileBinaryExpression(BinaryExpression binExpr);
        void CompileUnaryExpression(UnaryExpression unExpr);
        void CompileLiteral(Literal literal);
        void CompileComplexInitializer(ComplexInitializer cplxInit);
        void CompileListInitializer(ListInitializer listInit);
        void CompileMapInitializer(MapInitializer mapInit);
        void CompileSetInitializer(SetInitializer setInit);
        void CompileObjectInitializer(ObjectInitializer objInit);
        void CompileInlineFunction(InlineFunction inlineFn);
        void CompileVariableRef(VariableRef varRef);
        void CompileItemRef(ItemRef itemRef);
        void CompilePropertyRef(PropertyRef propertyRef);
        void CompileStaticPropertyRef(StaticPropertyRef staticRef);
        void CompileThisReference(ThisReference thisRef);
        void CompileFunctionCall(FunctionCall fnCall);
        void CompileAnonymousCall(AnonymousCall anCall);
        void CompileMethodCall(MethodCall methodCall);
        void CompileStaticMethodCall(StaticMethodCall staticCall);
        void CompileConstructorCall(ConstructorCall ctorCall);
        void CompileParentMethodCall(ParentMethodCall pmc);
        void CompileParentConstructorCall(ParentConstructorCall pcc);
        void CompileInnerFunctionCall(InnerFunctionCall innerCall);
        void CompileExternalFunctionCall(ExternalFunctionCall extCall);
        void CompileTypeVerification(TypeVerification typeVerif);
        void CompileTypeOfExpression(TypeOfExpression typeOf);
        void CompileConversion(Conversion conversion);
        void CompileIfThenElse(IfThenElse ifThenElse);
        void CompileSwitchBlock(SwitchBlock switchBlock);
        void CompileForLoop(ForLoop forLoop);
        void CompileForEachLoop(ForEachLoop forEach);
        void CompileWhileLoop(WhileLoop whileLoop);
        void CompileDoLoop(DoLoop doLoop);
        void CompileContinue(Continue _continue);
        void CompileBreak(Break _break);
        void CompileGoto(Goto _goto);
        void CompileReturn(Return _return);
        void CompileThrow(Throw _throw);
        void CompileTryCatchFinally(TryCatchFinally tcf);
    }
}