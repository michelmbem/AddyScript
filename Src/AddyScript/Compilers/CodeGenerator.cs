#region 'using' Directives

using System;
using System.Globalization;
using System.IO;
using System.Text;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Compilers.Utility;
using AddyScript.Parsers;
using AddyScript.Runtime;
using AddyScript.Runtime.Dynamics;
using Attribute = AddyScript.Runtime.Attribute;

#endregion


namespace AddyScript.Compilers
{
    public class CodeGenerator : ICompiler
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

        private readonly IndentedTextWriter textWriter;
        private bool inFunctionBody, isBlockInline;

        public CodeGenerator(TextWriter textWriter)
        {
            this.textWriter = textWriter is IndentedTextWriter
                            ? (IndentedTextWriter) textWriter
                            : new IndentedTextWriter(textWriter);
        }

        #region ICompiler Members

        public void CompileProgram(Program program)
        {
            AstNode prevNode = null;
            int counter = 0;

            while (counter < program.Statements.Length)
            {
                foreach (var pair in program.Labels)
                    if (pair.Value.Address == counter)
                        textWriter.WriteLine("{0}:", SafeName(pair.Key));

                AstNode node = program.Statements[counter];
                if (prevNode != null && (
                    node is ClassDefinition || prevNode is ClassDefinition ||
                    node is FunctionDecl || prevNode is FunctionDecl ||
                    (node is ImportDirective ^ prevNode is ImportDirective)))
                    textWriter.WriteLine();

                node.AcceptCompiler(this);
                if (node is Expression) textWriter.WriteLine(";");
                prevNode = node;
                ++counter;
            }
        }

        public void CompileImportDirective(ImportDirective import)
        {
            if (string.IsNullOrEmpty(import.Alias))
                textWriter.WriteLine("import {0};", SafeName(import.ModuleName));
            else
                textWriter.WriteLine("import {0} as {1};", SafeName(import.ModuleName), SafeName(import.Alias));
        }

        public void CompileClassDefinition(ClassDefinition classDef)
        {
            if (classDef.Attributes != null && classDef.Attributes.Length > 0)
                DumpAttributesList(classDef.Attributes, true);

            if (classDef.Modifier != Modifier.Default)
                textWriter.Write("{0} ", classDef.Modifier.ToString().ToLower());

            textWriter.Write("class {0}", SafeName(classDef.ClassName));
            if (!string.IsNullOrEmpty(classDef.SuperClassName))
                textWriter.Write(" : {0}", SafeTypeName(classDef.SuperClassName));

            textWriter.WriteLine();
            textWriter.WriteLine("{");
            ++textWriter.Indentation;

            if (classDef.Fields.Length > 0)
            {
                foreach (ClassField field in classDef.Fields)
                    DumpField(field);
                textWriter.WriteLine();
            }

            if (classDef.Constructor != null)
                DumpConstructor(classDef.Constructor);

            foreach (ClassProperty property in classDef.Properties)
                DumpProperty(property);

            if (classDef.Events.Length > 0)
            {
                foreach (ClassEvent _event in classDef.Events)
                    DumpEvent(_event);
                textWriter.WriteLine();
            }

            foreach (ClassMethod method in classDef.Methods)
                DumpMethod(method);

            --textWriter.Indentation;
            textWriter.WriteLine("}");
        }

        public void CompileFunctionDecl(FunctionDecl fnDecl)
        {
            if (fnDecl.Attributes != null && fnDecl.Attributes.Length > 0)
                DumpAttributesList(fnDecl.Attributes, true);

            textWriter.Write("function {0}", SafeName(fnDecl.Name));
            DumpFunction(fnDecl.Function);
        }

        public void CompileExternalFunctionDecl(ExternalFunctionDecl extDecl)
        {
            if (extDecl.Attributes != null && extDecl.Attributes.Length > 0)
                DumpAttributesList(extDecl.Attributes, true);

            textWriter.Write("extern function {0}", SafeName(extDecl.Name));
            DumpParametersList(extDecl.Parameters);
            textWriter.WriteLine(";");
        }

        public void CompileConstantDecl(ConstantDecl cstDecl)
        {
            textWriter.Write("const ");

            for (int i = 0; i < cstDecl.Initializers.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                DumpPropertyInitializer(cstDecl.Initializers[i]);
            }

            textWriter.WriteLine(";");
        }

        public void CompileVariableDecl(VariableDecl varDecl)
        {
            textWriter.Write("var ");

            for (int i = 0; i < varDecl.Initializers.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                DumpPropertyInitializer(varDecl.Initializers[i]);
            }

            textWriter.WriteLine(";");
        }

        public void CompileBlock(Block block)
        {
            textWriter.WriteLine("{");
            ++textWriter.Indentation;

            bool wasFunctionBody = inFunctionBody;
            inFunctionBody = false;
            bool wasBlockInline = isBlockInline;
            isBlockInline = false;

            int counter = 0;
            int length = block.Statements.Length;
            if (wasFunctionBody) --length;

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
                stmt.AcceptCompiler(this);
                if (stmt is Expression) textWriter.WriteLine(";");
                ++counter;
            }

            --textWriter.Indentation;
            textWriter.Write("}");
            if (!wasBlockInline) textWriter.WriteLine();

            isBlockInline = wasBlockInline;
            inFunctionBody = wasFunctionBody;
        }

        public void CompileAssignment(Assignment assignment)
        {
            assignment.LeftOperand.AcceptCompiler(this);
            if (assignment.Operator == BinaryOperator.IfNull)
                textWriter.Write(" ?? ");
            else
                textWriter.Write(" {0}= ", BinaryOperatorToString(assignment.Operator));
            MayBeParenthesize(assignment.RightOperand);
        }

        public void CompileTernaryExpression(TernaryExpression terExpr)
        {
            MayBeParenthesize(terExpr.Test);
            textWriter.Write(" ? ");
            MayBeParenthesize(terExpr.TruePart);
            textWriter.Write(" : ");
            MayBeParenthesize(terExpr.FalsePart);
        }

        public void CompileBinaryExpression(BinaryExpression binaryExpr)
        {
            MayBeParenthesize(binaryExpr.LeftOperand);
            textWriter.Write(" {0} ", BinaryOperatorToString(binaryExpr.Operator));
            MayBeParenthesize(binaryExpr.RightOperand);
        }

        public void CompileUnaryExpression(UnaryExpression unExpr)
        {
            switch (unExpr.Operator)
            {
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

        public void CompileLiteral(Literal literal)
        {
            DumpDynamic(literal.Value);
        }

        public void CompileComplexInitializer(ComplexInitializer cplxInit)
        {
            textWriter.Write("(");
            cplxInit.RealPartInitializer.AcceptCompiler(this);
            textWriter.Write(", ");
            cplxInit.ImaginaryPartInitializer.AcceptCompiler(this);
            textWriter.Write(")");
        }

        public void CompileListInitializer(ListInitializer listInit)
        {
            DumpExpressionsList(listInit.Items, "[", "]");
        }

        public void CompileMapInitializer(MapInitializer mapInit)
        {
            DumpMapItemInitializersList(mapInit.ItemInitializers);
        }

        public void CompileSetInitializer(SetInitializer setInit)
        {
            DumpExpressionsList(setInit.Items, "{", "}");
        }

        public void CompileObjectInitializer(ObjectInitializer objInit)
        {
            textWriter.Write("new ");
            DumpPropertyInitializersList(objInit.PropertyInitializers);
        }

        public void CompileInlineFunction(InlineFunction inlineFn)
        {
            if (inlineFn.IsLambda())
                DumpLambda(inlineFn.Function);
            else
            {
                textWriter.PushPrefix();
                textWriter.Write("function ");
                bool wasBlockInline = isBlockInline;
                isBlockInline = true;
                DumpFunction(inlineFn.Function);
                isBlockInline = wasBlockInline;
                textWriter.PopPrefix();
            }
        }

        public void CompileVariableRef(VariableRef variableRef)
        {
            textWriter.Write(SafeName(variableRef.Name));
        }

        public void CompileItemRef(ItemRef itemRef)
        {
            MayBeParenthesize(itemRef.Owner);
            textWriter.Write("[");
            itemRef.Index.AcceptCompiler(this);
            textWriter.Write("]");
        }

        public void CompilePropertyRef(PropertyRef fieldRef)
        {
            MayBeParenthesize(fieldRef.Owner);
            textWriter.Write(".{0}", SafeName(fieldRef.PropertyName));
        }

        public void CompileStaticPropertyRef(StaticPropertyRef staticRef)
        {
            textWriter.Write(SafeName(staticRef.Name));
        }

        public void CompileThisReference(ThisReference thisRef)
        {
            textWriter.Write("this");
        }

        public void CompileFunctionCall(FunctionCall fnCall)
        {
            textWriter.Write(SafeName(fnCall.FunctionName));
            DumpExpressionsList(fnCall.Arguments);
        }

        public void CompileAnonymousCall(AnonymousCall anCall)
        {
            MayBeParenthesize(anCall.Callee);
            DumpExpressionsList(anCall.Arguments);
        }

        public void CompileMethodCall(MethodCall methodCall)
        {
            MayBeParenthesize(methodCall.Caller);
            textWriter.Write(".{0}", SafeName(methodCall.FunctionName));
            DumpExpressionsList(methodCall.Arguments);
        }

        public void CompileStaticMethodCall(StaticMethodCall staticCall)
        {
            textWriter.Write(SafeName(staticCall.Name));
            DumpExpressionsList(staticCall.Arguments);
        }

        public void CompileConstructorCall(ConstructorCall ctorCall)
        {
            textWriter.Write("new {0}", SafeName(ctorCall.Name));
            DumpExpressionsList(ctorCall.Arguments);

            if (ctorCall.PropertyInitializers == null) return;

            textWriter.Write(" ");
            DumpPropertyInitializersList(ctorCall.PropertyInitializers);
        }

        public void CompileParentMethodCall(ParentMethodCall pmc)
        {
            textWriter.Write("super::{0}", SafeName(pmc.FunctionName));
            DumpExpressionsList(pmc.Arguments);
        }

        public void CompileParentConstructorCall(ParentConstructorCall pcc)
        {
            textWriter.Write(" : super");
            DumpExpressionsList(pcc.Arguments);
        }

        public void CompileInnerFunctionCall(InnerFunctionCall ifc)
        {
        }

        public void CompileExternalFunctionCall(ExternalFunctionCall efc)
        {
        }

        public void CompileTypeVerification(TypeVerification typeVerif)
        {
            MayBeParenthesize(typeVerif.Expression);
            textWriter.Write(" is {0}", SafeTypeName(typeVerif.TypeName));
        }

        public void CompileTypeOfExpression(TypeOfExpression typeOf)
        {
            textWriter.Write("typeof({0})", SafeTypeName(typeOf.TypeName));
        }

        public void CompileConversion(Conversion conversion)
        {
            textWriter.Write("({0}) ", SafeTypeName(conversion.TypeName));
            MayBeParenthesize(conversion.Expression);
        }

        public void CompileIfThenElse(IfThenElse ifThenElse)
        {
            textWriter.Write("if (");
            ifThenElse.Condition.AcceptCompiler(this);
            textWriter.WriteLine(")");
            MayBeIndent(ifThenElse.IfBlock);

            if (ifThenElse.ElseBlock == null) return;

            if (ifThenElse.ElseBlock is IfThenElse)
            {
                textWriter.Write("else ");
                ifThenElse.ElseBlock.AcceptCompiler(this);
            }
            else
            {
                textWriter.WriteLine("else");
                MayBeIndent(ifThenElse.ElseBlock);
            }
        }

        public void CompileSwitchBlock(SwitchBlock switchBlock)
        {
            textWriter.Write("switch (");
            switchBlock.Expression.AcceptCompiler(this);
            textWriter.WriteLine(")");
            textWriter.WriteLine("{");
            ++textWriter.Indentation;

            int counter = 0;
            while (counter < switchBlock.Statements.Length)
            {
                foreach (CaseLabel caseLabel in switchBlock.Cases)
                {
                    if (caseLabel.Address != counter) continue;

                    textWriter.Write("case ");
                    DumpDynamic(caseLabel.Value);
                    textWriter.WriteLine(":");
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
            textWriter.WriteLine("}");
        }

        public void CompileForLoop(ForLoop forLoop)
        {
            textWriter.Write("for (");

            if (forLoop.Initializers.Length == 1 && forLoop.Initializers[0] is VariableDecl)
                forLoop.Initializers[0].AcceptCompiler(this);
            else
            {
                bool comma1 = false;
                foreach (Statement initializer in forLoop.Initializers)
                {
                    if (comma1) textWriter.Write(", ");
                    initializer.AcceptCompiler(this);
                    comma1 = true;
                }
            }

            textWriter.Write("; ");
            if (forLoop.Guard != null)
                forLoop.Guard.AcceptCompiler(this);
            textWriter.Write("; ");

            bool comma2 = false;
            foreach (Expression updater in forLoop.Updaters)
            {
                if (comma2) textWriter.Write(", ");
                updater.AcceptCompiler(this);
                comma2 = true;
            }

            textWriter.WriteLine(")");
            MayBeIndent(forLoop.Body);
        }

        public void CompileForEachLoop(ForEachLoop forEach)
        {
            textWriter.Write("foreach (");
            if (forEach.KeyName != ForEachLoop.DEFAULT_KEY_NAME)
                textWriter.Write("{0} => ", SafeName(forEach.KeyName));
            textWriter.Write("{0} in ", SafeName(forEach.ValueName));
            forEach.Enumerated.AcceptCompiler(this);
            textWriter.WriteLine(")");
            MayBeIndent(forEach.Body);
        }

        public void CompileWhileLoop(WhileLoop whileLoop)
        {
            textWriter.Write("while (");
            whileLoop.Guard.AcceptCompiler(this);
            textWriter.WriteLine(")");
            MayBeIndent(whileLoop.Body);
        }

        public void CompileDoLoop(DoLoop doLoop)
        {
            textWriter.WriteLine("do ");
            MayBeIndent(doLoop.Body);
            textWriter.Write("while (");
            doLoop.Guard.AcceptCompiler(this);
            textWriter.WriteLine(");");
        }

        public void CompileContinue(Continue _continue)
        {
            textWriter.WriteLine("continue;");
        }

        public void CompileBreak(Break _break)
        {
            textWriter.WriteLine("break;");
        }

        public void CompileGoto(Goto _goto)
        {
            textWriter.WriteLine("goto {0};", SafeName(_goto.LabelName));
        }

        public void CompileReturn(Return _return)
        {
            textWriter.Write("return");

            if (_return.Expression != null)
            {
                textWriter.Write(" ");
                _return.Expression.AcceptCompiler(this);
            }

            textWriter.WriteLine(";");
        }

        public void CompileThrow(Throw _throw)
        {
            textWriter.Write("throw ");
            _throw.Expression.AcceptCompiler(this);
            textWriter.WriteLine(";");
        }

        public void CompileTryCatchFinally(TryCatchFinally tcf)
        {
            textWriter.WriteLine("try");
            tcf.TryBlock.AcceptCompiler(this);
            
            if (tcf.CatchBlock != null)
            {
                textWriter.WriteLine("catch ({0})", SafeName(tcf.ExceptionName));
                tcf.CatchBlock.AcceptCompiler(this);
            }

            if (tcf.FinallyBlock != null)
            {
                textWriter.WriteLine("finally");
                tcf.FinallyBlock.AcceptCompiler(this);
            }
        }

        #endregion

        #region Utility

        private void DumpConstructor(ClassMethod constructor)
        {
            textWriter.Write(constructor.Scope.ToString().ToLower());
            textWriter.Write(" constructor");

            Function function = constructor.Function;
            DumpParametersList(function.Parameters);
            if (function.Body.Statements.Length > 0 &&
                function.Body.Statements[0] is ParentConstructorCall)
                function.Body.Statements[0].AcceptCompiler(this);

            textWriter.WriteLine();
            DumpFunctionBody(function.Body);
            textWriter.WriteLine();
        }

        private void DumpField(ClassField field)
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
                field.Initializer.AcceptCompiler(this);
            }

            textWriter.WriteLine(";");
        }

        private void DumpProperty(ClassProperty property)
        {
            textWriter.Write(property.Scope.ToString().ToLower());
            if (property.Modifier != Modifier.Default)
                textWriter.Write(" {0}", property.Modifier.ToString().ToLower());
            textWriter.Write(" property {0}", SafeName(property.Name));

            if (string.IsNullOrEmpty(property.BackingFieldName))
                DumpExpandedProperty(property);
            else
                DumpAutoProperty(property);

            textWriter.WriteLine();
        }

        private void DumpExpandedProperty(ClassProperty property)
        {
            textWriter.WriteLine();
            textWriter.WriteLine("{");
            ++textWriter.Indentation;

            if (property.CanRead)
            {
                if (property.Reader.Scope != property.Scope)
                    textWriter.Write("{0} ", property.Reader.Scope.ToString().ToLower());
                textWriter.WriteLine("read");
                DumpFunctionBody(property.Reader.Function.Body);
            }

            if (property.CanWrite)
            {
                if (property.Writer.Scope != property.Scope)
                    textWriter.Write("{0} ", property.Writer.Scope.ToString().ToLower());
                textWriter.WriteLine("write");
                DumpFunctionBody(property.Writer.Function.Body);
            }

            --textWriter.Indentation;
            textWriter.WriteLine("}");
        }

        private void DumpAutoProperty(ClassProperty property)
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

        private void DumpMethod(ClassMethod method)
        {
            textWriter.Write(method.Scope.ToString().ToLower());
            if (method.Modifier != Modifier.Default)
                textWriter.Write(" {0}", method.Modifier.ToString().ToLower());
            
            if (ClassMethod.GetUnaryOperator(method.Name) != UnaryOperator.None)
            {
                var _operator = ClassMethod.GetUnaryOperator(method.Name);
                textWriter.Write(" operator {0}", UnaryOperatorToString(_operator));
            }
            else if (ClassMethod.GetBinaryOperator(method.Name) != BinaryOperator.None)
            {
                var _operator = ClassMethod.GetBinaryOperator(method.Name);
                textWriter.Write(" operator {0}", BinaryOperatorToString(_operator));
            }
            else
                textWriter.Write(" function {0}", SafeName(method.Name));

            if (method.Modifier == Modifier.Abstract)
            {
                DumpParametersList(method.Function.Parameters);
                textWriter.WriteLine(";");
            }
            else
                DumpFunction(method.Function);

            textWriter.WriteLine();
        }

        private void DumpEvent(ClassEvent _event)
        {
            textWriter.Write(_event.Scope.ToString().ToLower());
            if (_event.Modifier != Modifier.Default)
                textWriter.Write(" {0}", _event.Modifier.ToString().ToLower());
            textWriter.Write(" event {0}", SafeName(_event.Name));
            DumpParametersList(_event.Parameters);
            textWriter.WriteLine(";");
        }

        private void DumpFunctionBody(Block body)
        {
            bool wasFunctionBody = inFunctionBody;
            inFunctionBody = true;
            body.AcceptCompiler(this);
            inFunctionBody = wasFunctionBody;
        }

        private void DumpFunction(Function function)
        {
            DumpParametersList(function.Parameters);
            textWriter.WriteLine();
            DumpFunctionBody(function.Body);
        }

        private void DumpLambda(Function lambda)
        {
            Parameter[] parameters = lambda.Parameters;
            
            textWriter.Write("|");
            if (parameters.Length <= 0)
                textWriter.Write(" ");
            else
                for (int i = 0; i < parameters.Length; ++i)
                {
                    if (i > 0) textWriter.Write(", ");
                    textWriter.Write(SafeName(parameters[i].Name));
                }
            textWriter.Write("| => ");

            var _return = (Return) lambda.Body.Statements[0];
            _return.Expression.AcceptCompiler(this);
        }

        private void DumpParametersList(Parameter[] parameters)
        {
            textWriter.Write("(");
            
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                DumpParameter(parameters[i]);
            }

            textWriter.Write(")");
        }

        private void DumpParameter(Parameter parameter)
        {
            if (parameter.Attributes != null && parameter.Attributes.Length > 0)
                DumpAttributesList(parameter.Attributes, false);

            if (parameter.ByRef)
                textWriter.Write("ref ");
            else if (parameter.VaArgs)
                textWriter.Write("params ");

            textWriter.Write(SafeName(parameter.Name));

            Dynamic defVal = parameter.DefaultValue;
            if (defVal == null) return;

            textWriter.Write(" = ");
            DumpDynamic(defVal);
        }

        private void DumpDynamic(Dynamic v)
        {
            IFormatProvider fp = CultureInfo.InvariantCulture;

            switch (v.Class.ClassID)
            {
                case ClassID.Void:
                    textWriter.Write("null");
                    break;
                case ClassID.Boolean:
                    textWriter.Write(v.AsBoolean ? "true" : "false");
                    break;
                case ClassID.Integer:
                    textWriter.Write(v.ToString("g", fp));
                    break;
                case ClassID.Long:
                    textWriter.Write(v.ToString("g", fp) + "L");
                    break;
                case ClassID.Float:
                    textWriter.Write(v.ToString("g", fp) + "F");
                    break;
                case ClassID.Decimal:
                    textWriter.Write(v.ToString("", fp) + "D");
                    break;
                case ClassID.Date:
                    textWriter.Write("`" + v.ToString("u", fp) + "`");
                    break;
                case ClassID.String:
                    textWriter.Write("'" +
                                     v.ToString().
                                     Replace("\\", "\\\\").
                                     Replace("'", "\\'").
                                     Replace("\a", "\\a").
                                     Replace("\b", "\\b").
                                     Replace("\r", "\\r").
                                     Replace("\n", "\\n").
                                     Replace("\t", "\\t").
                                     Replace("\v", "\\v").
                                     Replace("\f", "\\f") +
                                     "'");
                    break;
            }
        }

        private void DumpExpressionsList(Expression[] expressions, string startDelim, string endDelim)
        {
            textWriter.Write(startDelim);

            for (int i = 0; i < expressions.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                expressions[i].AcceptCompiler(this);
            }

            textWriter.Write(endDelim);
        }

        private void DumpExpressionsList(Expression[] expressions)
        {
            DumpExpressionsList(expressions, "(", ")");
        }

        private void DumpPropertyInitializersList(PropertyInitializer[] propertyInitializers)
        {
            textWriter.Write("{");

            for (int i = 0; i < propertyInitializers.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                DumpPropertyInitializer(propertyInitializers[i]);
            }

            textWriter.Write("}");
        }

        private void DumpPropertyInitializer(PropertyInitializer propertyInitializer)
        {
            textWriter.Write(SafeName(propertyInitializer.Name));

            if (propertyInitializer.Expression != null)
            {
                textWriter.Write(" = ");
                propertyInitializer.Expression.AcceptCompiler(this);
            }
        }

        private void DumpMapItemInitializersList(MapItemInitializer[] itemInitializers)
        {
            textWriter.Write("{");

            if (itemInitializers.Length > 0)
                for (int i = 0; i < itemInitializers.Length; ++i)
                {
                    if (i > 0) textWriter.Write(", ");
                    DumpMapItemInitializer(itemInitializers[i]);
                }
            else
                textWriter.Write("=>");

            textWriter.Write("}");
        }

        private void DumpMapItemInitializer(MapItemInitializer itemInitializer)
        {
            itemInitializer.Key.AcceptCompiler(this);
            textWriter.Write(" => ");
            itemInitializer.Value.AcceptCompiler(this);
        }

        private void DumpAttributesList(Attribute[] attributes, bool multiline)
        {
            bool wrapLines = multiline & attributes.Length > 1;

            if (wrapLines)
            {
                textWriter.WriteLine("[");
                ++textWriter.Indentation;
            }
            else
                textWriter.Write("[");

            for (int i = 0; i < attributes.Length; ++i)
            {
                if (i > 0) textWriter.WriteLine(", ");
                DumpAttribute(attributes[i]);
            }

            if (wrapLines)
            {
                --textWriter.Indentation;
                textWriter.WriteLine();
                textWriter.WriteLine("]");
            }
            else if (multiline)
                textWriter.WriteLine("]");
            else
                textWriter.Write("] ");
        }

        private void DumpAttribute(Attribute attribute)
        {
            textWriter.Write(SafeName(attribute.Name));
            DumpAttributeProperties(attribute.Properties);
        }

        private void DumpAttributeProperties(AttributeProperty[] attributeProperties)
        {
            textWriter.Write("(");

            for (int i = 0; i < attributeProperties.Length; ++i)
            {
                if (i > 0) textWriter.Write(", ");
                DumpAttributeProperty(attributeProperties[i]);
            }

            textWriter.Write(")");
        }

        private void DumpAttributeProperty(AttributeProperty attributeProperty)
        {
            textWriter.Write("{0} = ", SafeName(attributeProperty.Name));
            DumpDynamic(attributeProperty.Value);
        }

        private void MayBeIndent(AstNode stmt)
        {
            if (stmt.GetType() == typeof(Block))
                stmt.AcceptCompiler(this);
            else
            {
                ++textWriter.Indentation;
                stmt.AcceptCompiler(this);
                if (stmt is Expression) textWriter.WriteLine(";");
                --textWriter.Indentation;
            }
        }

        private void MayBeParenthesize(Expression expr)
        {
            if (expr.IsParenthesized)
            {
                textWriter.Write("(");
                expr.AcceptCompiler(this);
                textWriter.Write(")");
            }
            else
                expr.AcceptCompiler(this);
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

        public static string EscapedString(string original)
        {
            var sb = new StringBuilder();

            foreach (char ch in original)
            {
                if (Lexer.IsLegalIdChar(ch))
                    sb.Append(ch);
                else
                    sb.Append("\\u").Append(((short) ch).ToString("x4"));
            }

            return sb.ToString();
        }

        public static string SafeTypeName(string name)
        {
            return IsKeyword(name) || !Lexer.IsLegalFirstIdChar(name[0])
                 ? "$" + EscapedString(name)
                 : EscapedString(name);
        }

        public static string SafeName(string name)
        {
            return IsTypeName(name) || IsKeyword(name) || !Lexer.IsLegalFirstIdChar(name[0])
                 ? "$" + EscapedString(name)
                 : EscapedString(name);
        }

        public static string SafeName(QualifiedName name)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < name.Length; ++i)
            {
                if (i > 0) sb.Append("::");
                sb.Append(SafeName(name[i]));
            }

            return sb.ToString();
        }

        public static string UnaryOperatorToString(UnaryOperator oper)
        {
            switch (oper)
            {
                case UnaryOperator.Plus:
                    return "+";
                case UnaryOperator.Minus:
                    return "-";
                case UnaryOperator.Not:
                    return "!";
                case UnaryOperator.BitwiseNot:
                    return "~";
                case UnaryOperator.PreIncrement:
                case UnaryOperator.PostIncrement:
                    return "++";
                case UnaryOperator.PreDecrement:
                case UnaryOperator.PostDecrement:
                    return "--";
                default:
                    return string.Empty;
            }
        }

        public static string BinaryOperatorToString(BinaryOperator oper)
        {
            switch (oper)
            {
                case BinaryOperator.Plus:
                    return "+";
                case BinaryOperator.Minus:
                    return "-";
                case BinaryOperator.Times:
                    return "*";
                case BinaryOperator.Divide:
                    return "/";
                case BinaryOperator.Modulo:
                    return "%";
                case BinaryOperator.Power:
                    return "**";
                case BinaryOperator.ShiftLeft:
                    return "<<";
                case BinaryOperator.ShiftRight:
                    return ">>";
                case BinaryOperator.LessThan:
                    return "<";
                case BinaryOperator.LessThanOrEqual:
                    return "<=";
                case BinaryOperator.GreaterThan:
                    return ">";
                case BinaryOperator.GreaterThanOrEqual:
                    return ">=";
                case BinaryOperator.Equal:
                    return "==";
                case BinaryOperator.NotEqual:
                    return "!=";
                case BinaryOperator.Identical:
                    return "===";
                case BinaryOperator.NotIdentical:
                    return "!==";
                case BinaryOperator.And:
                    return "&";
                case BinaryOperator.AndAlso:
                    return "&&";
                case BinaryOperator.Or:
                    return "|";
                case BinaryOperator.OrElse:
                    return "||";
                case BinaryOperator.ExclusiveOr:
                    return "^";
                case BinaryOperator.StartsWith:
                    return "startswith";
                case BinaryOperator.EndsWith:
                    return "endswith";
                case BinaryOperator.Contains:
                    return "contains";
                case BinaryOperator.Matches:
                    return "matches";
                default:
                    return string.Empty;
            }
        }

        #endregion
    }
}