using System.Collections.Generic;
using System.Xml;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Runtime.OOP;


namespace AddyScript.Translators;


public class XmlGenerator : ITranslator
{
    private readonly XmlDocument document;
    private readonly XmlDeclaration declaration;
    private XmlElement currentElement;
    private string blockElementName;

    public XmlGenerator()
    {
        document = new XmlDocument();
        declaration = document.CreateXmlDeclaration("1.0", null, null);
        document.InsertBefore(declaration, document.DocumentElement);
    }

    public XmlDocument Document => document;

    #region Members of ITranslator

    public void TranslateProgram(Program program)
    {
        currentElement = document.CreateElement("Program");
        document.InsertAfter(currentElement, declaration);

        ProcessLabels(currentElement, program.Labels);

        foreach (Statement statement in program.Statements)
            statement.AcceptTranslator(this);
    }

    public void TranslateImportDirective(ImportDirective import)
    {
        XmlElement tmpElement = document.CreateElement("ImportDirective");
        tmpElement.SetAttribute("ModuleName", import.ModuleName.ToString());
        if (!string.IsNullOrEmpty(import.Alias)) tmpElement.SetAttribute("Alias", import.Alias);
        currentElement.AppendChild(tmpElement);
    }

    public void TranslateClassDefinition(ClassDefinition classDef)
    {
        XmlElement tmpElement = document.CreateElement("ClassDefinition");
        tmpElement.SetAttribute("ClassName", classDef.ClassName);
        currentElement.AppendChild(tmpElement);

        if (!string.IsNullOrEmpty(classDef.SuperClassName))
            tmpElement.SetAttribute("SuperClassName", classDef.SuperClassName);

        if (classDef.Modifier != Modifier.Default)
            tmpElement.SetAttribute("Modifier", classDef.Modifier.ToString());

        if (classDef.Attributes != null && classDef.Attributes.Length > 0)
            ProcessAttributes(tmpElement, classDef.Attributes);

        if (classDef.Constructor != null)
        {
            XmlElement constructElement = document.CreateElement("Constructor");
            ProcessClassMethod(constructElement, classDef.Constructor);
            tmpElement.AppendChild(constructElement);
        }

        if (classDef.Indexer != null)
        {
            XmlElement indexerElement = document.CreateElement("Indexer");
            ProcessClassProperty(indexerElement, classDef.Indexer);
            tmpElement.AppendChild(indexerElement);
        }

        XmlElement fieldsElement = document.CreateElement("Fields");
        tmpElement.AppendChild(fieldsElement);
        foreach (ClassFieldDecl field in classDef.Fields)
            ProcessClassField(fieldsElement, field);

        XmlElement propertiesElement = document.CreateElement("Properties");
        tmpElement.AppendChild(propertiesElement);
        foreach (ClassPropertyDecl property in classDef.Properties)
            ProcessClassProperty(propertiesElement, property);

        XmlElement methodsElement = document.CreateElement("Methods");
        tmpElement.AppendChild(methodsElement);
        foreach (ClassMethodDecl method in classDef.Methods)
            ProcessClassMethod(methodsElement, method);

        XmlElement eventsElement = document.CreateElement("Events");
        tmpElement.AppendChild(eventsElement);
        foreach (ClassEventDecl _event in classDef.Events)
            ProcessClassEvent(eventsElement, _event);
    }

    public void TranslateFunctionDecl(FunctionDecl fnDecl)
    {
        XmlElement tmpElement = document.CreateElement("FunctionDecl");
        tmpElement.SetAttribute("Name", fnDecl.Name);
        currentElement.AppendChild(tmpElement);

        if (fnDecl.Attributes != null && fnDecl.Attributes.Length > 0)
            ProcessAttributes(tmpElement, fnDecl.Attributes);

        XmlElement paramsElement = document.CreateElement("Parameters");
        tmpElement.AppendChild(paramsElement);
        ProcessParameters(paramsElement, fnDecl.Parameters);

        XmlElement previousElement = currentElement;
        currentElement = tmpElement;

        blockElementName = "Body";
        fnDecl.Body.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateExternalFunctionDecl(ExternalFunctionDecl extDecl)
    {
        XmlElement tmpElement = document.CreateElement("ExternalFunctionDecl");
        tmpElement.SetAttribute("Name", extDecl.Name);
        currentElement.AppendChild(tmpElement);

        if (extDecl.Attributes != null && extDecl.Attributes.Length > 0)
            ProcessAttributes(tmpElement, extDecl.Attributes);

        XmlElement paramsElement = document.CreateElement("Parameters");
        tmpElement.AppendChild(paramsElement);
        ProcessParameters(paramsElement, extDecl.Parameters);
    }

    public void TranslateConstantDecl(ConstantDecl cstDecl)
    {
        XmlElement tmpElement = document.CreateElement("ConstantDecl");
        currentElement.AppendChild(tmpElement);
        ProcessVariableSetters(tmpElement, cstDecl.Setters);
    }

    public void TranslateVariableDecl(VariableDecl varDecl)
    {
        XmlElement tmpElement = document.CreateElement("VariableDecl");
        currentElement.AppendChild(tmpElement);
        ProcessVariableSetters(tmpElement, varDecl.Setters);
    }

    public void TranslateBlock(Block block)
    {
        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement(blockElementName ?? "Block");
        previousElement.AppendChild(currentElement);
        blockElementName = null;

        ProcessLabels(currentElement, block.Labels);

        foreach (Statement statement in block.Statements)
        {
            if (statement is Return { Expression: null }) break;
            statement.AcceptTranslator(this);
        }

        currentElement = previousElement;
    }

    public void TranslateBlockAsExpression(BlockAsExpression blkAsExpr)
    {
        TranslateBlock(blkAsExpr.Block);
    }

    public void TranslateAssignment(Assignment assign)
    {
        XmlElement tmpElement = document.CreateElement("Assignment");
        tmpElement.SetAttribute("Operator", assign.Operator.ToString());
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("LeftOperand");
        tmpElement.AppendChild(currentElement);
        assign.LeftOperand.AcceptTranslator(this);

        currentElement = document.CreateElement("RightOperand");
        tmpElement.AppendChild(currentElement);
        assign.RightOperand.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateTernaryExpression(TernaryExpression terExpr)
    {
        XmlElement tmpElement = document.CreateElement("TernaryExpression");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Test");
        tmpElement.AppendChild(currentElement);
        terExpr.Test.AcceptTranslator(this);

        currentElement = document.CreateElement("TruePart");
        tmpElement.AppendChild(currentElement);
        terExpr.TruePart.AcceptTranslator(this);

        currentElement = document.CreateElement("FalsePart");
        tmpElement.AppendChild(currentElement);
        terExpr.FalsePart.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateBinaryExpression(BinaryExpression binExpr)
    {
        XmlElement tmpElement = document.CreateElement("BinaryExpression");
        tmpElement.SetAttribute("Operator", binExpr.Operator.ToString());
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("LeftOperand");
        tmpElement.AppendChild(currentElement);
        binExpr.LeftOperand.AcceptTranslator(this);

        currentElement = document.CreateElement("RightOperand");
        tmpElement.AppendChild(currentElement);
        binExpr.RightOperand.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateUnaryExpression(UnaryExpression unExpr)
    {
        XmlElement tmpElement = document.CreateElement("UnaryExpression");
        tmpElement.SetAttribute("Operator", unExpr.Operator.ToString());
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Operand");
        tmpElement.AppendChild(currentElement);
        unExpr.Operand.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateLiteral(Literal literal)
    {
        XmlElement tmpElement = document.CreateElement("Literal");
        tmpElement.SetAttribute("Type", literal.Value.Class.Name);
        tmpElement.SetAttribute("Value", literal.Value.ToString());
        currentElement.AppendChild(tmpElement);
    }

    public void TranslateComplexInitializer(ComplexInitializer cplxInit)
    {
        XmlElement tmpElement = document.CreateElement("ComplexInitializer");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("RealPartInitializer");
        tmpElement.AppendChild(currentElement);
        cplxInit.RealPartInitializer.AcceptTranslator(this);
        
        currentElement = document.CreateElement("ImaginaryPartInitializer");
        tmpElement.AppendChild(currentElement);
        cplxInit.ImaginaryPartInitializer.AcceptTranslator(this);
        
        currentElement = previousElement;
    }

    public void TranslateTupleInitializer(TupleInitializer tupleInit)
    {
        XmlElement tmpElement = document.CreateElement("TupleInitializer");
        currentElement.AppendChild(tmpElement);
        ProcessArguments(tmpElement, tupleInit.Items);
    }

    public void TranslateListInitializer(ListInitializer listInit)
    {
        XmlElement tmpElement = document.CreateElement("ListInitializer");
        currentElement.AppendChild(tmpElement);
        ProcessArguments(tmpElement, listInit.Items);
    }

    public void TranslateSetInitializer(SetInitializer setInit)
    {
        XmlElement tmpElement = document.CreateElement("SetInitializer");
        currentElement.AppendChild(tmpElement);
        ProcessArguments(tmpElement, setInit.Items);
    }

    public void TranslateMapInitializer(MapInitializer mapInit)
    {
        XmlElement tmpElement = document.CreateElement("MapInitializer");
        currentElement.AppendChild(tmpElement);
        ProcessMapEntries(tmpElement, mapInit.Entries);
    }

    public void TranslateObjectInitializer(ObjectInitializer objectInit)
    {
        XmlElement tmpElement = document.CreateElement("ObjectInitializer");
        currentElement.AppendChild(tmpElement);
        ProcessVariableSetters(tmpElement, objectInit.PropertySetters);
    }

    public void TranslateInlineFunction(InlineFunction inline)
    {
        XmlElement tmpElement = document.CreateElement("InlineFunction");
        currentElement.AppendChild(tmpElement);

        XmlElement paramsElement = document.CreateElement("Parameters");
        tmpElement.AppendChild(paramsElement);
        ProcessParameters(paramsElement, inline.Parameters);

        XmlElement previousElement = currentElement;
        currentElement = tmpElement;

        blockElementName = "Body";
        inline.Body.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateVariableRef(VariableRef variableRef)
    {
        XmlElement tmpElement = document.CreateElement("VariableRef");
        tmpElement.SetAttribute("Name", variableRef.Name);
        currentElement.AppendChild(tmpElement);
    }

    public void TranslateItemRef(ItemRef itemRef)
    {
        XmlElement tmpElement = document.CreateElement("ItemRef");
        tmpElement.SetAttribute("Optional", itemRef.Optional.ToString());
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Owner");
        tmpElement.AppendChild(currentElement);
        itemRef.Owner.AcceptTranslator(this);

        currentElement = document.CreateElement("Index");
        tmpElement.AppendChild(currentElement);
        itemRef.Index.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateSliceRef(SliceRef sliceRef)
    {
        XmlElement tmpElement = document.CreateElement("SliceRef");
        tmpElement.SetAttribute("Optional", sliceRef.Optional.ToString());
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement("Owner");
        tmpElement.AppendChild(currentElement);
        sliceRef.Owner.AcceptTranslator(this);

        if (sliceRef.LowerBound != null)
        {
            currentElement = document.CreateElement("LowerBound");
            tmpElement.AppendChild(currentElement);
            sliceRef.LowerBound.AcceptTranslator(this);
        }
        sliceRef.Owner.AcceptTranslator(this);

        if (sliceRef.UpperBound != null)
        {
            currentElement = document.CreateElement("UpperBound");
            tmpElement.AppendChild(currentElement);
            sliceRef.UpperBound.AcceptTranslator(this);
        }

        currentElement = previousElement;
    }

    public void TranslatePropertyRef(PropertyRef propRef)
    {
        XmlElement tmpElement = document.CreateElement("PropertyRef");
        tmpElement.SetAttribute("PropertyName", propRef.PropertyName);
        tmpElement.SetAttribute("Optional", propRef.Optional.ToString());
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Owner");
        tmpElement.AppendChild(currentElement);
        propRef.Owner.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateStaticPropertyRef(StaticPropertyRef staticRef)
    {
        XmlElement tmpElement = document.CreateElement("StaticPropertyRef");
        tmpElement.SetAttribute("Name", staticRef.Name.ToString());
        currentElement.AppendChild(tmpElement);
    }

    public void TranslateSelfReference(SelfReference selfRef)
    {
        currentElement.AppendChild(document.CreateElement("SelfReference"));
    }

    public void TranslateFunctionCall(FunctionCall functionCall)
    {
        XmlElement tmpElement = document.CreateElement("FunctionCall");
        tmpElement.SetAttribute("FunctionName", functionCall.FunctionName);
        currentElement.AppendChild(tmpElement);

        ProcessArguments(tmpElement, functionCall);
    }

    public void TranslateAnonymousCall(AnonymousCall anCall)
    {
        XmlElement tmpElement = document.CreateElement("AnonymousCall");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Callee");
        tmpElement.AppendChild(currentElement);
        anCall.Callee.AcceptTranslator(this);
        
        currentElement = previousElement;

        ProcessArguments(tmpElement, anCall);
    }

    public void TranslateMethodCall(MethodCall methodCall)
    {
        XmlElement tmpElement = document.CreateElement("MethodCall");
        tmpElement.SetAttribute("FunctionName", methodCall.FunctionName);
        tmpElement.SetAttribute("Optional", methodCall.Optional.ToString());
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement("Target");
        tmpElement.AppendChild(currentElement);
        methodCall.Target.AcceptTranslator(this);

        currentElement = previousElement;

        ProcessArguments(tmpElement, methodCall);
    }

    public void TranslateStaticMethodCall(StaticMethodCall staticCall)
    {
        XmlElement tmpElement = document.CreateElement("StaticMethodCall");
        tmpElement.SetAttribute("Name", staticCall.Name.ToString());
        currentElement.AppendChild(tmpElement);

        ProcessArguments(tmpElement, staticCall);
    }

    public void TranslateConstructorCall(ConstructorCall constCall)
    {
        XmlElement tmpElement = document.CreateElement("ConstructorCall");
        tmpElement.SetAttribute("ClassName", constCall.Name.ToString());
        currentElement.AppendChild(tmpElement);

        ProcessArguments(tmpElement, constCall);

        if (constCall.PropertySetters != null)
        {
            XmlElement propsElement = document.CreateElement("PropertySetters");
            tmpElement.AppendChild(propsElement);

            ProcessVariableSetters(propsElement, constCall.PropertySetters);
        }
    }

    public void TranslateParentMethodCall(ParentMethodCall pmc)
    {
        XmlElement tmpElement = document.CreateElement("ParentMethodCall");
        tmpElement.SetAttribute("FunctionName", pmc.FunctionName);
        currentElement.AppendChild(tmpElement);

        ProcessArguments(tmpElement, pmc);
    }

    public void TranslateParentConstructorCall(ParentConstructorCall pcc)
    {
        XmlElement tmpElement = document.CreateElement("ParentConstructorCall");
        currentElement.AppendChild(tmpElement);

        ProcessArguments(tmpElement, pcc);
    }

    public void TranslateParentPropertyRef(ParentPropertyRef ppr)
    {
        XmlElement tmpElement = document.CreateElement("ParentPropertyRef");
        tmpElement.SetAttribute("PropertyName", ppr.PropertyName);
        currentElement.AppendChild(tmpElement);
    }

    public void TranslateParentIndexerRef(ParentIndexerRef pir)
    {
        XmlElement tmpElement = document.CreateElement("ParentIndexerRef");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement("Index");
        tmpElement.AppendChild(currentElement);
        pir.Index.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateInnerFunctionCall(InnerFunctionCall ifc)
    {
    }

    public void TranslateExternalFunctionCall(ExternalFunctionCall efc)
    {
    }

    public void TranslateTypeVerification(TypeVerification typeVerif)
    {
        XmlElement tmpElement = document.CreateElement("TypeVerification");
        tmpElement.SetAttribute("TypeName", typeVerif.TypeName);
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Expression");
        tmpElement.AppendChild(currentElement);
        typeVerif.Expression.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateTypeOfExpression(TypeOfExpression typeOf)
    {
        XmlElement tmpElement = document.CreateElement("TypeOfExpression");
        tmpElement.SetAttribute("TypeName", typeOf.TypeName);
        currentElement.AppendChild(tmpElement);
    }

    public void TranslateConversion(Conversion conversion)
    {
        XmlElement tmpElement = document.CreateElement("Conversion");
        tmpElement.SetAttribute("TypeName", conversion.TypeName);
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Expression");
        tmpElement.AppendChild(currentElement);
        conversion.Expression.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateIfElse(IfElse ifElse)
    {
        XmlElement tmpElement = document.CreateElement("IfElse");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement("Guard");
        tmpElement.AppendChild(currentElement);
        ifElse.Guard.AcceptTranslator(this);

        currentElement = document.CreateElement("Action");
        tmpElement.AppendChild(currentElement);
        ifElse.Action.AcceptTranslator(this);

        if (ifElse.AlternativeAction != null)
        {
            currentElement = document.CreateElement("AlternativeAction");
            tmpElement.AppendChild(currentElement);
            ifElse.AlternativeAction.AcceptTranslator(this);
        }

        currentElement = previousElement;
    }

    public void TranslateSwitchBlock(SwitchBlock switchBlock)
    {
        XmlElement tmpElement = document.CreateElement("SwitchBlock");
        currentElement.AppendChild(tmpElement);

        if (switchBlock.DefaultCase < int.MaxValue)
            tmpElement.SetAttribute("DefaultCase", switchBlock.DefaultCase.ToString());

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Test");
        tmpElement.AppendChild(currentElement);
        switchBlock.Test.AcceptTranslator(this);

        if (switchBlock.Cases.Length > 0)
        {
            XmlElement casesElement = document.CreateElement("Cases");
            tmpElement.AppendChild(casesElement);
            foreach (CaseLabel _case in switchBlock.Cases)
                ProcessSwitchCase(casesElement, _case);
        }

        ProcessLabels(currentElement, switchBlock.Labels);

        currentElement = document.CreateElement("Statements");
        tmpElement.AppendChild(currentElement);
        foreach (Statement statement in switchBlock.Statements)
            statement.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateForLoop(ForLoop forLoop)
    {
        XmlElement tmpElement = document.CreateElement("ForLoop");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Initializers");
        tmpElement.AppendChild(currentElement);
        foreach (Statement initializer in forLoop.Initializers)
            initializer.AcceptTranslator(this);

        currentElement = document.CreateElement("Guard");
        tmpElement.AppendChild(currentElement);
        forLoop.Guard?.AcceptTranslator(this);

        currentElement = document.CreateElement("Incrementers");
        tmpElement.AppendChild(currentElement);
        foreach (Expression incrementer in forLoop.Incrementers)
            incrementer.AcceptTranslator(this);

        currentElement = document.CreateElement("Action");
        tmpElement.AppendChild(currentElement);
        forLoop.Action.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateForEachLoop(ForEachLoop forEach)
    {
        XmlElement tmpElement = document.CreateElement("ForEachLoop");
        tmpElement.SetAttribute("ValueName", forEach.ValueName);
        currentElement.AppendChild(tmpElement);

        if (forEach.KeyName != ForEachLoop.DEFAULT_KEY_NAME)
            tmpElement.SetAttribute("KeyName", forEach.KeyName);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Guard");
        tmpElement.AppendChild(currentElement);
        forEach.Guard.AcceptTranslator(this);

        currentElement = document.CreateElement("Action");
        tmpElement.AppendChild(currentElement);
        forEach.Action.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateWhileLoop(WhileLoop whileLoop)
    {
        XmlElement tmpElement = document.CreateElement("WhileLoop");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement("Guard");
        tmpElement.AppendChild(currentElement);
        whileLoop.Guard.AcceptTranslator(this);

        currentElement = document.CreateElement("Action");
        tmpElement.AppendChild(currentElement);
        whileLoop.Action.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateDoLoop(DoLoop doLoop)
    {
        XmlElement tmpElement = document.CreateElement("DoLoop");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        
        currentElement = document.CreateElement("Guard");
        tmpElement.AppendChild(currentElement);
        doLoop.Guard.AcceptTranslator(this);

        currentElement = document.CreateElement("Action");
        tmpElement.AppendChild(currentElement);
        doLoop.Action.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslateContinue(Continue _continue)
    {
        currentElement.AppendChild(document.CreateElement("Continue"));
    }

    public void TranslateBreak(Break _break)
    {
        currentElement.AppendChild(document.CreateElement("Break"));
    }

    public void TranslateGoto(Goto _goto)
    {
        XmlElement tmpElement = document.CreateElement("Goto");
        tmpElement.SetAttribute("LabelName", _goto.LabelName);
        currentElement.AppendChild(tmpElement);
    }

    public void TranslateYield(Yield yield)
    {
        XmlElement tmpElement = document.CreateElement("Yield");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        currentElement = tmpElement;
        yield.Expression.AcceptTranslator(this);
        currentElement = previousElement;
    }

    public void TranslateReturn(Return _return)
    {
        XmlElement tmpElement = document.CreateElement("Return");
        currentElement.AppendChild(tmpElement);

        if (_return.Expression == null) return;

        XmlElement previousElement = currentElement;
        currentElement = tmpElement;
        _return.Expression.AcceptTranslator(this);
        currentElement = previousElement;
    }

    public void TranslateThrow(Throw _throw)
    {
        XmlElement tmpElement = document.CreateElement("Throw");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;
        currentElement = tmpElement;
        _throw.Expression.AcceptTranslator(this);
        currentElement = previousElement;
    }

    public void TranslateTryCatchFinally(TryCatchFinally tcf)
    {
        XmlElement tmpElement = document.CreateElement("TryCatchFinally");
        tmpElement.SetAttribute("ExceptionName", tcf.ExceptionName);
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = tmpElement;
        blockElementName = "TryBlock";
        tcf.TryBlock.AcceptTranslator(this);

        if (tcf.CatchBlock != null)
        {
            currentElement = tmpElement;
            blockElementName = "CatchBlock";
            tcf.CatchBlock.AcceptTranslator(this);
        }

        if (tcf.FinallyBlock != null)
        {
            currentElement = tmpElement;
            blockElementName = "FinallyBlock";
            tcf.FinallyBlock.AcceptTranslator(this);
        }

        currentElement = previousElement;
    }

    public void TranslateStringInterpolation(StringInterpolation stringInt)
    {
        XmlElement tmpElement = document.CreateElement("StringInterpolation");
        tmpElement.SetAttribute("Pattern", stringInt.Pattern);
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement("Substitions");
        tmpElement.AppendChild(currentElement);

        foreach (Expression segment in stringInt.Substitions)
            segment.AcceptTranslator(this);

        currentElement = previousElement;
    }

    public void TranslatePatternMatching(PatternMatching patMatch)
    {
        XmlElement tmpElement = document.CreateElement("PatternMatching");
        currentElement.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement("Expression");
        tmpElement.AppendChild(currentElement);
        patMatch.Expression.AcceptTranslator(this);
        
        currentElement = previousElement;

        ProcessMatchCases(tmpElement, patMatch.MatchCases);
    }

    public void TranslateMutableCopy(MutableCopy mutableCopy)
    {
        XmlElement tmpElement = document.CreateElement("MutableCopy");
        currentElement.AppendChild(tmpElement);

        XmlElement propsElement = document.CreateElement("PropertySetters");
        tmpElement.AppendChild(propsElement);

        ProcessVariableSetters(propsElement, mutableCopy.PropertySetters);
    }

    #endregion

    #region Utility

    private void ProcessAttributes(XmlElement parent, AttributeDecl[] attributes)
    {
        XmlElement tmpElement = document.CreateElement("Attributes");
        parent.AppendChild(tmpElement);

        foreach (AttributeDecl attribute in attributes)
            ProcessAttribute(tmpElement, attribute);
    }

    private void ProcessAttribute(XmlElement parent, AttributeDecl attribute)
    {
        XmlElement tmpElement = document.CreateElement("Attribute");
        tmpElement.SetAttribute("Name", attribute.Name);
        parent.AppendChild(tmpElement);

        XmlElement propsElement = document.CreateElement("Fields");
        tmpElement.AppendChild(propsElement);

        ProcessVariableSetters(propsElement, attribute.Fields);
    }

    private void ProcessParameters(XmlElement parent, ParameterDecl[] parameters)
    {
        foreach (ParameterDecl parameter in parameters)
            ProcessParameter(parent, parameter);
    }

    private void ProcessParameter(XmlElement parent, ParameterDecl parameter)
    {
        XmlElement tmpElement = document.CreateElement("ParameterDecl");
        tmpElement.SetAttribute("Name", parameter.Name);
        tmpElement.SetAttribute("ByRef", parameter.ByRef.ToString());
        tmpElement.SetAttribute("VaList", parameter.VaList.ToString());
        tmpElement.SetAttribute("CanBeEmpty", parameter.CanBeEmpty.ToString());
        parent.AppendChild(tmpElement);

        if (parameter.DefaultValue != null)
            tmpElement.SetAttribute("DefaultValue", parameter.DefaultValue.ToString());
        
        if (parameter.Attributes != null && parameter.Attributes.Length > 0)
            ProcessAttributes(tmpElement, parameter.Attributes);
    }

    private void ProcessVariableSetters(XmlElement parent, VariableSetter[] initializers)
    {
        foreach (VariableSetter initializer in initializers)
            ProcessVariableSetter(parent, initializer);
    }

    private void ProcessVariableSetter(XmlElement parent, VariableSetter initializer)
    {
        XmlElement tmpElement = document.CreateElement("VariableSetter");
        parent.AppendChild(tmpElement);
        tmpElement.SetAttribute("Name", initializer.Name);

        if (initializer.Value != null)
        {
            XmlElement previousElement = currentElement;
            currentElement = document.CreateElement("Value");
            tmpElement.AppendChild(currentElement);
            initializer.Value.AcceptTranslator(this);
            currentElement = previousElement;
        }
    }

    private void ProcessArguments(XmlElement parent, CallWithNamedArgs call)
    {
        XmlElement previousElement = currentElement;

        if (call.Arguments != null)
        {
            currentElement = document.CreateElement("Arguments");
            parent.AppendChild(currentElement);
            ProcessArguments(currentElement, call.Arguments);
        }

        if (call.NamedArgs != null)
        {
            XmlElement tmpElement = document.CreateElement("NamedArgs");
            parent.AppendChild(tmpElement);

            foreach (var argPair in call.NamedArgs)
            {
                currentElement = document.CreateElement("Arg");
                currentElement.SetAttribute("Name", argPair.Key);
                tmpElement.AppendChild(currentElement);
                argPair.Value.AcceptTranslator(this);
            }
        }

        currentElement = previousElement;
    }

    private void ProcessArguments(XmlElement parent, Argument[] arguments)
    {
        foreach (Argument argument in arguments)
            ProcessArgument(parent, argument);
    }

    private void ProcessArgument(XmlElement parent, Argument argument)
    {
        XmlElement tmpElement = document.CreateElement("Argument");
        tmpElement.SetAttribute("Spread", argument.Spread.ToString());
        parent.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement("Value");
        tmpElement.AppendChild(currentElement);
        argument.Value.AcceptTranslator(this);

        currentElement = previousElement;
    }

    private void ProcessMapEntries(XmlElement parent, MapEntry[] entries)
    {
        foreach (MapEntry entry in entries)
            ProcessMapEntry(parent, entry);
    }

    private void ProcessMapEntry(XmlElement parent, MapEntry entry)
    {
        XmlElement tmpElement = document.CreateElement("MapEntry");
        parent.AppendChild(tmpElement);

        XmlElement previousElement = currentElement;

        currentElement = document.CreateElement("Key");
        previousElement.AppendChild(currentElement);
        entry.Key.AcceptTranslator(this);

        currentElement = document.CreateElement("Value");
        previousElement.AppendChild(currentElement);
        entry.Value.AcceptTranslator(this);

        currentElement = previousElement;
    }

    private void ProcessLabels(XmlElement parent, Dictionary<string, Label> labels)
    {
        XmlElement labelsElement = document.CreateElement("Labels");
        parent.AppendChild(labelsElement);

        foreach (var pair in labels)
        {
            if (pair.Key.StartsWith('@')) continue;

            XmlElement labelElement = document.CreateElement(pair.Key);
            labelElement.SetAttribute("Address", pair.Value.Address.ToString());
            labelsElement.AppendChild(labelElement);
        }
    }

    private void ProcessSwitchCase(XmlElement parent, CaseLabel switchCase)
    {
        XmlElement tmpElement = document.CreateElement("SwitchCase");
        tmpElement.SetAttribute("HashCode", switchCase.Value.ToString());
        tmpElement.SetAttribute("Address", switchCase.Address.ToString());
        parent.AppendChild(tmpElement);
    }

    private void ProcessClassField(XmlElement parent, ClassFieldDecl field)
    {
        XmlElement tmpElement = document.CreateElement("ClassFieldDecl");
        tmpElement.SetAttribute("Name", field.Name);
        tmpElement.SetAttribute("Scope", field.Scope.ToString());
        parent.AppendChild(tmpElement);

        if (field.Modifier != Modifier.Default)
            tmpElement.SetAttribute("Modifier", field.Modifier.ToString());

        if (field.Initializer == null) return;

        XmlElement savedElement = currentElement;
        currentElement = document.CreateElement("Initializer");
        tmpElement.AppendChild(currentElement);
        field.Initializer.AcceptTranslator(this);
        currentElement = savedElement;
    }

    private void ProcessClassProperty(XmlElement parent, ClassPropertyDecl property)
    {
        XmlElement tmpElement = document.CreateElement("ClassPropertyDecl");
        tmpElement.SetAttribute("Name", property.Name);
        tmpElement.SetAttribute("Scope", property.Scope.ToString());
        parent.AppendChild(tmpElement);

        if (property.Modifier != Modifier.Default)
            tmpElement.SetAttribute("Modifier", property.Modifier.ToString());

        if (property.Access != PropertyAccess.None)
            tmpElement.SetAttribute("Access", property.Access.ToString());

        if (property.ReaderScope != property.Scope)
            tmpElement.SetAttribute("ReaderScope", property.ReaderScope.ToString());

        if (property.WriterScope != property.Scope)
            tmpElement.SetAttribute("WriterScope", property.WriterScope.ToString());

        XmlElement savedElement = currentElement;
        currentElement = tmpElement;

        if (property.ReaderBody != null)
        {
            blockElementName = "ReaderBody";
            property.ReaderBody.AcceptTranslator(this);
        }

        if (property.WriterBody != null)
        {
            blockElementName = "WriterBody";
            property.WriterBody.AcceptTranslator(this);
        }

        currentElement = savedElement;
    }

    private void ProcessClassMethod(XmlElement parent, ClassMethodDecl method)
    {
        XmlElement tmpElement = document.CreateElement("ClassMethodDecl");
        tmpElement.SetAttribute("Name", method.Name);
        tmpElement.SetAttribute("Scope", method.Scope.ToString());
        if (method.Modifier != Modifier.Default)
            tmpElement.SetAttribute("Modifier", method.Modifier.ToString());
        parent.AppendChild(tmpElement);

        XmlElement paramElement = document.CreateElement("Parameters");
        tmpElement.AppendChild(paramElement);
        ProcessParameters(paramElement, method.Parameters);

        if (method.Body == null) return;

        XmlElement savedElement = currentElement;
        currentElement = tmpElement;
        blockElementName = "Body";
        method.Body.AcceptTranslator(this);
        currentElement = savedElement;
    }

    private void ProcessClassEvent(XmlElement parent, ClassEventDecl _event)
    {
        XmlElement tmpElement = document.CreateElement("ClassEventDecl");
        tmpElement.SetAttribute("Name", _event.Name);
        tmpElement.SetAttribute("Scope", _event.Scope.ToString());
        parent.AppendChild(tmpElement);

        if (_event.Modifier != Modifier.Default)
            tmpElement.SetAttribute("Modifier", _event.Modifier.ToString());

        XmlElement savedElement = currentElement;
        currentElement = document.CreateElement("Parameters");
        ProcessParameters(currentElement, _event.Parameters);
        tmpElement.AppendChild(currentElement);
        currentElement = savedElement;
    }

    private void ProcessMatchCases(XmlElement parent, MatchCase[] matchCases)
    {
        XmlElement tmpElement = document.CreateElement("MatchCases");
        parent.AppendChild(tmpElement);

        foreach (MatchCase matchCase in matchCases)
            ProcessMatchCase(tmpElement, matchCase);
    }

    private void ProcessMatchCase(XmlElement parent, MatchCase matchCase)
    {
        XmlElement tmpElement = document.CreateElement("MatchCase");
        parent.AppendChild(tmpElement);

        ProcessPattern(tmpElement, matchCase.Pattern);

        XmlElement savedElement = currentElement;
        
        currentElement = document.CreateElement("Expression");
        matchCase.Expression.AcceptTranslator(this);
        tmpElement.AppendChild(currentElement);
        
        if (matchCase.Guard != null)
        {
            currentElement = document.CreateElement("Guard");
            matchCase.Guard.AcceptTranslator(this);
            tmpElement.AppendChild(currentElement);
        }

        currentElement = savedElement;
    }

    private void ProcessPattern(XmlElement parent, Pattern pattern)
    {
        switch (pattern)
        {
            case AlwaysTruePattern:
                parent.AppendChild(document.CreateElement("AlwaysTruePattern"));
                break;
            case RegexPattern regex:
            {
                // We check RegexPattern before RelationalPattern to avoid problems with inheritance!!
                XmlElement tmpElement = document.CreateElement("RegexPattern");
                tmpElement.SetAttribute("Regex", regex.Value.ToString());
                parent.AppendChild(tmpElement);
                break;
            }
            case RelationalPattern relational:
            {
                XmlElement tmpElement = document.CreateElement("RelationalPattern");
                tmpElement.SetAttribute("Operator", relational.Operator.ToString());
                tmpElement.SetAttribute("Value", relational.Value.ToString());
                parent.AppendChild(tmpElement);
                break;
            }
            case ObjectPattern objectPat:
            {
                // We check ObjectPattern before TypePattern to avoid problems with inheritance!!
                XmlElement tmpElement = document.CreateElement("ObjectPattern");
                tmpElement.SetAttribute("TypeName", objectPat.TypeName);
                parent.AppendChild(tmpElement);

                XmlElement matchersElement = document.CreateElement("PropertyMatchers");
                tmpElement.AppendChild(matchersElement);

                foreach (var matcher in objectPat.PropertyMatchers)
                {
                    XmlElement propertyElement = document.CreateElement(string.Join('.', matcher.Path));
                    ProcessPattern(propertyElement, matcher.Pattern);
                    matchersElement.AppendChild(propertyElement);
                }
                break;
            }
            case DestructuringPattern destructuring:
            {
                // We check DestructuringPattern before TypePattern to avoid problems with inheritance!!
                XmlElement tmpElement = document.CreateElement("DestructuringPattern");
                tmpElement.SetAttribute("TypeName", destructuring.TypeName);
                tmpElement.SetAttribute("PropertyNames", string.Join(", ", destructuring.PropertyNames));
                parent.AppendChild(tmpElement);
                break;
            }
            case TypePattern type:
            {
                XmlElement tmpElement = document.CreateElement("TypePattern");
                tmpElement.SetAttribute("TypeName", type.TypeName);
                parent.AppendChild(tmpElement);
                break;
            }
            case NegativePattern negative:
            {
                XmlElement tmpElement = document.CreateElement("NegativePattern");

                XmlElement childElement = document.CreateElement("Child");
                ProcessPattern(childElement, negative.Child);
                tmpElement.AppendChild(childElement);

                parent.AppendChild(tmpElement);
                break;
            }
            case GroupingPattern grouping:
            {
                XmlElement tmpElement = document.CreateElement("GroupingPattern");

                XmlElement childElement = document.CreateElement("Child");
                ProcessPattern(childElement, grouping.Child);
                tmpElement.AppendChild(childElement);

                parent.AppendChild(tmpElement);
                break;
            }
            case LogicalPattern logical:
            {
                XmlElement tmpElement = document.CreateElement("LogicalPattern");
                tmpElement.SetAttribute("Inclusive", logical.Inclusive.ToString());

                XmlElement childElement = document.CreateElement("Left");
                ProcessPattern(childElement, logical.Left);
                tmpElement.AppendChild(childElement);


                childElement = document.CreateElement("Right");
                ProcessPattern(childElement, logical.Right);
                tmpElement.AppendChild(childElement);

                parent.AppendChild(tmpElement);
                break;
            }
            case PositionalPattern positional:
            {
                XmlElement tmpElement = document.CreateElement("PositionalPattern");
                parent.AppendChild(tmpElement);

                XmlElement itemsElement = document.CreateElement("Items");
                tmpElement.AppendChild(itemsElement);

                foreach (var item in positional.Items)
                    ProcessPattern(itemsElement, item);
                break;
            }
            case StringDestructuringPattern stringDest:
            {
                XmlElement tmpElement = document.CreateElement("StringDestructuringPattern");
                tmpElement.SetAttribute("Regex", stringDest.Regex.ToString());
                tmpElement.SetAttribute("VariableNames", string.Join(", ", stringDest.VariableNames));
                parent.AppendChild(tmpElement);
                break;
            }
        }
    }

    #endregion
}
