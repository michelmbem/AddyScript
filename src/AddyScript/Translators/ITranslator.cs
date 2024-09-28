using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;


namespace AddyScript.Translators;


public interface ITranslator
{
    void TranslateProgram(Program program);
    void TranslateImportDirective(ImportDirective import);
    void TranslateClassDefinition(ClassDefinition classDef);
    void TranslateFunctionDecl(FunctionDecl fnDecl);
    void TranslateExternalFunctionDecl(ExternalFunctionDecl extDecl);
    void TranslateConstantDecl(ConstantDecl cstDecl);
    void TranslateVariableDecl(VariableDecl varDecl);
    void TranslateBlock(Block block);
    void TranslateAssignment(Assignment assignment);
    void TranslateGroupAssignment(GroupAssignment grpAssign);
    void TranslateTernaryExpression(TernaryExpression terExpr);
    void TranslateBinaryExpression(BinaryExpression binExpr);
    void TranslateUnaryExpression(UnaryExpression unExpr);
    void TranslateLiteral(Literal literal);
    void TranslateComplexInitializer(ComplexInitializer cplxInit);
    void TranslateListInitializer(ListInitializer listInit);
    void TranslateMapInitializer(MapInitializer mapInit);
    void TranslateSetInitializer(SetInitializer setInit);
    void TranslateObjectInitializer(ObjectInitializer objInit);
    void TranslateInlineFunction(InlineFunction inlineFn);
    void TranslateVariableRef(VariableRef varRef);
    void TranslateItemRef(ItemRef itemRef);
    void TranslateSliceRef(SliceRef sliceRef);
    void TranslatePropertyRef(PropertyRef propertyRef);
    void TranslateStaticPropertyRef(StaticPropertyRef staticRef);
    void TranslateSelfReference(SelfReference selfRef);
    void TranslateFunctionCall(FunctionCall fnCall);
    void TranslateAnonymousCall(AnonymousCall anCall);
    void TranslateMethodCall(MethodCall methodCall);
    void TranslateStaticMethodCall(StaticMethodCall staticCall);
    void TranslateConstructorCall(ConstructorCall ctorCall);
    void TranslateParentMethodCall(ParentMethodCall pmc);
    void TranslateParentConstructorCall(ParentConstructorCall pcc);
    void TranslateParentPropertyRef(ParentPropertyRef ppr);
    void TranslateParentIndexerRef(ParentIndexerRef pir);
    void TranslateInnerFunctionCall(InnerFunctionCall innerCall);
    void TranslateExternalFunctionCall(ExternalFunctionCall extCall);
    void TranslateTypeVerification(TypeVerification typeVerif);
    void TranslateTypeOfExpression(TypeOfExpression typeOf);
    void TranslateConversion(Conversion conversion);
    void TranslateIfElse(IfElse ifElse);
    void TranslateSwitchBlock(SwitchBlock switchBlock);
    void TranslateForLoop(ForLoop forLoop);
    void TranslateForEachLoop(ForEachLoop forEach);
    void TranslateWhileLoop(WhileLoop whileLoop);
    void TranslateDoLoop(DoLoop doLoop);
    void TranslateContinue(Continue _continue);
    void TranslateBreak(Break _break);
    void TranslateGoto(Goto _goto);
    void TranslateYield(Yield yield);
    void TranslateReturn(Return _return);
    void TranslateThrow(Throw _throw);
    void TranslateTryCatchFinally(TryCatchFinally tcf);
    void TranslateStringInterpolation(StringInterpolation stringInt);
    void TranslatePatternMatching(PatternMatching patMatch);
    void TranslateAlteredCopy(AlteredCopy altCopy);
}