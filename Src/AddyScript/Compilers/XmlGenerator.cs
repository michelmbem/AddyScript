using System.Xml;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Runtime;


namespace AddyScript.Compilers
{
    public class XmlGenerator : ICompiler
    {
        private readonly XmlDocument document;
        private readonly XmlDeclaration declaration;
        private XmlElement currentElement;

        public XmlGenerator()
        {
            document = new XmlDocument();
            declaration = document.CreateXmlDeclaration("1.0", null, null);
            document.InsertBefore(declaration, document.DocumentElement);
        }

        public XmlDocument Document
        {
            get { return document; }
        }

        #region ICompiler Members

        public void CompileProgram(Program program)
        {
            currentElement = document.CreateElement("Program");
            document.InsertAfter(currentElement, declaration);

            foreach (AstNode astNode in program.Statements)
                astNode.AcceptCompiler(this);
        }

        public void CompileImportDirective(ImportDirective import)
        {
            XmlElement tmpElement = document.CreateElement("ImportDirective");
            tmpElement.SetAttribute("ModuleName", import.ModuleName.ToString());
            if (!string.IsNullOrEmpty(import.Alias)) tmpElement.SetAttribute("Alias", import.Alias);
            currentElement.AppendChild(tmpElement);
        }

        public void CompileClassDefinition(ClassDefinition classDef)
        {
            XmlElement tmpElement = document.CreateElement("ClassDefinition");
            tmpElement.SetAttribute("ClassName", classDef.ClassName);

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

            XmlElement fieldsElement = document.CreateElement("Fields");
            foreach (ClassField field in classDef.Fields)
                ProcessClassField(fieldsElement, field);
            tmpElement.AppendChild(fieldsElement);

            XmlElement propertiesElement = document.CreateElement("Properties");
            foreach (ClassProperty property in classDef.Properties)
                ProcessClassProperty(propertiesElement, property);
            tmpElement.AppendChild(propertiesElement);

            XmlElement methodsElement = document.CreateElement("Methods");
            foreach (ClassMethod method in classDef.Methods)
                ProcessClassMethod(methodsElement, method);
            tmpElement.AppendChild(methodsElement);

            XmlElement eventsElement = document.CreateElement("Events");
            foreach (ClassEvent _event in classDef.Events)
                ProcessClassEvent(eventsElement, _event);
            tmpElement.AppendChild(eventsElement);

            currentElement.AppendChild(tmpElement);
        }

        public void CompileFunctionDecl(FunctionDecl fnDecl)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("FunctionDecl");
            tmpElement.SetAttribute("Name", fnDecl.Name);

            if (fnDecl.Attributes != null && fnDecl.Attributes.Length > 0)
                ProcessAttributes(tmpElement, fnDecl.Attributes);

            XmlElement paramsElement = document.CreateElement("Parameters");
            ProcessParameters(paramsElement, fnDecl.Function.Parameters);
            tmpElement.AppendChild(paramsElement);

            currentElement = document.CreateElement("Body");
            fnDecl.Function.Body.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileExternalFunctionDecl(ExternalFunctionDecl extDecl)
        {
            XmlElement tmpElement = document.CreateElement("ExternalFunctionDecl");
            tmpElement.SetAttribute("Name", extDecl.Name);

            if (extDecl.Attributes != null && extDecl.Attributes.Length > 0)
                ProcessAttributes(tmpElement, extDecl.Attributes);

            XmlElement paramsElement = document.CreateElement("Parameters");
            ProcessParameters(paramsElement, extDecl.Parameters);
            tmpElement.AppendChild(paramsElement);

            currentElement.AppendChild(tmpElement);
        }

        public void CompileConstantDecl(ConstantDecl cstDecl)
        {
            XmlElement tmpElement = document.CreateElement("ConstantDecl");
            ProcessPropertyInitializers(tmpElement, cstDecl.Initializers);
            currentElement.AppendChild(tmpElement);
        }

        public void CompileVariableDecl(VariableDecl varDecl)
        {
            XmlElement tmpElement = document.CreateElement("VariableDecl");
            ProcessPropertyInitializers(tmpElement, varDecl.Initializers);
            currentElement.AppendChild(tmpElement);
        }

        public void CompileBlock(Block block)
        {
            XmlElement previousElement = currentElement;
            currentElement = document.CreateElement("Block");

            foreach (Statement statement in block.Statements)
                statement.AcceptCompiler(this);

            previousElement.AppendChild(currentElement);
            currentElement = previousElement;
        }

        public void CompileAssignment(Assignment assign)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("Assignment");
            tmpElement.SetAttribute("Operator", assign.Operator.ToString());

            currentElement = document.CreateElement("LeftOperand");
            assign.LeftOperand.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("RightOperand");
            assign.RightOperand.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileTernaryExpression(TernaryExpression terExpr)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("TernaryExpression");

            currentElement = document.CreateElement("Test");
            terExpr.Test.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("TruePart");
            terExpr.TruePart.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("FalsePart");
            terExpr.FalsePart.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileBinaryExpression(BinaryExpression binExpr)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("BinaryExpression");
            tmpElement.SetAttribute("Operator", binExpr.Operator.ToString());

            currentElement = document.CreateElement("LeftOperand");
            binExpr.LeftOperand.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("RightOperand");
            binExpr.RightOperand.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileUnaryExpression(UnaryExpression unExpr)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("UnaryExpression");
            tmpElement.SetAttribute("Operator", unExpr.Operator.ToString());

            currentElement = document.CreateElement("Operand");
            unExpr.Operand.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileLiteral(Literal literal)
        {
            XmlElement tmpElement = document.CreateElement("Literal");
            tmpElement.SetAttribute("Type", literal.Value.Class.Name);
            tmpElement.SetAttribute("Value", literal.Value.ToString());
            currentElement.AppendChild(tmpElement);
        }

        public void CompileComplexInitializer(ComplexInitializer cplxInit)
        {
            XmlElement tmpElement = document.CreateElement("ComplexInitializer");
            XmlElement previousElement = currentElement;
            currentElement = document.CreateElement("RealPartInitializer");
            cplxInit.RealPartInitializer.AcceptCompiler(this);
            currentElement = document.CreateElement("ImaginaryPartInitializer");
            cplxInit.ImaginaryPartInitializer.AcceptCompiler(this);
            currentElement = previousElement;
            currentElement.AppendChild(tmpElement);
        }

        public void CompileListInitializer(ListInitializer listInit)
        {
            XmlElement tmpElement = document.CreateElement("ListInitializer");
            XmlElement previousElement = currentElement;
            currentElement = document.CreateElement("Items");
            foreach (Expression item in listInit.Items)
                item.AcceptCompiler(this);
            currentElement = previousElement;
            currentElement.AppendChild(tmpElement);
        }

        public void CompileMapInitializer(MapInitializer mapInit)
        {
            XmlElement tmpElement = document.CreateElement("MapInitializer");
            ProcessMapItemInitializers(tmpElement, mapInit.ItemInitializers);
            currentElement.AppendChild(tmpElement);
        }

        public void CompileSetInitializer(SetInitializer setInit)
        {
            XmlElement tmpElement = document.CreateElement("SetInitializer");
            XmlElement previousElement = currentElement;
            currentElement = document.CreateElement("Items");
            foreach (Expression item in setInit.Items)
                item.AcceptCompiler(this);
            currentElement = previousElement;
            currentElement.AppendChild(tmpElement);
        }

        public void CompileObjectInitializer(ObjectInitializer objectInit)
        {
            XmlElement tmpElement = document.CreateElement("ObjectInitializer");
            ProcessPropertyInitializers(tmpElement, objectInit.PropertyInitializers);
            currentElement.AppendChild(tmpElement);
        }

        public void CompileInlineFunction(InlineFunction inline)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("InlineFunction");

            XmlElement paramsElement = document.CreateElement("Parameters");
            ProcessParameters(paramsElement, inline.Function.Parameters);
            tmpElement.AppendChild(paramsElement);

            currentElement = document.CreateElement("Body");
            inline.Function.Body.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileVariableRef(VariableRef variableRef)
        {
            XmlElement tmpElement = document.CreateElement("VariableRef");
            tmpElement.SetAttribute("Name", variableRef.Name);
            currentElement.AppendChild(tmpElement);
        }

        public void CompileItemRef(ItemRef itemRef)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ItemRef");

            currentElement = document.CreateElement("Owner");
            itemRef.Owner.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Index");
            itemRef.Index.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompilePropertyRef(PropertyRef propRef)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("PropertyRef");
            tmpElement.SetAttribute("PropertyName", propRef.PropertyName);

            currentElement = document.CreateElement("Owner");
            propRef.Owner.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileStaticPropertyRef(StaticPropertyRef staticRef)
        {
            XmlElement tmpElement = document.CreateElement("StaticPropertyRef");
            tmpElement.SetAttribute("Name", staticRef.Name.ToString());
            currentElement.AppendChild(tmpElement);
        }

        public void CompileThisReference(ThisReference thisRef)
        {
            currentElement.AppendChild(document.CreateElement("ThisReference"));
        }

        public void CompileFunctionCall(FunctionCall functionCall)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("FunctionCall");
            tmpElement.SetAttribute("FunctionName", functionCall.FunctionName);

            currentElement = document.CreateElement("Arguments");
            foreach (Expression argument in functionCall.Arguments)
                argument.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileAnonymousCall(AnonymousCall anCall)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("AnonymousCall");

            currentElement = document.CreateElement("Callee");
            anCall.Callee.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Arguments");
            foreach (Expression argument in anCall.Arguments)
                argument.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileMethodCall(MethodCall methodCall)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("MethodCall");
            tmpElement.SetAttribute("FunctionName", methodCall.FunctionName);

            currentElement = document.CreateElement("Caller");
            methodCall.Caller.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Arguments");
            foreach (Expression argument in methodCall.Arguments)
                argument.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileStaticMethodCall(StaticMethodCall staticCall)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("StaticMethodCall");
            tmpElement.SetAttribute("Name", staticCall.Name.ToString());

            currentElement = document.CreateElement("Arguments");
            foreach (Expression argument in staticCall.Arguments)
                argument.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileConstructorCall(ConstructorCall constCall)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ConstructorCall");
            tmpElement.SetAttribute("ClassName", constCall.Name.ToString());

            currentElement = document.CreateElement("Arguments");
            foreach (Expression argument in constCall.Arguments)
                argument.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileParentMethodCall(ParentMethodCall pmc)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ParentMethodCall");
            tmpElement.SetAttribute("FunctionName", pmc.FunctionName);

            currentElement = document.CreateElement("Arguments");
            foreach (Expression argument in pmc.Arguments)
                argument.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileParentConstructorCall(ParentConstructorCall pcc)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ParentConstructorCall");

            currentElement = document.CreateElement("Arguments");
            foreach (Expression argument in pcc.Arguments)
                argument.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileInnerFunctionCall(InnerFunctionCall ifc)
        {
        }

        public void CompileExternalFunctionCall(ExternalFunctionCall efc)
        {
        }

        public void CompileTypeVerification(TypeVerification typeVerif)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("TypeVerification");
            tmpElement.SetAttribute("TypeName", typeVerif.TypeName);

            currentElement = document.CreateElement("Expression");
            typeVerif.Expression.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileTypeOfExpression(TypeOfExpression typeOf)
        {
            XmlElement tmpElement = document.CreateElement("TypeOfExpression");
            tmpElement.SetAttribute("TypeName", typeOf.TypeName);
            currentElement.AppendChild(tmpElement);
        }

        public void CompileConversion(Conversion conversion)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("Conversion");
            tmpElement.SetAttribute("TypeName", conversion.TypeName);

            currentElement = document.CreateElement("Expression");
            conversion.Expression.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileIfThenElse(IfThenElse ifThenElse)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("IfThenElse");

            currentElement = document.CreateElement("Condition");
            ifThenElse.Condition.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("IfBlock");
            ifThenElse.IfBlock.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            if (ifThenElse.ElseBlock != null)
            {
                currentElement = document.CreateElement("ElseBlock");
                ifThenElse.ElseBlock.AcceptCompiler(this);
                tmpElement.AppendChild(currentElement);
            }

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileSwitchBlock(SwitchBlock switchBlock)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("SwitchBlock");

            if (switchBlock.DefaultCase < int.MaxValue)
                tmpElement.SetAttribute("DefaultCase", switchBlock.DefaultCase.ToString());

            currentElement = document.CreateElement("Expression");
            switchBlock.Expression.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            if (switchBlock.Cases.Length > 0)
            {
                XmlElement casesElement = document.CreateElement("Cases");
                foreach (CaseLabel _case in switchBlock.Cases)
                    ProcessSwitchCase(casesElement, _case);
                tmpElement.AppendChild(casesElement);
            }

            currentElement = document.CreateElement("Statements");
            foreach (Statement statement in switchBlock.Statements)
                statement.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileForLoop(ForLoop forLoop)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ForLoop");

            currentElement = document.CreateElement("Initializers");
            foreach (Statement initializer in forLoop.Initializers)
                initializer.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Guard");
            if (forLoop.Guard != null)
                forLoop.Guard.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Updaters");
            foreach (Expression updater in forLoop.Updaters)
                updater.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Body");
            forLoop.Body.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileForEachLoop(ForEachLoop forEach)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ForEachLoop");
            tmpElement.SetAttribute("ValueName", forEach.ValueName);

            if (forEach.KeyName != ForEachLoop.DEFAULT_KEY_NAME)
                tmpElement.SetAttribute("KeyName", forEach.KeyName);

            currentElement = document.CreateElement("Enumerated");
            forEach.Enumerated.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Body");
            forEach.Body.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileWhileLoop(WhileLoop whileLoop)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("WhileLoop");

            currentElement = document.CreateElement("Guard");
            whileLoop.Guard.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Body");
            whileLoop.Body.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileDoLoop(DoLoop doLoop)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("DoLoop");

            currentElement = document.CreateElement("Guard");
            doLoop.Guard.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Body");
            doLoop.Body.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileContinue(Continue _continue)
        {
            currentElement.AppendChild(document.CreateElement("Continue"));
        }

        public void CompileBreak(Break _break)
        {
            currentElement.AppendChild(document.CreateElement("Break"));
        }

        public void CompileGoto(Goto _goto)
        {
            XmlElement tmpElement = document.CreateElement("Goto");
            tmpElement.SetAttribute("LabelName", _goto.LabelName);
            currentElement.AppendChild(tmpElement);
        }

        public void CompileReturn(Return _return)
        {
            XmlElement tmpElement = document.CreateElement("Return");
            currentElement.AppendChild(tmpElement);

            if (_return.Expression == null) return;

            XmlElement previousElement = currentElement;
            currentElement = tmpElement;
            _return.Expression.AcceptCompiler(this);
            currentElement = previousElement;
        }

        public void CompileThrow(Throw _throw)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("Throw");

            currentElement = document.CreateElement("Throw.Expression");
            _throw.Expression.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        public void CompileTryCatchFinally(TryCatchFinally tcf)
        {
            XmlElement previousElement = currentElement;
            XmlElement tmpElement = document.CreateElement("TryCatchFinally");
            tmpElement.SetAttribute("ExceptionName", tcf.ExceptionName);

            currentElement = document.CreateElement("TryBlock");
            tcf.TryBlock.AcceptCompiler(this);
            tmpElement.AppendChild(currentElement);

            if (tcf.CatchBlock != null)
            {
                currentElement = document.CreateElement("CatchBlock");
                tcf.CatchBlock.AcceptCompiler(this);
                tmpElement.AppendChild(currentElement);
            }

            if (tcf.FinallyBlock != null)
            {
                currentElement = document.CreateElement("FinallyBlock");
                tcf.FinallyBlock.AcceptCompiler(this);
                tmpElement.AppendChild(currentElement);
            }

            previousElement.AppendChild(tmpElement);
            currentElement = previousElement;
        }

        #endregion

        #region Utility

        private void ProcessAttributes(XmlElement parent, Attribute[] attributes)
        {
            XmlElement tmpElement = document.CreateElement("Attributes");
            parent.AppendChild(tmpElement);

            foreach (Attribute attribute in attributes)
                ProcessAttribute(tmpElement, attribute);
        }

        private void ProcessAttribute(XmlElement parent, Attribute attribute)
        {
            XmlElement tmpElement = document.CreateElement("Attribute");
            parent.AppendChild(tmpElement);
            tmpElement.SetAttribute("Name", attribute.Name);

            XmlElement fieldElement = document.CreateElement("Properties");
            ProcessAttributeProperties(fieldElement, attribute.Properties);
            tmpElement.AppendChild(fieldElement);
        }

        private void ProcessAttributeProperties(XmlElement parent, AttributeProperty[] props)
        {
            foreach (AttributeProperty prop in props)
                ProcessAttributeProperty(parent, prop);
        }

        private void ProcessAttributeProperty(XmlElement parent, AttributeProperty prop)
        {
            XmlElement tmpElement = document.CreateElement("Property");
            tmpElement.SetAttribute("Name", prop.Name);
            tmpElement.SetAttribute("Value", prop.Value.ToString());
            parent.AppendChild(tmpElement);
        }

        private void ProcessParameters(XmlElement parent, Parameter[] parameters)
        {
            foreach (Parameter parameter in parameters)
                ProcessParameter(parent, parameter);
        }

        private void ProcessParameter(XmlElement parent, Parameter parameter)
        {
            XmlElement tmpElement = document.CreateElement("Parameter");
            tmpElement.SetAttribute("Name", parameter.Name);
            tmpElement.SetAttribute("ByRef", parameter.ByRef.ToString());
            tmpElement.SetAttribute("VaArgs", parameter.VaArgs.ToString());
            
            if (parameter.DefaultValue != null)
                tmpElement.SetAttribute("DefaultValue", parameter.DefaultValue.ToString());
            
            if (parameter.Attributes != null && parameter.Attributes.Length > 0)
                ProcessAttributes(tmpElement, parameter.Attributes);
            
            parent.AppendChild(tmpElement);
        }

        private void ProcessPropertyInitializers(XmlElement parent, PropertyInitializer[] initializers)
        {
            foreach (PropertyInitializer initializer in initializers)
                ProcessPropertyInitializer(parent, initializer);
        }

        private void ProcessPropertyInitializer(XmlElement parent, PropertyInitializer initializer)
        {
            XmlElement tmpElement = document.CreateElement("PropertyInitializer");
            parent.AppendChild(tmpElement);
            tmpElement.SetAttribute("Name", initializer.Name);

            if (initializer.Expression != null)
            {
                XmlElement previousElement = currentElement;
                currentElement = document.CreateElement("Value");
                initializer.Expression.AcceptCompiler(this);
                tmpElement.AppendChild(currentElement);
                currentElement = previousElement;
            }
        }

        private void ProcessMapItemInitializers(XmlElement parent, MapItemInitializer[] initializers)
        {
            foreach (MapItemInitializer initializer in initializers)
                ProcessMapItemInitializer(parent, initializer);
        }

        private void ProcessMapItemInitializer(XmlElement parent, MapItemInitializer initializer)
        {
            XmlElement tmpElement = document.CreateElement("MapItemInitializer");
            parent.AppendChild(tmpElement);

            XmlElement previousElement = currentElement;

            currentElement = document.CreateElement("Key");
            initializer.Key.AcceptCompiler(this);
            previousElement.AppendChild(currentElement);

            currentElement = document.CreateElement("Value");
            initializer.Value.AcceptCompiler(this);
            previousElement.AppendChild(currentElement);

            currentElement = previousElement;
        }

        private void ProcessSwitchCase(XmlElement parent, CaseLabel switchCase)
        {
            XmlElement tmpElement = document.CreateElement("SwitchCase");
            tmpElement.SetAttribute("HashCode", switchCase.Value.ToString());
            tmpElement.SetAttribute("Address", switchCase.Address.ToString());
            parent.AppendChild(tmpElement);
        }

        private void ProcessClassField(XmlElement parent, ClassField field)
        {
            XmlElement savedElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ClassField");
            tmpElement.SetAttribute("Name", field.Name);
            tmpElement.SetAttribute("Scope", field.Scope.ToString());

            if (field.Modifier != Modifier.Default)
                tmpElement.SetAttribute("Modifier", field.Modifier.ToString());

            if (field.Initializer != null)
            {
                currentElement = document.CreateElement("Initializer");
                field.Initializer.AcceptCompiler(this);
                tmpElement.AppendChild(currentElement);
            }

            parent.AppendChild(tmpElement);
            currentElement = savedElement;
        }

        private void ProcessClassProperty(XmlElement parent, ClassProperty property)
        {
            XmlElement savedElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ClassProperty");
            tmpElement.SetAttribute("Name", property.Name);
            tmpElement.SetAttribute("Scope", property.Scope.ToString());

            if (property.Modifier != Modifier.Default)
                tmpElement.SetAttribute("Modifier", property.Modifier.ToString());

            if (string.IsNullOrEmpty(property.BackingFieldName))
            {
                if (property.CanRead)
                {
                    currentElement = document.CreateElement("Getter");
                    currentElement.SetAttribute("Scope", property.Reader.Scope.ToString());
                    property.Reader.Function.Body.AcceptCompiler(this);
                    tmpElement.AppendChild(currentElement);
                }

                if (property.CanWrite)
                {
                    currentElement = document.CreateElement("Setter");
                    currentElement.SetAttribute("Scope", property.Writer.Scope.ToString());
                    property.Writer.Function.Body.AcceptCompiler(this);
                    tmpElement.AppendChild(currentElement);
                }
            }
            else
            {
                tmpElement.SetAttribute("Access", property.Access.ToString());
                if ((property.Access & PropertyAccess.Read) != PropertyAccess.None)
                    tmpElement.SetAttribute("GetterScope", property.ReaderScope.ToString());
                if ((property.Access & PropertyAccess.Write) != PropertyAccess.None)
                    tmpElement.SetAttribute("SetterScope", property.WriterScope.ToString());
            }

            parent.AppendChild(tmpElement);
            currentElement = savedElement;
        }

        private void ProcessClassMethod(XmlElement parent, ClassMethod method)
        {
            XmlElement savedElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ClassMethod");
            tmpElement.SetAttribute("Name", method.Name);
            tmpElement.SetAttribute("Scope", method.Scope.ToString());

            if (method.Modifier != Modifier.Default)
                tmpElement.SetAttribute("Modifier", method.Modifier.ToString());

            currentElement = document.CreateElement("Parameters");
            ProcessParameters(currentElement, method.Function.Parameters);
            tmpElement.AppendChild(currentElement);

            if (method.Function.Body != null)
            {
                currentElement = document.CreateElement("Body");
                method.Function.Body.AcceptCompiler(this);
                tmpElement.AppendChild(currentElement);
            }

            parent.AppendChild(tmpElement);
            currentElement = savedElement;
        }

        private void ProcessClassEvent(XmlElement parent, ClassEvent _event)
        {
            XmlElement savedElement = currentElement;
            XmlElement tmpElement = document.CreateElement("ClassEvent");
            tmpElement.SetAttribute("Name", _event.Name);
            tmpElement.SetAttribute("Scope", _event.Scope.ToString());

            if (_event.Modifier != Modifier.Default)
                tmpElement.SetAttribute("Modifier", _event.Modifier.ToString());

            currentElement = document.CreateElement("Parameters");
            ProcessParameters(currentElement, _event.Parameters);
            tmpElement.AppendChild(currentElement);

            parent.AppendChild(tmpElement);
            currentElement = savedElement;
        }

        #endregion
    }
}
