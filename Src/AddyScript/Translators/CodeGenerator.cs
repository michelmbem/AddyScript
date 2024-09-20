using System;
using System.Globalization;
using System.IO;
using System.Text;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Translators.Utility;
using AddyScript.Parsers;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;
using System.Collections.Generic;


namespace AddyScript.Translators
{
    public class CodeGenerator(TextWriter textWriter) : ITranslator
    {
        #region Keywords

        private static readonly string[] typeNames = {
                                                         "bool", "closure", "complex", "date", "decimal",
                                                         "float", "int", "list", "long", "map", "object", "queue",
                                                         "rational", "resource", "set", "stack", "string", "void"
                                                     };

        private static readonly string[] keywords = {
                                                        "abstract", "break", "case", "catch",
                                                        "class", "const", "constructor",
                                                        "contains", "continue", "default",
                                                        "do", "else", "endswith", "extern", "event",
                                                        "false", "final", "finally", "for",
                                                        "foreach", "function", "goto", "if",
                                                        "import", "in", "is", "local", "matches",
                                                        "new", "null", "params", "private", "property",
                                                        "protected", "public", "ref", "return",
                                                        "startswith", "static", "super", "switch",
                                                        "this", "throw", "true", "try", "typeof",
                                                        "using", "while"
                                                    };

        #endregion

        private readonly IndentedTextWriter textWriter = textWriter is IndentedTextWriter itw ? itw : new IndentedTextWriter(textWriter);
        private bool inFunctionBody, inForLoopInitializer, isBlockInline;
        private char stringWrapper = '\'';

        #region Members of ITranslator

        public void TranslateProgram(Program program)
        {
            Statement prevStatement = null;
            int counter = 0;

            while (counter < program.Statements.Length)
            {
                foreach (var pair in program.Labels)
                    if (pair.Value.Address == counter)
                        textWriter.WriteLine("{0}:", SafeName(pair.Key));

                Statement statement = program.Statements[counter];
                if (prevStatement != null && (
                    statement is ClassDefinition || prevStatement is ClassDefinition ||
                    statement is FunctionDecl || prevStatement is FunctionDecl ||
                    (statement is ImportDirective ^ prevStatement is ImportDirective)))
                    textWriter.WriteLine();

                statement.AcceptTranslator(this);
                if (statement is Expression) textWriter.WriteLine(';');
                prevStatement = statement;
                ++counter;
            }
        }

        public void TranslateImportDirective(ImportDirective import)
        {
            if (string.IsNullOrEmpty(import.Alias))
                textWriter.WriteLine("import {0};", SafeName(import.ModuleName));
            else
                textWriter.WriteLine("import {0} as {1};", SafeName(import.ModuleName), SafeName(import.Alias));
        }

        public void TranslateClassDefinition(ClassDefinition classDef)
        {
            if (classDef.Attributes != null && classDef.Attributes.Length > 0)
                DumpAttributesList(classDef.Attributes, true);

            if (classDef.Modifier != Modifier.Default)
                textWriter.Write("{0} ", classDef.Modifier.ToString().ToLower());

            textWriter.Write("class {0}", SafeName(classDef.ClassName));
            if (!string.IsNullOrEmpty(classDef.SuperClassName))
                textWriter.Write(" : {0}", SafeTypeName(classDef.SuperClassName));

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
            textWriter.Write('}');
        }

        public void TranslateFunctionDecl(FunctionDecl fnDecl)
        {
            if (fnDecl.Attributes != null && fnDecl.Attributes.Length > 0)
                DumpAttributesList(fnDecl.Attributes, true);

            textWriter.Write("function {0}", SafeName(fnDecl.Name));
            DumpParametersList(fnDecl.Parameters);
            DumpFunctionBody(fnDecl.Body);
        }

        public void TranslateExternalFunctionDecl(ExternalFunctionDecl extDecl)
        {
            if (extDecl.Attributes != null && extDecl.Attributes.Length > 0)
                DumpAttributesList(extDecl.Attributes, true);

            textWriter.Write("extern function {0}", SafeName(extDecl.Name));
            DumpParametersList(extDecl.Parameters);
            textWriter.WriteLine(';');
        }

        public void TranslateConstantDecl(ConstantDecl cstDecl)
        {
            textWriter.Write("const ");

            for (int i = 0; i < cstDecl.Initializers.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                DumpPropertyInitializer(cstDecl.Initializers[i]);
            }

            textWriter.WriteLine(';');
        }

        public void TranslateVariableDecl(VariableDecl varDecl)
        {
            textWriter.Write("var ");

            for (int i = 0; i < varDecl.Initializers.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                DumpPropertyInitializer(varDecl.Initializers[i]);
            }

            if (!inForLoopInitializer) textWriter.WriteLine(';');
        }

        public void TranslateBlock(Block block)
        {
            textWriter.WriteLine('{');
            ++textWriter.Indentation;

            bool wasFunctionBody = inFunctionBody;
            inFunctionBody = false;
            bool wasBlockInline = isBlockInline;
            isBlockInline = false;

            int counter = 0;
            int length = block.Statements.Length;
            
            if (wasFunctionBody && length > 0 && block.Statements[length - 1] is Return ret && ret.Expression == null)
                --length;

            while (counter < length)
            {
                if (block.Statements[counter] is ParentConstructorCall)
                {
                    ++counter;
                    continue;
                }

                foreach (var pair in block.Labels)
                    if (pair.Value.Address == counter)
                        textWriter.WriteLine("{0}:", SafeName(pair.Key));

                Statement stmt = block.Statements[counter];
                stmt.AcceptTranslator(this);
                if (stmt is Expression) textWriter.WriteLine(';');
                ++counter;
            }

            --textWriter.Indentation;
            textWriter.Write("}");
            if (!wasBlockInline) textWriter.WriteLine();

            isBlockInline = wasBlockInline;
            inFunctionBody = wasFunctionBody;
        }

        public void TranslateAssignment(Assignment assignment)
        {
            assignment.LeftOperand.AcceptTranslator(this);
            if (assignment.Operator == BinaryOperator.IfEmpty)
                textWriter.Write(" ?? ");
            else
                textWriter.Write(" {0}= ", BinaryOperatorToString(assignment.Operator));
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

        public void TranslateBinaryExpression(BinaryExpression binaryExpr)
        {
            MayBeParenthesize(binaryExpr.LeftOperand);
            textWriter.Write(" {0} ", BinaryOperatorToString(binaryExpr.Operator));
            MayBeParenthesize(binaryExpr.RightOperand);
        }

        public void TranslateUnaryExpression(UnaryExpression unExpr)
        {
            switch (unExpr.Operator)
            {
                case UnaryOperator.NotEmpty:
                case UnaryOperator.PostIncrement:
                case UnaryOperator.PostDecrement:
                    MayBeParenthesize(unExpr.Operand);
                    textWriter.Write(UnaryOperatorToString(unExpr.Operator));
                    break;
                default:
                    textWriter.Write(UnaryOperatorToString(unExpr.Operator));
                    MayBeParenthesize(unExpr.Operand);
                    break;
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

        public void TranslateListInitializer(ListInitializer listInit)
        {
            DumpExpressionsList(listInit.Items, "[", "]");
        }

        public void TranslateMapInitializer(MapInitializer mapInit)
        {
            DumpMapItemInitializersList(mapInit.ItemInitializers);
        }

        public void TranslateSetInitializer(SetInitializer setInit)
        {
            DumpExpressionsList(setInit.Items, "{", "}");
        }

        public void TranslateObjectInitializer(ObjectInitializer objInit)
        {
            textWriter.Write("new ");
            DumpPropertyInitializersList(objInit.PropertyInitializers);
        }

        public void TranslateInlineFunction(InlineFunction inlineFn)
        {
            if (inlineFn.IsLambda())
            {
                ParameterDecl[] parameters = inlineFn.Parameters;

                textWriter.Write("|");
                if (parameters.Length <= 0)
                    textWriter.Write(' ');
                else
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        if (i > 0) textWriter.Write(", ");
                        textWriter.Write(SafeName(parameters[i].Name));
                    }
                textWriter.Write("| => ");

                var _return = (Return)inlineFn.Body.Statements[0];
                _return.Expression.AcceptTranslator(this);
            }
            else
            {
                bool wasBlockInline = isBlockInline;
                isBlockInline = true;

                textWriter.PushPrefix();
                textWriter.Write("function ");
                DumpParametersList(inlineFn.Parameters);
                DumpFunctionBody(inlineFn.Body);
                textWriter.PopPrefix();

                isBlockInline = wasBlockInline;
            }
        }

        public void TranslateVariableRef(VariableRef variableRef)
        {
            textWriter.Write(SafeName(variableRef.Name));
        }

        public void TranslateItemRef(ItemRef itemRef)
        {
            MayBeParenthesize(itemRef.Owner);
            textWriter.Write('[');
            itemRef.Index.AcceptTranslator(this);
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
            DumpArgumentsList(fnCall.Arguments, fnCall.NamedArgs);
        }

        public void TranslateAnonymousCall(AnonymousCall anCall)
        {
            MayBeParenthesize(anCall.Callee);
            DumpArgumentsList(anCall.Arguments, anCall.NamedArgs);
        }

        public void TranslateMethodCall(MethodCall methodCall)
        {
            MayBeParenthesize(methodCall.Target);
            if (methodCall.Optional) textWriter.Write('?');
            textWriter.Write(".{0}", SafeName(methodCall.FunctionName));
            DumpArgumentsList(methodCall.Arguments, methodCall.NamedArgs);
        }

        public void TranslateStaticMethodCall(StaticMethodCall staticCall)
        {
            textWriter.Write(SafeName(staticCall.Name));
            DumpArgumentsList(staticCall.Arguments, staticCall.NamedArgs);
        }

        public void TranslateConstructorCall(ConstructorCall ctorCall)
        {
            textWriter.Write("new {0}", SafeName(ctorCall.Name));
            DumpArgumentsList(ctorCall.Arguments, ctorCall.NamedArgs);

            if (ctorCall.PropertyInitializers == null) return;

            textWriter.Write(' ');
            DumpPropertyInitializersList(ctorCall.PropertyInitializers);
        }

        public void TranslateParentMethodCall(ParentMethodCall pmc)
        {
            textWriter.Write("super::{0}", SafeName(pmc.FunctionName));
            DumpArgumentsList(pmc.Arguments, pmc.NamedArgs);
        }

        public void TranslateParentConstructorCall(ParentConstructorCall pcc)
        {
            textWriter.WriteLine();
            ++textWriter.Indentation;
            textWriter.Write(" : super");
            --textWriter.Indentation;
            DumpArgumentsList(pcc.Arguments, pcc.NamedArgs);
        }

        public void TranslateParentPropertyRef(ParentPropertyRef ppr)
        {
            textWriter.Write("super::{0}", SafeName(ppr.PropertyName));
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
            textWriter.Write(" is {0}", SafeTypeName(typeVerif.TypeName));
        }

        public void TranslateTypeOfExpression(TypeOfExpression typeOf)
        {
            textWriter.Write("typeof({0})", SafeTypeName(typeOf.TypeName));
        }

        public void TranslateConversion(Conversion conversion)
        {
            textWriter.Write("({0}) ", SafeTypeName(conversion.TypeName));
            MayBeParenthesize(conversion.Expression);
        }

        public void TranslateIfElse(IfElse ifElse)
        {
            textWriter.Write("if (");
            ifElse.Condition.AcceptTranslator(this);
            textWriter.WriteLine(')');

            MayBeIndent(ifElse.PositiveAction);

            if (ifElse.NegativeAction == null) return;

            if (ifElse.NegativeAction is IfElse)
            {
                textWriter.Write("else ");
                ifElse.NegativeAction.AcceptTranslator(this);
            }
            else
            {
                textWriter.WriteLine("else");
                MayBeIndent(ifElse.NegativeAction);
            }
        }

        public void TranslateSwitchBlock(SwitchBlock switchBlock)
        {
            textWriter.Write("switch (");
            switchBlock.Expression.AcceptTranslator(this);
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

                foreach (var pair in switchBlock.Labels)
                {
                    if (pair.Value.Address != counter) continue;

                    ++textWriter.Indentation;
                    textWriter.WriteLine("{0}:", SafeName(pair.Key));
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
                bool comma1 = false;
                foreach (Statement initializer in forLoop.Initializers)
                {
                    if (comma1) textWriter.Write(", ");
                    initializer.AcceptTranslator(this);
                    comma1 = true;
                }
            }

            textWriter.Write("; ");
            forLoop.Guard?.AcceptTranslator(this);
            textWriter.Write("; ");

            bool comma2 = false;
            foreach (Expression updater in forLoop.Updaters)
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
                textWriter.Write("{0} => ", SafeName(forEach.KeyName));
            textWriter.Write("{0} in ", SafeName(forEach.ValueName));
            forEach.Enumerated.AcceptTranslator(this);
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
            textWriter.WriteLine("goto {0};", SafeName(_goto.LabelName));
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

            textWriter.WriteLine(" switch {");
            ++textWriter.Indentation;

            foreach (MatchCase matchCase in patMatch.MatchCases)
                DumpMatchCase(matchCase);

            --textWriter.Indentation;
            textWriter.Write('}');
        }

        public void TranslateAlteredCopy(AlteredCopy altCopy)
        {
            MayBeParenthesize(altCopy.Original);
            textWriter.Write(" with ");
            DumpPropertyInitializersList(altCopy.PropertyInitializers);
        }

        #endregion

        #region Utility

        private void ToggleStringWrapper()
        {
            stringWrapper = stringWrapper == '\'' ? '"' : '\'';
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
                    textWriter.Write(" {0}", field.Modifier.ToString().ToLower());
                    break;
            }
            
            textWriter.Write(" {0}", SafeName(field.Name));

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
                textWriter.Write(" {0}", property.Modifier.ToString().ToLower());

            textWriter.Write(" property {0}", property.IsIndexer ? "[]" : SafeName(property.Name));

            if (property.IsAuto || property.Modifier == Modifier.Abstract)
                DumpAutoProperty(property);
            else
                DumpExpandedProperty(property);

            textWriter.WriteLine();
        }

        private void DumpAutoProperty(ClassPropertyDecl property)
        {
            if (property.Access == PropertyAccess.ReadWrite && property.ReaderScope == property.WriterScope)
                textWriter.WriteLine(';');
            else
            {
                textWriter.Write(" {");

                if ((property.Access & PropertyAccess.Read) != PropertyAccess.None)
                {
                    if (property.ReaderScope != property.Scope)
                        textWriter.Write(" {0}", property.ReaderScope.ToString().ToLower());
                    textWriter.Write(" read;");
                }

                if ((property.Access & PropertyAccess.Write) != PropertyAccess.None)
                {
                    if (property.WriterScope != property.Scope)
                        textWriter.Write(" {0}", property.WriterScope.ToString().ToLower());
                    textWriter.Write(" write;");
                }

                textWriter.WriteLine(" }");
            }
        }

        private void DumpExpandedProperty(ClassPropertyDecl property)
        {
            if (property.CanRead && !property.CanWrite && property.ReaderBody.Statements.Length == 1 &&
                property.ReaderBody.Statements[0] is Return ret && ret.Expression != null)
            {
                textWriter.Write(" => ");
                ret.Expression.AcceptTranslator(this);
                textWriter.WriteLine(';');
            }
            else
            {
                textWriter.WriteLine();
                textWriter.WriteLine('{');
                ++textWriter.Indentation;

                if (property.CanRead)
                {
                    if (property.ReaderScope != property.Scope)
                        textWriter.Write("{0} ", property.ReaderScope.ToString().ToLower());

                    textWriter.Write("read");
                    DumpFunctionBody(property.ReaderBody);
                }

                if (property.CanWrite)
                {
                    if (property.WriterScope != property.Scope)
                        textWriter.Write("{0} ", property.WriterScope.ToString().ToLower());

                    textWriter.Write("write");
                    DumpFunctionBody(property.WriterBody);
                }

                --textWriter.Indentation;
                textWriter.Write('}');
            }
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
                if (binaryOperator != BinaryOperator.None)
                    textWriter.Write(" operator {0}", BinaryOperatorToString(binaryOperator));
                else
                    textWriter.Write(" function {0}", SafeName(method.Name));
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
                textWriter.Write(" {0}", _event.Modifier.ToString().ToLower());
            textWriter.Write(" event {0}", SafeName(_event.Name));
            DumpParametersList(_event.Parameters);
            textWriter.WriteLine(';');
        }

        private void DumpFunctionBody(Block body)
        {
            if (body.Statements.Length == 1 && body.Statements[0] is Return ret &&
                ret.Expression != null)
            {
                textWriter.Write(" => ");
                ret.Expression.AcceptTranslator(this);
                textWriter.WriteLine(';');
            }
            else
            {
                bool wasFunctionBody = inFunctionBody;
                inFunctionBody = true;
                textWriter.WriteLine();
                body.AcceptTranslator(this);
                inFunctionBody = wasFunctionBody;
            }
        }

        private void DumpParametersList(ParameterDecl[] parameters)
        {
            textWriter.Write('(');
            
            for (int i = 0; i < parameters.Length; ++i)
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
                textWriter.Write("ref ");
            else if (parameter.VaArgs)
                textWriter.Write("params ");

            textWriter.Write(SafeName(parameter.Name));

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
                    textWriter.Write(dataItem.AsBoolean ? "true" : "false");
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
                    textWriter.Write(dataItem.ToString("", fp) + "D");
                    break;
                case ClassID.Date:
                    textWriter.Write("`" + dataItem.ToString("u", fp) + "`");
                    break;
                case ClassID.String:
                    textWriter.Write(stringWrapper + EscapedString(dataItem.ToString(), false) + stringWrapper);
                    break;
            }
        }

        private void DumpArgumentsList(Expression[] positionalArgs, Dictionary<string, Expression> namedArgs,
                                       string prefix = "(", string suffix = ")")
        {
            textWriter.Write(prefix);

            bool firstArg = true;

            if (positionalArgs != null)
                foreach (Expression arg in positionalArgs)
                {
                    if (firstArg)
                        firstArg = false;
                    else
                        textWriter.Write(", ");

                    arg.AcceptTranslator(this);
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

        private void DumpExpressionsList(Expression[] expressions, string prefix = "(", string suffix = ")")
        {
            textWriter.Write(prefix);

            for (int i = 0; i < expressions.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                expressions[i].AcceptTranslator(this);
            }

            textWriter.Write(suffix);
        }

        private void DumpPropertyInitializersList(PropertyInitializer[] propertyInitializers, string prefix = "{", string suffix = "}")
        {
            textWriter.Write(prefix);

            for (int i = 0; i < propertyInitializers.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                DumpPropertyInitializer(propertyInitializers[i]);
            }

            textWriter.Write(suffix);
        }

        private void DumpPropertyInitializer(PropertyInitializer propertyInitializer)
        {
            textWriter.Write(SafeName(propertyInitializer.Name));

            if (propertyInitializer.Expression != null)
            {
                textWriter.Write(" = ");
                propertyInitializer.Expression.AcceptTranslator(this);
            }
        }

        private void DumpMapItemInitializersList(MapItemInitializer[] itemInitializers, string prefix = "{", string suffix = "}")
        {
            textWriter.Write(prefix);

            if (itemInitializers.Length > 0)
                for (int i = 0; i < itemInitializers.Length; ++i)
                {
                    if (i > 0) textWriter.Write(", ");
                    DumpMapItemInitializer(itemInitializers[i]);
                }
            else
                textWriter.Write("=>");

            textWriter.Write(suffix);
        }

        private void DumpMapItemInitializer(MapItemInitializer itemInitializer)
        {
            itemInitializer.Key.AcceptTranslator(this);
            textWriter.Write(" => ");
            itemInitializer.Value.AcceptTranslator(this);
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

            for (int i = 0; i < attributes.Length; ++i)
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
            DumpPropertyInitializersList(attribute.PropertyInitializers, "(", ")");
        }

        private void DumpMatchCase(MatchCase matchCase)
        {
            DumpMatchCasePattern(matchCase.Pattern);
            textWriter.Write(" => ");
            DumpMatchExpression(matchCase.Expression);
        }

        private void DumpMatchCasePattern(Pattern pattern)
        {
            if (pattern is AlwaysPattern)
                textWriter.Write(AlwaysPattern.Symbol);
            else if (pattern is NullPattern)
                textWriter.Write(NullPattern.Symbol);
            else if (pattern is ValuePattern valuePat)
                DumpDataItem(valuePat.Value);
            else if (pattern is RangePattern rangePat)
            {
                if (rangePat.LowerBound != null) DumpDataItem(rangePat.LowerBound);
                textWriter.Write("..");
                if (rangePat.UpperBound != null) DumpDataItem(rangePat.UpperBound);
            }
            else if (pattern is ObjectPattern objectPat)
            {
                // We check ObjectPattern before TypePattern to avoid problems with inheritance!!
                if (objectPat.TypeName != Class.Object.Name)
                {
                    textWriter.Write(objectPat.TypeName);
                    textWriter.Write(' ');
                }

                textWriter.Write("{ ");

                bool firstProp = true;
                foreach (var exampleProp in objectPat.Example.AsDynamicObject)
                {
                    if (firstProp)
                        firstProp = false;
                    else
                        textWriter.Write(", ");

                    textWriter.Write(exampleProp.Key);
                    textWriter.Write(" = ");
                    DumpDataItem(exampleProp.Value);
                }

                textWriter.Write(" }");
            }
            else if (pattern is TypePattern typePat)
                textWriter.Write(typePat.TypeName);
            else if (pattern is PredicatePattern predPat)
            {
                textWriter.Write(predPat.ParameterName);
                textWriter.Write(": ");
                predPat.Predicate.AcceptTranslator(this);
            }
            else if (pattern is CompositePattern compPat)
            {
                DumpMatchCasePattern(compPat.Components[0]);

                for (int i = 1; i < compPat.Components.Length; ++i)
                {
                    textWriter.Write(", ");
                    DumpMatchCasePattern(compPat.Components[i]);
                }
            }
        }

        private void DumpMatchExpression(Expression expression)
        {
            if (expression is AnonymousCall anoCall)
            {
                Block block = ((InlineFunction)anoCall.Callee).Body;

                if (block.Statements.Length == 1 && block.Statements[0] is Throw _throw)
                    _throw.AcceptTranslator(this);
                else
                {
                    bool wasFunctionBody = inFunctionBody;
                    bool wasBlockInline = isBlockInline;

                    inFunctionBody = isBlockInline = true;
                    block.AcceptTranslator(this);
                    textWriter.WriteLine();

                    inFunctionBody = wasFunctionBody;
                    isBlockInline = wasBlockInline;
                }
            }
            else
            {
                expression.AcceptTranslator(this);
                textWriter.WriteLine(';');
            }
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

            foreach (char ch in original)
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
            string escaped = original.Replace("\\", "\\\\").
                                      Replace("'", "\\'").
                                      Replace("\"", "\\\"").
                                      Replace("\a", "\\a").
                                      Replace("\b", "\\b").
                                      Replace("\r", "\\r").
                                      Replace("\n", "\\n").
                                      Replace("\t", "\\t").
                                      Replace("\v", "\\v").
                                      Replace("\f", "\\f");

            return duplicateBraces ? escaped.Replace("{", "{{").Replace("}", "}}") : escaped;
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
            var sb = new StringBuilder();

            for (int i = 0; i < name.Length; ++i)
            {
                if (i > 0) sb.Append("::");
                
                NamePart part = name[i];

                sb.Append(SafeName(part.Value));

                if (part.ParamCount > 0)
                {
                    sb.Append('{');
                    sb.Append(part.ParamCount);
                    sb.Append('}');
                }
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
}