using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Parsers;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;
using AddyScript.Translators.Utility;


namespace AddyScript.Translators;


public class CodeGenerator(TextWriter textWriter) : ITranslator
{
    #region Keywords

    private static readonly string[] typeNames = [
        "blob", "bool", "closure", "complex", "date", "decimal", "float", "int", "list", "long",
        "map", "object", "queue", "rational", "resource", "set", "stack", "string", "tuple", "void"
    ];

    private static readonly string[] keywords = [
        "abstract", "break", "case", "catch", "class", "const", "constructor", "contains", "continue", "default",
        "do", "else", "endswith", "extern", "event", "false", "final", "finally", "for", "foreach", "function",
        "goto", "if", "import", "in", "is", "let", "local", "matches", "new", "null", "private", "property",
        "protected", "public", "return", "startswith", "static", "super", "switch", "this", "throw", "true", "try",
        "typeof","using", "yield", "when", "while"
    ];

    #endregion

    private readonly IndentedTextWriter textWriter =
        textWriter is IndentedTextWriter itw ? itw : new IndentedTextWriter(textWriter);
    private bool inFunctionBody, inForLoopInitializer, isBlockInline;
    private char stringWrapper = '"';

    #region Members of ITranslator

    public void TranslateProgram(Program program)
    {
        Statement prevStatement = null;
        int counter = 0;

        while (counter < program.Statements.Length)
        {
            foreach (var pair in program.Labels.Where(pair => pair.Value.Address == counter))
                textWriter.WriteLine($"{SafeName(pair.Key)}:");

            Statement statement = program.Statements[counter];
            if (prevStatement != null && (
                statement is StatementWithAttributes ||
                prevStatement is  StatementWithAttributes ||
                statement is ImportDirective ^ prevStatement is ImportDirective))
                textWriter.WriteLine();

            statement.AcceptTranslator(this);
            if (statement is Expression) textWriter.WriteLine(';');
            prevStatement = statement;
            ++counter;
        }
    }

    public void TranslateImportDirective(ImportDirective import)
    {
        textWriter.WriteLine(string.IsNullOrEmpty(import.Alias)
            ? $"import {SafeName(import.ModuleName)};"
            : $"import {SafeName(import.ModuleName)} as {SafeName(import.Alias)};");
    }

    public void TranslateClassDefinition(ClassDefinition classDef)
    {
        if (classDef.Attributes is { Length: > 0 })
            DumpAttributesList(classDef.Attributes, true);

        if (Equals(classDef.SuperClassName, Class.Record.Name))
            DumpRecord(classDef);
        else
            DumpClass(classDef);
    }

    public void TranslateFunctionDecl(FunctionDecl fnDecl)
    {
        if (fnDecl.Attributes is { Length: > 0 })
            DumpAttributesList(fnDecl.Attributes, true);

        textWriter.Write($"function {SafeName(fnDecl.Name)}");
        DumpParametersList(fnDecl.Parameters);
        DumpFunctionBody(fnDecl.Body);
    }

    public void TranslateExternalFunctionDecl(ExternalFunctionDecl extDecl)
    {
        if (extDecl.Attributes != null && extDecl.Attributes.Length > 0)
            DumpAttributesList(extDecl.Attributes, true);

        textWriter.Write($"extern function {SafeName(extDecl.Name)}");
        DumpParametersList(extDecl.Parameters);
        textWriter.WriteLine(';');
    }

    public void TranslateConstantDecl(ConstantDecl cstDecl)
    {
        textWriter.Write("const ");

        for (int i = 0; i < cstDecl.Setters.Length; ++i)
        {
            if (i > 0) textWriter.Write(", ");
            DumpVariableSetter(cstDecl.Setters[i]);
        }

        textWriter.WriteLine(';');
    }

    public void TranslateVariableDecl(VariableDecl varDecl)
    {
        textWriter.Write("var ");

        for (int i = 0; i < varDecl.Setters.Length; ++i)
        {
            if (i > 0) textWriter.Write(", ");
            DumpVariableSetter(varDecl.Setters[i]);
        }

        if (!inForLoopInitializer) textWriter.WriteLine(';');
    }

    public void TranslateBlock(Block block)
    {
        textWriter.WriteLine('{');
        ++textWriter.Indentation;

        var (wasInFunctionBody, wasBlockInline) = (inFunctionBody, isBlockInline);
        inFunctionBody = isBlockInline = false;

        var (counter, length) = (0, block.Statements.Length);

        if (wasInFunctionBody && length > 0 &&
            block.Statements[^1] is Return { Expression: null })
            --length;

        while (counter < length)
        {
            if (block.Statements[counter] is ParentConstructorCall)
            {
                ++counter;
                continue;
            }

            foreach (var pair in block.Labels.Where(pair => pair.Value.Address == counter))
                textWriter.WriteLine($"{SafeName(pair.Key)}:");

            Statement stmt = block.Statements[counter];
            stmt.AcceptTranslator(this);
            if (stmt is Expression) textWriter.WriteLine(';');
            ++counter;
        }

        --textWriter.Indentation;
        textWriter.Write("}");
        if (!wasBlockInline) textWriter.WriteLine();

        (inFunctionBody, isBlockInline) = (wasInFunctionBody, wasBlockInline);
    }

    public void TranslateBlockAsExpression(BlockAsExpression blkAsExpr)
    {
        bool wasBlockInline = isBlockInline;
        isBlockInline = true;
        TranslateBlock(blkAsExpr.Block);
        isBlockInline = wasBlockInline;
    }

    public void TranslateAssignment(Assignment assignment)
    {
        assignment.LeftOperand.AcceptTranslator(this);
        textWriter.Write(assignment.Operator == BinaryOperator.IfEmpty
            ? " ?? "
            : $" {BinaryOperatorToString(assignment.Operator)}= ");
        MayBeParenthesize(assignment.RightOperand);
    }

    public void TranslateTernaryExpression(TernaryExpression terExpr)
    {
        MayBeParenthesize(terExpr.Test);
        textWriter.Write(" ? ");
        MayBeParenthesize(terExpr.TruePart);
        textWriter.Write(" : ");
        MayBeParenthesize(terExpr.FalsePart);
    }

    public void TranslateBinaryExpression(BinaryExpression binExpr)
    {
        MayBeParenthesize(binExpr.LeftOperand);
        textWriter.Write($" {BinaryOperatorToString(binExpr.Operator)} ");
        MayBeParenthesize(binExpr.RightOperand);
    }

    public void TranslateUnaryExpression(UnaryExpression unExpr)
    {
        if (unExpr.Operator is UnaryOperator.NotEmpty or UnaryOperator.PostIncrement or UnaryOperator.PostDecrement)
        {
            MayBeParenthesize(unExpr.Operand);
            textWriter.Write(UnaryOperatorToString(unExpr.Operator));
        }
        else
        {
            textWriter.Write(UnaryOperatorToString(unExpr.Operator));
            MayBeParenthesize(unExpr.Operand);
        }
    }

    public void TranslateLiteral(Literal literal)
    {
        DumpDataItem(literal.Value);
    }

    public void TranslateComplexInitializer(ComplexInitializer cplxInit)
    {
        textWriter.Write('(');
        cplxInit.RealPartInitializer.AcceptTranslator(this);
        textWriter.Write(", ");
        cplxInit.ImaginaryPartInitializer.AcceptTranslator(this);
        textWriter.Write(')');
    }

    public void TranslateTupleInitializer(TupleInitializer tupleInitializer)
    {
        DumpArgumentList(tupleInitializer.Items, "(", ")");
    }

    public void TranslateListInitializer(ListInitializer listInit)
    {
        DumpArgumentList(listInit.Items);
    }

    public void TranslateSetInitializer(SetInitializer setInit)
    {
        DumpArgumentList(setInit.Items, "{", "}");
    }

    public void TranslateMapInitializer(MapInitializer mapInit)
    {
        DumpMapEntryList(mapInit.Entries);
    }

    public void TranslateObjectInitializer(ObjectInitializer objInit)
    {
        textWriter.Write("new ");
        DumpVariableSetterList(objInit.PropertySetters);
    }

    public void TranslateInlineFunction(InlineFunction inlineFn)
    {
        if (inlineFn.IsLambda())
        {
            var parameters = inlineFn.Parameters;

            textWriter.Write("|");
            if (parameters.Length == 0)
                textWriter.Write(' ');
            else
                for (var i = 0; i < parameters.Length; ++i)
                {
                    if (i > 0) textWriter.Write(", ");
                    textWriter.Write(SafeName(parameters[i].Name));
                }
            textWriter.Write("| => ");

            var ret = (Return)inlineFn.Body.Statements[0];
            ret.Expression.AcceptTranslator(this);
        }
        else
        {
            var wasBlockInline = isBlockInline;
            isBlockInline = true;

            textWriter.PushPrefix();
            textWriter.Write("function ");
            DumpParametersList(inlineFn.Parameters);
            DumpFunctionBody(inlineFn.Body);
            textWriter.PopPrefix();

            isBlockInline = wasBlockInline;
        }
    }

    public void TranslateVariableRef(VariableRef varRef)
    {
        textWriter.Write(SafeName(varRef.Name));
    }

    public void TranslateItemRef(ItemRef itemRef)
    {
        MayBeParenthesize(itemRef.Owner);
        textWriter.Write('[');
        itemRef.Index.AcceptTranslator(this);
        textWriter.Write(']');
    }

    public void TranslateSliceRef(SliceRef sliceRef)
    {
        MayBeParenthesize(sliceRef.Owner);
        textWriter.Write('[');
        sliceRef.LowerBound?.AcceptTranslator(this);
        textWriter.Write("..");
        sliceRef.UpperBound?.AcceptTranslator(this);
        textWriter.Write(']');
    }

    public void TranslatePropertyRef(PropertyRef propertyRef)
    {
        MayBeParenthesize(propertyRef.Owner);
        if (propertyRef.Optional) textWriter.Write('?');
        textWriter.Write(".{0}", SafeName(propertyRef.PropertyName));
    }

    public void TranslateStaticPropertyRef(StaticPropertyRef staticRef)
    {
        textWriter.Write(SafeName(staticRef.Name));
    }

    public void TranslateSelfReference(SelfReference selfRef)
    {
        textWriter.Write("this");
    }

    public void TranslateFunctionCall(FunctionCall fnCall)
    {
        textWriter.Write(SafeName(fnCall.FunctionName));
        DumpArgumentList(fnCall.Arguments, fnCall.NamedArgs);
    }

    public void TranslateAnonymousCall(AnonymousCall anCall)
    {
        MayBeParenthesize(anCall.FunctionSource);
        DumpArgumentList(anCall.Arguments, anCall.NamedArgs);
    }

    public void TranslateMethodCall(MethodCall methodCall)
    {
        MayBeParenthesize(methodCall.Target);
        if (methodCall.Optional) textWriter.Write('?');
        textWriter.Write(".{0}", SafeName(methodCall.FunctionName));
        DumpArgumentList(methodCall.Arguments, methodCall.NamedArgs);
    }

    public void TranslateStaticMethodCall(StaticMethodCall staticCall)
    {
        textWriter.Write(SafeName(staticCall.Name));
        DumpArgumentList(staticCall.Arguments, staticCall.NamedArgs);
    }

    public void TranslateConstructorCall(ConstructorCall ctorCall)
    {
        textWriter.Write($"new {SafeName(ctorCall.Name)}");
        DumpArgumentList(ctorCall.Arguments, ctorCall.NamedArgs);

        if (ctorCall.PropertySetters == null) return;

        textWriter.Write(' ');
        DumpVariableSetterList(ctorCall.PropertySetters);
    }

    public void TranslateParentMethodCall(ParentMethodCall pmc)
    {
        textWriter.Write($"super::{SafeName(pmc.FunctionName)}");
        DumpArgumentList(pmc.Arguments, pmc.NamedArgs);
    }

    public void TranslateParentConstructorCall(ParentConstructorCall pcc)
    {
        textWriter.WriteLine();
        ++textWriter.Indentation;
        textWriter.Write(" : super");
        --textWriter.Indentation;
        DumpArgumentList(pcc.Arguments, pcc.NamedArgs);
    }

    public void TranslateParentPropertyRef(ParentPropertyRef ppr)
    {
        textWriter.Write($"super::{SafeName(ppr.PropertyName)}");
    }

    public void TranslateParentIndexerRef(ParentIndexerRef pir)
    {
        textWriter.Write("super[");
        pir.Index.AcceptTranslator(this);
        textWriter.Write(']');
    }

    public void TranslateInnerFunctionCall(InnerFunctionCall ifc)
    {
    }

    public void TranslateExternalFunctionCall(ExternalFunctionCall efc)
    {
    }

    public void TranslateTypeVerification(TypeVerification typeVerif)
    {
        MayBeParenthesize(typeVerif.Expression);
        textWriter.Write($" is {SafeTypeName(typeVerif.TypeName)}");
    }

    public void TranslateTypeOfExpression(TypeOfExpression typeOf)
    {
        textWriter.Write($"typeof({SafeTypeName(typeOf.TypeName)})");
    }

    public void TranslateConversion(Conversion conversion)
    {
        textWriter.Write($"({SafeTypeName(conversion.TypeName)}) ");
        MayBeParenthesize(conversion.Expression);
    }

    public void TranslateIfElse(IfElse ifElse)
    {
        textWriter.Write("if (");
        ifElse.Guard.AcceptTranslator(this);
        textWriter.WriteLine(')');

        MayBeIndent(ifElse.Action);

        if (ifElse.AlternativeAction == null) return;

        if (ifElse.AlternativeAction is IfElse)
        {
            textWriter.Write("else ");
            ifElse.AlternativeAction.AcceptTranslator(this);
        }
        else
        {
            textWriter.WriteLine("else");
            MayBeIndent(ifElse.AlternativeAction);
        }
    }

    public void TranslateSwitchBlock(SwitchBlock switchBlock)
    {
        textWriter.Write("switch (");
        switchBlock.Test.AcceptTranslator(this);
        textWriter.WriteLine(')');

        textWriter.WriteLine('{');
        ++textWriter.Indentation;

        int counter = 0;
        while (counter < switchBlock.Statements.Length)
        {
            foreach (CaseLabel caseLabel in switchBlock.Cases)
            {
                if (caseLabel.Address != counter) continue;

                textWriter.Write("case ");
                DumpDataItem(caseLabel.Value);
                textWriter.WriteLine(':');
            }

            if (counter == switchBlock.DefaultCase)
                textWriter.WriteLine("default:");

            foreach (var pair in switchBlock.Labels
                .Where(pair => !pair.Key.StartsWith('@') && pair.Value.Address == counter))
            {
                ++textWriter.Indentation;
                textWriter.WriteLine($"{SafeName(pair.Key)}:");
                --textWriter.Indentation;
            }

            MayBeIndent(switchBlock.Statements[counter]);
            ++counter;
        }

        --textWriter.Indentation;
        textWriter.Write('}');
    }

    public void TranslateForLoop(ForLoop forLoop)
    {
        textWriter.Write("for (");

        if (forLoop.Initializers.Length == 1 && forLoop.Initializers[0] is VariableDecl)
        {
            inForLoopInitializer = true;
            forLoop.Initializers[0].AcceptTranslator(this);
            inForLoopInitializer = false;
        }
        else
        {
            var comma1 = false;
            foreach (var initializer in forLoop.Initializers)
            {
                if (comma1) textWriter.Write(", ");
                initializer.AcceptTranslator(this);
                comma1 = true;
            }
        }

        textWriter.Write("; ");
        forLoop.Guard?.AcceptTranslator(this);
        textWriter.Write("; ");

        var comma2 = false;
        foreach (var updater in forLoop.Incrementers)
        {
            if (comma2) textWriter.Write(", ");
            updater.AcceptTranslator(this);
            comma2 = true;
        }

        textWriter.WriteLine(')');
        MayBeIndent(forLoop.Action);
    }

    public void TranslateForEachLoop(ForEachLoop forEach)
    {
        textWriter.Write("foreach (");
        if (forEach.KeyName != ForEachLoop.DEFAULT_KEY_NAME)
            textWriter.Write($"{SafeName(forEach.KeyName)} => ");
        textWriter.Write($"{SafeName(forEach.ValueName)} in ");
        forEach.Guard.AcceptTranslator(this);
        textWriter.WriteLine(')');
        MayBeIndent(forEach.Action);
    }

    public void TranslateWhileLoop(WhileLoop whileLoop)
    {
        textWriter.Write("while (");
        whileLoop.Guard.AcceptTranslator(this);
        textWriter.WriteLine(')');
        MayBeIndent(whileLoop.Action);
    }

    public void TranslateDoLoop(DoLoop doLoop)
    {
        textWriter.WriteLine("do ");
        MayBeIndent(doLoop.Action);
        textWriter.Write("while (");
        doLoop.Guard.AcceptTranslator(this);
        textWriter.WriteLine(");");
    }

    public void TranslateContinue(Continue _continue)
    {
        textWriter.WriteLine("continue;");
    }

    public void TranslateBreak(Break _break)
    {
        textWriter.WriteLine("break;");
    }

    public void TranslateGoto(Goto _goto)
    {
        textWriter.Write("goto ");
        textWriter.Write(_goto.LabelName.StartsWith('@') ? _goto.LabelName[1..] : SafeName(_goto.LabelName));
        textWriter.WriteLine(';');
    }

    public void TranslateYield(Yield yield)
    {
        textWriter.Write("yield ");
        yield.Expression.AcceptTranslator(this);
        textWriter.WriteLine(';');
    }

    public void TranslateReturn(Return _return)
    {
        textWriter.Write("return");

        if (_return.Expression != null)
        {
            textWriter.Write(' ');
            _return.Expression.AcceptTranslator(this);
        }

        textWriter.WriteLine(';');
    }

    public void TranslateThrow(Throw _throw)
    {
        textWriter.Write("throw ");
        _throw.Expression.AcceptTranslator(this);
        textWriter.WriteLine(';');
    }

    public void TranslateTryCatchFinally(TryCatchFinally tcf)
    {
        textWriter.WriteLine("try");
        tcf.TryBlock.AcceptTranslator(this);

        if (tcf.CatchBlock != null)
        {
            textWriter.WriteLine("catch ({0})", SafeName(tcf.ExceptionName));
            tcf.CatchBlock.AcceptTranslator(this);
        }

        if (tcf.FinallyBlock != null)
        {
            textWriter.WriteLine("finally");
            tcf.FinallyBlock.AcceptTranslator(this);
        }
    }

    public void TranslateStringInterpolation(StringInterpolation stringInt)
    {
        textWriter.Write("$'");

        string pattern = stringInt.Pattern;
        int i = 0, l = pattern.Length, k = 0;

        while (i < l)
        {
            char ch = pattern[i];

            if (ch == '{')
            {
                textWriter.Write(ch);

                if (pattern[i + 1] == ch)
                {
                    textWriter.Write(ch);
                    i += 2;
                }
                else
                {
                    ToggleStringWrapper();
                    stringInt.Substitions[k++].AcceptTranslator(this);
                    ToggleStringWrapper();

                    int j = i + 1;
                    while (",:}".IndexOf(pattern[j]) < 0) ++j;
                    i = j;
                }
            }
            else
            {
                textWriter.Write(EscapedString(ch.ToString(), false));
                ++i;
            }
        }

        textWriter.Write("'");
    }

    public void TranslatePatternMatching(PatternMatching patMatch)
    {
        MayBeParenthesize(patMatch.Expression);

        if (patMatch.IsSimple)
        {
            textWriter.Write(" is ");
            DumpMatchCasePattern(patMatch.MatchCases[0].Pattern);
        }
        else
        {
            textWriter.WriteLine(" switch {");
            ++textWriter.Indentation;

            var firstCase = true;

            foreach (var matchCase in patMatch.MatchCases)
            {
                if (firstCase)
                    firstCase = false;
                else
                    textWriter.WriteLine(',');

                DumpMatchCase(matchCase);
            }

            textWriter.WriteLine();
            --textWriter.Indentation;
            textWriter.Write('}');
        }
    }

    public void TranslateMutableCopy(MutableCopy mutableCopy)
    {
        MayBeParenthesize(mutableCopy.Original);
        textWriter.Write(" with ");
        DumpVariableSetterList(mutableCopy.PropertySetters);
    }

    #endregion

    #region Utility

    private void ToggleStringWrapper()
    {
        stringWrapper = stringWrapper == '\'' ? '"' : '\'';
    }

    private void DumpClass(ClassDefinition classDef)
    {
        if (classDef.Modifier != Modifier.Default)
            textWriter.Write($"{classDef.Modifier} ".ToLower());

        textWriter.Write($"class {SafeName(classDef.ClassName)}");
        if (!string.IsNullOrEmpty(classDef.SuperClassName))
            textWriter.Write($" : {SafeTypeName(classDef.SuperClassName)}");

        textWriter.WriteLine();
        textWriter.WriteLine('{');
        ++textWriter.Indentation;

        if (classDef.Fields.Length > 0)
        {
            foreach (ClassFieldDecl field in classDef.Fields)
                DumpField(field);
            textWriter.WriteLine();
        }

        if (classDef.Constructor != null)
            DumpConstructor(classDef.Constructor);

        if (classDef.Indexer != null)
            DumpProperty(classDef.Indexer);

        foreach (ClassPropertyDecl property in classDef.Properties)
            DumpProperty(property);

        if (classDef.Events.Length > 0)
        {
            foreach (ClassEventDecl _event in classDef.Events)
                DumpEvent(_event);
            textWriter.WriteLine();
        }

        foreach (ClassMethodDecl method in classDef.Methods)
            DumpMethod(method);

        --textWriter.Indentation;
        textWriter.WriteLine('}');
    }

    private void DumpRecord(ClassDefinition recordDef)
    {
        textWriter.Write($"record {SafeName(recordDef.ClassName)}");

        var parameters = recordDef.Constructor?.Parameters ?? [];
        DumpParametersList(parameters);

        var (indexer, fields, methods, events) = (recordDef.Indexer, recordDef.Fields, recordDef.Methods, recordDef.Events);
        var additionalProps = recordDef.Properties
                                       .Where(prop => prop.Name != Class.RECORD_MEMBERS_PROPERTY_NAME &&
                                                      parameters.All(param => param.Name != prop.Name))
                                       .ToArray();

        if (fields.Length + additionalProps.Length + methods.Length + events.Length == 0 && indexer == null)
        {
            textWriter.WriteLine(';');
            return;
        }

        textWriter.WriteLine();
        textWriter.WriteLine('{');
        ++textWriter.Indentation;

        if (fields.Length > 0)
        {
            foreach (ClassFieldDecl field in fields)
                DumpField(field);
            textWriter.WriteLine();
        }

        if (indexer != null) DumpProperty(indexer);

        foreach (ClassPropertyDecl property in additionalProps)
            DumpProperty(property);

        if (events.Length > 0)
        {
            foreach (ClassEventDecl _event in events)
                DumpEvent(_event);
            textWriter.WriteLine();
        }

        foreach (ClassMethodDecl method in methods)
            DumpMethod(method);

        --textWriter.Indentation;
        textWriter.WriteLine('}');
    }

    private void DumpConstructor(ClassMethodDecl constructor)
    {
        textWriter.Write(constructor.Scope.ToString().ToLower());
        textWriter.Write(" constructor");

        DumpParametersList(constructor.Parameters);

        if (constructor.Body.Statements.Length > 0 &&
            constructor.Body.Statements[0] is ParentConstructorCall pcc)
            pcc.AcceptTranslator(this);

        DumpFunctionBody(constructor.Body);
        textWriter.WriteLine();
    }

    private void DumpField(ClassFieldDecl field)
    {
        textWriter.Write(field.Scope.ToString().ToLower());

        switch (field.Modifier)
        {
            case Modifier.Default:
                break;
            case Modifier.StaticFinal:
                textWriter.Write(" static final");
                break;
            default:
                textWriter.Write($" {field.Modifier}".ToLower());
                break;
        }

        textWriter.Write($" {SafeName(field.Name)}");

        if (field.Initializer != null)
        {
            textWriter.Write(" = ");
            field.Initializer.AcceptTranslator(this);
        }

        textWriter.WriteLine(';');
    }

    private void DumpProperty(ClassPropertyDecl property)
    {
        textWriter.Write(property.Scope.ToString().ToLower());

        if (property.Modifier != Modifier.Default)
            textWriter.Write($" {property.Modifier}".ToLower());

        var propertyName = property.IsIndexer ? "[]" : SafeName(property.Name);
        textWriter.Write($" property {propertyName}");

        switch (property.ReaderBody)
        {
            case null when property.WriterBody is null:
                if (property.Access == PropertyAccess.ReadWrite &&
                    property.ReaderScope == property.Scope &&
                    property.WriterScope == property.Scope)
                {
                    textWriter.WriteLine(';');
                }
                else
                {
                    textWriter.Write(" { ");

                    if (property.CanRead)
                        DumpPropertyAccessor(property.Scope, property.ReaderScope, property.ReaderBody,
                                             Parser.PROPERTY_READER_START, true);

                    if (property.CanWrite)
                        DumpPropertyAccessor(property.Scope, property.WriterScope, property.WriterBody,
                                             Parser.PROPERTY_WRITER_START, true);

                    textWriter.WriteLine('}');
                }
                break;
            case { IsExpressionBody: true } when !property.CanWrite:
                textWriter.Write(" => ");
                ((Return)property.ReaderBody.Statements[0]).Expression.AcceptTranslator(this);
                textWriter.WriteLine(';');
                break;
            default:
                textWriter.WriteLine();
                textWriter.WriteLine('{');
                ++textWriter.Indentation;

                if (property.CanRead)
                    DumpPropertyAccessor(property.Scope, property.ReaderScope, property.ReaderBody,
                                         Parser.PROPERTY_READER_START, false);

                if (property.CanWrite)
                    DumpPropertyAccessor(property.Scope, property.WriterScope, property.WriterBody,
                                         Parser.PROPERTY_WRITER_START, false);

                --textWriter.Indentation;
                textWriter.WriteLine('}');
                break;
        }

        textWriter.WriteLine();
    }

    private void DumpPropertyAccessor(Scope propertyScope, Scope accessorScope,
                                      Block accessorBody, string keyword, bool inline)
    {
        if (accessorScope != propertyScope)
            textWriter.Write($"{accessorScope} ".ToLower());

        textWriter.Write(keyword);

        if (accessorBody != null)
            DumpFunctionBody(accessorBody);
        else if (inline)
            textWriter.Write("; ");
        else
            textWriter.WriteLine(';');
    }

    private void DumpMethod(ClassMethodDecl method)
    {
        textWriter.Write(method.Scope.ToString().ToLower());
        if (method.Modifier != Modifier.Default)
            textWriter.Write(" {0}", method.Modifier.ToString().ToLower());

        var unaryOperator = ClassMethod.GetUnaryOperator(method.Name);
        if (unaryOperator != UnaryOperator.None)
            textWriter.Write(" operator {0}", UnaryOperatorToString(unaryOperator));
        else
        {
            var binaryOperator = ClassMethod.GetBinaryOperator(method.Name);
            textWriter.Write(binaryOperator != BinaryOperator.None
                ? $" operator {BinaryOperatorToString(binaryOperator)}"
                : $" function {SafeName(method.Name)}");
        }

        DumpParametersList(method.Parameters);

        if (method.Modifier == Modifier.Abstract)
            textWriter.WriteLine(';');
        else
            DumpFunctionBody(method.Body);

        textWriter.WriteLine();
    }

    private void DumpEvent(ClassEventDecl _event)
    {
        textWriter.Write(_event.Scope.ToString().ToLower());
        if (_event.Modifier != Modifier.Default)
            textWriter.Write($" {_event.Modifier}".ToLower());
        textWriter.Write($" event {SafeName(_event.Name)}");
        DumpParametersList(_event.Parameters);
        textWriter.WriteLine(';');
    }

    private void DumpFunctionBody(Block body)
    {
        if (body.IsExpressionBody)
        {
            textWriter.Write(" => ");
            ((Return)body.Statements[0]).Expression.AcceptTranslator(this);
            textWriter.WriteLine(';');
        }
        else
        {
            var wasFunctionBody = inFunctionBody;
            inFunctionBody = true;
            textWriter.WriteLine();
            body.AcceptTranslator(this);
            inFunctionBody = wasFunctionBody;
        }
    }

    private void DumpParametersList(ParameterDecl[] parameters)
    {
        textWriter.Write('(');

        for (var i = 0; i < parameters.Length; ++i)
        {
            if (i > 0) textWriter.Write(", ");
            DumpParameter(parameters[i]);
        }

        textWriter.Write(')');
    }

    private void DumpParameter(ParameterDecl parameter)
    {
        if (parameter.Attributes != null && parameter.Attributes.Length > 0)
            DumpAttributesList(parameter.Attributes, false);

        if (parameter.ByRef)
            textWriter.Write('&');
        else if (parameter.VaList)
            textWriter.Write("..");

        textWriter.Write(SafeName(parameter.Name));
        if (!parameter.CanBeEmpty) textWriter.Write('!');

        DataItem defVal = parameter.DefaultValue;
        if (defVal == null) return;

        textWriter.Write(" = ");
        DumpDataItem(defVal);
    }

    private void DumpDataItem(DataItem dataItem)
    {
        IFormatProvider fp = CultureInfo.InvariantCulture;

        switch (dataItem.Class.ClassID)
        {
            case ClassID.Void:
                textWriter.Write("null");
                break;
            case ClassID.Boolean:
                textWriter.Write(dataItem.AsBoolean ? Boolean.TRUE_STRING : Boolean.FALSE_STRING);
                break;
            case ClassID.Integer:
                textWriter.Write(dataItem.ToString("g", fp));
                break;
            case ClassID.Long:
                textWriter.Write(dataItem.ToString("g", fp) + "L");
                break;
            case ClassID.Float:
                textWriter.Write(dataItem.ToString("g", fp) + "F");
                break;
            case ClassID.Decimal:
                textWriter.Write(dataItem.ToString(string.Empty, fp) + "D");
                break;
            case ClassID.Date:
                textWriter.Write("`" + dataItem.ToString("u", fp) + "`");
                break;
            case ClassID.String:
                textWriter.Write(stringWrapper + EscapedString(dataItem.ToString(), false) + stringWrapper);
                break;
            case ClassID.Blob:
                textWriter.Write("b" + stringWrapper + EscapedString(dataItem.ToString(), false) + stringWrapper);
                break;
        }
    }

    private void DumpVariableSetterList(VariableSetter[] variableSetters, string prefix = "{", string suffix = "}")
    {
        textWriter.Write(prefix);

        for (var i = 0; i < variableSetters.Length; ++i)
        {
            if (i > 0) textWriter.Write(", ");
            DumpVariableSetter(variableSetters[i]);
        }

        textWriter.Write(suffix);
    }

    private void DumpVariableSetter(VariableSetter variableSetter)
    {
        textWriter.Write(SafeName(variableSetter.Name));

        if (variableSetter.Value == null) return;

        textWriter.Write(" = ");
        variableSetter.Value.AcceptTranslator(this);
    }

    private void DumpArgumentList(Argument[] positionalArgs, Dictionary<string, Expression> namedArgs,
                                  string prefix = "(", string suffix = ")")
    {
        textWriter.Write(prefix);

        bool firstArg = true;

        if (positionalArgs?.Length > 0)
        {
            DumpArgumentList(positionalArgs, string.Empty, string.Empty);
            firstArg = false;
        }

        if (namedArgs != null)
            foreach (var argPair in namedArgs)
            {
                if (firstArg)
                    firstArg = false;
                else
                    textWriter.Write(", ");

                textWriter.Write($"{argPair.Key}: ");
                argPair.Value.AcceptTranslator(this);
            }

        textWriter.Write(suffix);
    }

    private void DumpArgumentList(Argument[] arguments, string prefix = "[", string suffix = "]")
    {
        textWriter.Write(prefix);

        for (var i = 0; i < arguments.Length; ++i)
        {
            if (i > 0) textWriter.Write(", ");
            DumpArgument(arguments[i]);
        }

        textWriter.Write(suffix);
    }

    private void DumpArgument(Argument argument)
    {
        if (argument.Spread) textWriter.Write(".. ");
        argument.Value.AcceptTranslator(this);
    }

    private void DumpMapEntryList(MapEntry[] mapEntries, string prefix = "{", string suffix = "}")
    {
        textWriter.Write(prefix);

        if (mapEntries.Length > 0)
            for (var i = 0; i < mapEntries.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                DumpMapEntry(mapEntries[i]);
            }
        else
            textWriter.Write("=>");

        textWriter.Write(suffix);
    }

    private void DumpMapEntry(MapEntry entry)
    {
        entry.Key.AcceptTranslator(this);
        textWriter.Write(" => ");
        entry.Value.AcceptTranslator(this);
    }

    private void DumpAttributesList(AttributeDecl[] attributes, bool multiline)
    {
        bool wrapLines = multiline & attributes.Length > 1;

        if (wrapLines)
        {
            textWriter.WriteLine('[');
            ++textWriter.Indentation;
        }
        else
            textWriter.Write('[');

        for (var i = 0; i < attributes.Length; ++i)
        {
            if (i > 0) textWriter.WriteLine(", ");
            DumpAttribute(attributes[i]);
        }

        if (wrapLines)
        {
            --textWriter.Indentation;
            textWriter.WriteLine();
            textWriter.WriteLine(']');
        }
        else if (multiline)
            textWriter.WriteLine(']');
        else
            textWriter.Write("] ");
    }

    private void DumpAttribute(AttributeDecl attribute)
    {
        textWriter.Write(SafeName(attribute.Name));
        DumpVariableSetterList(attribute.Fields, "(", ")");
    }

    private void DumpMatchCase(MatchCase matchCase)
    {
        DumpMatchCasePattern(matchCase.Pattern);
        if (matchCase.Guard != null)
        {
            textWriter.Write(" when ");
            matchCase.Guard.AcceptTranslator(this);
        }
        textWriter.Write(" => ");
        DumpMatchExpression(matchCase.Expression);
    }

    private void DumpMatchCasePattern(Pattern pattern)
    {
        switch (pattern)
        {
            case AlwaysTruePattern:
                textWriter.Write(AlwaysTruePattern.Symbol);
                break;
            case RelationalPattern relational:
                if (relational.Operator is not (BinaryOperator.Equal or BinaryOperator.Identical))
                    textWriter.Write($"{BinaryOperatorToString(relational.Operator)} ");
                DumpDataItem(relational.Value);
                break;
            case ObjectPattern objectPat:
            {
                // We check ObjectPattern before TypePattern to avoid problems with inheritance!!
                if (Equals(objectPat.TypeName, Class.Object.Name))
                {
                    textWriter.Write(objectPat.TypeName);
                    textWriter.Write(' ');
                }

                textWriter.Write("{ ");

                var firstProp = true;
                foreach (var matcher in objectPat.PropertyMatchers)
                {
                    if (firstProp)
                        firstProp = false;
                    else
                        textWriter.Write(", ");

                    textWriter.Write(string.Join('.', matcher.Path));
                    textWriter.Write(" : ");
                    DumpMatchCasePattern(matcher.Pattern);
                }

                textWriter.Write(" }");
                break;
            }
            case DestructuringPattern destructuring:
            {
                // We check DestructuringPattern before TypePattern to avoid problems with inheritance!!
                textWriter.Write(destructuring.TypeName);
                textWriter.Write('(');

                var firstElem = true;
                foreach (var propName in destructuring.PropertyNames)
                {
                    if (firstElem)
                        firstElem = false;
                    else
                        textWriter.Write(", ");

                    textWriter.Write(propName);
                }

                textWriter.Write(')');
                break;
            }
            case TypePattern type:
                textWriter.Write(type.TypeName);
                break;
            case NegativePattern negative:
                textWriter.Write("not ");
                DumpMatchCasePattern(negative.Child);
                break;
            case GroupingPattern grouping:
                textWriter.Write('(');
                DumpMatchCasePattern(grouping.Child);
                textWriter.Write(')');
                break;
            case LogicalPattern logical:
                DumpMatchCasePattern(logical.Left);
                textWriter.Write(logical.Inclusive ? " and " : " or ");
                DumpMatchCasePattern(logical.Right);
                break;
            case PositionalPattern positional:
            {
                textWriter.Write('(');

                var firstElem = true;
                foreach (var item in positional.Items)
                {
                    if (firstElem)
                        firstElem = false;
                    else
                        textWriter.Write(", ");

                    DumpMatchCasePattern(item);
                }

                textWriter.Write(')');
                break;
            }
            case StringDestructuringPattern stringDest:
            {
                var regex = StringUtil.ToString(stringDest.Regex)
                                      .Replace(@"(?<", @"{")
                                      .Replace(@">.+)", @"}")
                                      .Replace(@"\k<", @"{")
                                      .Replace(@">", @"}");

                textWriter.Write($"$'{regex}'");
                break;
            }
        }
    }

    private void DumpMatchExpression(Expression expression)
    {
        if (expression is ThrowExpression throwExpr)
        {
            textWriter.Write("throw ");
            throwExpr.Throw.Expression.AcceptTranslator(this);
        }
        else
            expression.AcceptTranslator(this);
    }

    private void MayBeIndent(Statement statement)
    {
        if (statement.GetType() == typeof(Block))
            statement.AcceptTranslator(this);
        else
        {
            ++textWriter.Indentation;
            statement.AcceptTranslator(this);
            if (statement is Expression) textWriter.WriteLine(';');
            --textWriter.Indentation;
        }
    }

    private void MayBeParenthesize(Expression expr)
    {
        if (expr.IsParenthesized)
        {
            textWriter.Write('(');
            expr.AcceptTranslator(this);
            textWriter.Write(')');
        }
        else
            expr.AcceptTranslator(this);
    }

    #endregion

    #region Static Methods

    public static bool IsTypeName(string name)
    {
        return Array.BinarySearch(typeNames, name) >= 0;
    }

    public static bool IsKeyword(string name)
    {
        return Array.BinarySearch(keywords, name) >= 0;
    }

    public static string EscapedIdentifier(string original)
    {
        var sb = new StringBuilder();

        foreach (var ch in original)
        {
            if (Lexer.IsLegalIdChar(ch))
                sb.Append(ch);
            else
                sb.Append("\\u").Append(((ushort)ch).ToString("x4"));
        }

        return sb.ToString();
    }

    public static string EscapedString(string original, bool duplicateBraces)
    {
        StringBuilder sb = new ();

        foreach (var c in original)
            switch (c)
            {
                case '\\' or '\'' or '"':
                    sb.Append('\\').Append(c);
                    break;
                case '\a':
                    sb.Append("\\a");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\v':
                    sb.Append("\\v");
                    break;
                case '{':
                    sb.Append(duplicateBraces ? "{{" : "{");
                    break;
                case '}':
                    sb.Append(duplicateBraces ? "}}" : "}");
                    break;
                case >= (char)32 and < (char)127:
                    sb.Append(c);
                    break;
                case <= (char)255:
                    sb.Append($"\\x{(int)c:x2}");
                    break;
                default:
                    sb.Append($"\\u{(int)c:x4}");
                    break;
            }

        return sb.ToString();
    }

    public static string SafeTypeName(string name)
    {
        return IsKeyword(name) || !Lexer.IsLegalFirstIdChar(name[0])
             ? "$" + EscapedIdentifier(name)
             : EscapedIdentifier(name);
    }

    public static string SafeName(string name)
    {
        return IsTypeName(name) || IsKeyword(name) || !Lexer.IsLegalFirstIdChar(name[0])
             ? "$" + EscapedIdentifier(name)
             : EscapedIdentifier(name);
    }

    public static string SafeName(QualifiedName name)
    {
        StringBuilder sb = new ();

        for (var i = 0; i < name.Length; ++i)
        {
            if (i > 0) sb.Append("::");

            NamePart part = name[i];
            sb.Append(SafeName(part.Value));

            if (part.ParamCount == 0) continue;

            sb.Append('{').Append(part.ParamCount).Append('}');
        }

        return sb.ToString();
    }

    public static string UnaryOperatorToString(UnaryOperator oper)
    {
        return oper switch
        {
            UnaryOperator.Plus => "+",
            UnaryOperator.Minus => "-",
            UnaryOperator.Not or UnaryOperator.NotEmpty => "!",
            UnaryOperator.BitwiseNot => "~",
            UnaryOperator.PreIncrement or UnaryOperator.PostIncrement => "++",
            UnaryOperator.PreDecrement or UnaryOperator.PostDecrement => "--",
            _ => string.Empty,
        };
    }

    public static string BinaryOperatorToString(BinaryOperator oper)
    {
        return oper switch
        {
            BinaryOperator.Plus => "+",
            BinaryOperator.Minus => "-",
            BinaryOperator.Times => "*",
            BinaryOperator.Divide => "/",
            BinaryOperator.Modulo => "%",
            BinaryOperator.Power => "**",
            BinaryOperator.ShiftLeft => "<<",
            BinaryOperator.ShiftRight => ">>",
            BinaryOperator.LessThan => "<",
            BinaryOperator.LessThanOrEqual => "<=",
            BinaryOperator.GreaterThan => ">",
            BinaryOperator.GreaterThanOrEqual => ">=",
            BinaryOperator.Equal => "==",
            BinaryOperator.NotEqual => "!=",
            BinaryOperator.Identical => "===",
            BinaryOperator.NotIdentical => "!==",
            BinaryOperator.And => "&",
            BinaryOperator.AndAlso => "&&",
            BinaryOperator.Or => "|",
            BinaryOperator.OrElse => "||",
            BinaryOperator.ExclusiveOr => "^",
            BinaryOperator.StartsWith => "startswith",
            BinaryOperator.EndsWith => "endswith",
            BinaryOperator.Contains => "contains",
            BinaryOperator.Matches => "matches",
            BinaryOperator.IfEmpty => "??",
            _ => string.Empty,
        };
    }

    #endregion
}