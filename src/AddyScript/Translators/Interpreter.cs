using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Translators.Utility;
using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.Frames;
using AddyScript.Runtime.Utilities;
using AddyScript.Parsers;
using AddyScript.Properties;
using AddyScript.Runtime.OOP;
using Boolean = AddyScript.Runtime.DataItems.Boolean;
using Complex = AddyScript.Runtime.DataItems.Complex;
using Label = AddyScript.Ast.Statements.Label;
using Object = AddyScript.Runtime.DataItems.Object;
using String = AddyScript.Runtime.DataItems.String;
using Void = AddyScript.Runtime.DataItems.Void;


namespace AddyScript.Translators
{
    public class Interpreter : ITranslator
    {
        #region Constants

        internal const string MODULE_NAME_CONSTANT = "__name";
        internal const string MAIN_MODULE_NAME = "main";
        internal const string ROOT_FRAME_NAME = "root";
        internal const string CONTEXT_VARIABLE_NAME = "__context";

        #endregion

        #region Fields

        private readonly HashSet<string> importedModules = [];
        private readonly NameTree nameCache = new();
        private readonly Dictionary<Class, DataItem> typeInfoCache = [];
        private Stack<MethodFrame> frames = new();
        private MethodFrame rootFrame, currentFrame;
        private string fileName = string.Empty;
        private MissingReferenceAction misRefAct = MissingReferenceAction.Fail;
        private JumpCode jumpCode = JumpCode.None;
        private DataItem returnedValue; // Do not reference this field twice expecting it to have the same value!
        private Goto lastGoto;
        
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an instance of <see cref="Interpreter"/>.
        /// </summary>
        /// <param name="context">The initial context of this instance</param>
        public Interpreter(ScriptContext context)
        {
            InitialContext = context ?? throw new ArgumentNullException(nameof(context));
            CreateRootFrame();
            RegisterDefaults(MAIN_MODULE_NAME);
        }

        /// <summary>
        /// Initializes an instance of <see cref="Interpreter"/>.
        /// </summary>
        public Interpreter() : this(new ScriptContext())
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets/Sets some initial settings.
        /// </summary>
        public ScriptContext InitialContext { get; private set; }

        /// <summary>
        /// Gets the value returned by the last evaluated expression.
        /// </summary>
        public DataItem ReturnedValue => returnedValue;

        #endregion

        #region Members of ITranslator

        public void TranslateProgram(Program program)
        {
            string prevFileName = fileName;
            fileName = program.FileName;

            foreach (KeyValuePair<string, Label> pair in program.Labels)
                RegisterLabel(pair.Key, pair.Value);

            try
            {
                int counter = 0;
                while (counter < program.Statements.Length)
                {
                    program.Statements[counter].AcceptTranslator(this);
                    switch (jumpCode)
                    {
                        case JumpCode.None:
                            ++counter;
                            break;
                        case JumpCode.Goto:
                            if (program.Labels.TryGetValue(lastGoto.LabelName, out Label label))
                            {
                                counter = label.Address;
                                jumpCode = JumpCode.None;
                                break;
                            }

                            throw new RuntimeException(fileName, lastGoto,
                                string.Format(Resources.MissingLabel, lastGoto.LabelName));
                        default:
                            counter = int.MaxValue;
                            break;
                    }
                }
            }
            finally
            {
                jumpCode = JumpCode.None;
                fileName = prevFileName;
            }
        }

        public void TranslateImportDirective(ImportDirective import)
        {
            try
            {
                if (string.IsNullOrEmpty(import.Alias))
                {
                    if (!(ImportScript(import.ModuleName) || ImportNamespace(import.ModuleName, null)))
                        throw new RuntimeException(fileName, import, string.Format(Resources.ModuleNotFound, import.ModuleName));
                }
                else if (!ImportNamespace(import.ModuleName, import.Alias))
                    throw new RuntimeException(fileName, import, string.Format(Resources.UndefinedType, import.ModuleName));
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, import, ex);
            }
        }

        public void TranslateClassDefinition(ClassDefinition classDef)
        {
            if (rootFrame.RootBlock.GetItem(classDef.ClassName) != null)
                throw new RuntimeException(fileName, classDef, string.Format(Resources.NameConflict, classDef.ClassName));

            Class superClass = Class.Object;
            if (!string.IsNullOrEmpty(classDef.SuperClassName))
            {
                superClass = rootFrame.GetItem(classDef.SuperClassName) as Class ??
                    throw new RuntimeException(fileName, classDef, string.Format(Resources.UndefinedType, classDef.SuperClassName));
            }

            switch (superClass.Modifier)
            {
                case Modifier.Final:
                case Modifier.Static:
                    throw new RuntimeException(fileName, classDef, string.Format(Resources.CannotCreateSubclass, classDef.SuperClassName));
                case Modifier.Abstract:
                    if (classDef.Modifier != Modifier.Abstract)
                    {
                        const MemberKind kind = MemberKind.Indexer | MemberKind.Property | MemberKind.Method;
                        
                        foreach (ClassMember member in superClass.GetMembers(kind))
                        {
                            if (member.Modifier != Modifier.Abstract) continue;

                            ClassMemberDecl _override = null;
                            foreach (ClassMemberDecl m in classDef.GetMembers(kind))
                                if (m.Name == member.Name)
                                {
                                    _override = m;
                                    break;
                                }

                            if (_override == null)
                                throw new RuntimeException(fileName, classDef,
                                    string.Format(Resources.MustOverride, classDef.ClassName, member.FullName));

                            if (_override.Modifier == Modifier.Abstract || _override.Modifier == Modifier.Static)
                                throw new ScriptException(fileName, _override, string.Format(Resources.InvalidMemberModifier, _override.Name));
                        }
                    }
                    break;
            }

            foreach (ClassFieldDecl field in classDef.Fields)
            {
                ClassField superField = superClass.GetField(field.Name);
                if (superField != null)
                    throw new ScriptException(fileName, field,
                        string.Format(Resources.FieldDeclaredInAncestor, field.Name, superField.Holder.Name));

                if (superClass.GetMember(field.Name, MemberKind.All & ~MemberKind.Field) != null)
                    throw new ScriptException(fileName, field, string.Format(Resources.FieldHidesHomonymous, field.Name));
            }

            foreach (ClassPropertyDecl property in classDef.Properties)
            {
                if (superClass.GetMember(property.Name, MemberKind.All & ~MemberKind.Property) != null)
                    throw new ScriptException(fileName, property, string.Format(Resources.PropertyHidesHomonymous, property.Name));

                ClassProperty overriden = superClass.GetProperty(property.Name);
                if (overriden != null)
                {
                    if (overriden.Modifier == Modifier.Final || overriden.Modifier == Modifier.Static)
                        throw new ScriptException(fileName, property, string.Format(Resources.MemberCantOverride, overriden.FullName));

                    if (!property.MatchesSignature(overriden))
                        throw new ScriptException(fileName, property,
                            string.Format(Resources.MustMatchSignature, property.Name, overriden.FullName));
                }
            }

            foreach (ClassMethodDecl method in classDef.Methods)
            {
                if (superClass.GetMember(method.Name, MemberKind.All & ~MemberKind.Method) != null)
                    throw new ScriptException(fileName, method, string.Format(Resources.MethodHidesHomonymous, method.Name));

                ClassMethod overriden = superClass.GetMethod(method.Name);
                if (overriden != null)
                {
                    if (overriden.Modifier == Modifier.Final || overriden.Modifier == Modifier.Static)
                        throw new ScriptException(fileName, method, string.Format(Resources.MemberCantOverride, overriden.FullName));

                    //Note: I'm not sure it's wise to check this!
                    if (!method.MatchesSignature(overriden))
                        throw new ScriptException(fileName, method,
                            string.Format(Resources.MustMatchSignature, method.Name, overriden.FullName));
                }
            }

            foreach (ClassEventDecl _event in classDef.Events)
            {
                ClassEvent superEvent = superClass.GetEvent(_event.Name);
                if (superEvent != null)
                    throw new ScriptException(fileName, _event,
                        string.Format(Resources.EventDeclaredInAncestor, _event.Name, superEvent.Holder.Name));

                if (superClass.GetMember(_event.Name, MemberKind.All & ~MemberKind.Event) != null)
                    throw new ScriptException(fileName, _event, string.Format(Resources.EventHidesHomonymous, _event.Name));
            }

            ClassMethod constructor = null;
            if (classDef.Constructor != null)
            {
                constructor = (ClassMethod)classDef.Constructor.ToClassMember();
                constructor.Attributes = ConvertAttributes(classDef.Constructor.Attributes);

                for (int i = 0; i < constructor.Function.Parameters.Length; ++i)
                    constructor.Function.Parameters[i].Attributes =
                        ConvertAttributes(classDef.Constructor.Parameters[i].Attributes);
            }

            ClassProperty indexer = null;
            if (classDef.Indexer != null)
            {
                indexer = (ClassProperty)classDef.Indexer.ToClassMember();
                indexer.Attributes = ConvertAttributes(classDef.Indexer.Attributes);
            }

            var fields = classDef.Fields.Select(f => {
                var field = (ClassField)f.ToClassMember();
                field.Attributes = ConvertAttributes(f.Attributes);
                return field;
            });

            var properties = classDef.Properties.Select(p => {
                var property = (ClassProperty)p.ToClassMember();
                property.Attributes = ConvertAttributes(p.Attributes);
                return property;
            });

            var methods = classDef.Methods.Select(m => {
                var method = (ClassMethod)m.ToClassMember();
                method.Attributes = ConvertAttributes(m.Attributes);

                for (int i = 0; i < method.Function.Parameters.Length; ++i)
                    method.Function.Parameters[i].Attributes = ConvertAttributes(m.Parameters[i].Attributes);

                return method;
            });

            var events = classDef.Events.Select(e => {
                var _event = (ClassEvent)e.ToClassMember();
                _event.Attributes = ConvertAttributes(e.Attributes);
                return _event;
            });

            var klass = new Class(superClass, classDef.ClassName, classDef.Modifier,
                                  constructor, indexer, fields, properties, methods, events)
            {
                Attributes = ConvertAttributes(classDef.Attributes)
            };

            rootFrame.RootBlock.PutItem(classDef.ClassName, klass);
            InitializeFields(klass);
        }

        public void TranslateFunctionDecl(FunctionDecl fnDecl)
        {
            if (rootFrame.RootBlock.GetItem(fnDecl.Name) != null)
                throw new RuntimeException(fileName, fnDecl, string.Format(Resources.NameConflict, fnDecl.Name));

            Function function = fnDecl.ToFunction();
            function.Attributes = ConvertAttributes(fnDecl.Attributes);

            for (int i = 0; i < function.Parameters.Length; ++i)
                function.Parameters[i].Attributes = ConvertAttributes(fnDecl.Parameters[i].Attributes);

            rootFrame.RootBlock.PutItem(fnDecl.Name, function);
        }

        public void TranslateExternalFunctionDecl(ExternalFunctionDecl extDecl)
        {
            if (rootFrame.RootBlock.GetItem(extDecl.Name) != null)
                throw new RuntimeException(fileName, extDecl, string.Format(Resources.NameConflict, extDecl.Name));

            const string IMPORT_ATTRIBUTE_NAME = "LibImport";
            const string TYPE_ATTRIBUTE_NAME = "Type";

            AttributeDecl importAttribute = extDecl.GetAttribute(IMPORT_ATTRIBUTE_NAME) ??
                throw new RuntimeException(fileName, extDecl,
                    string.Format(Resources.MissingAttribute, IMPORT_ATTRIBUTE_NAME, extDecl.Name));

            PropertyInitializer libNameProperty = importAttribute.GetPropertyInitializer(AttributeDecl.DEFAULT_FIELD_NAME) ??
                throw new ScriptException(fileName, importAttribute,
                    string.Format(Resources.MissingAttributeProperty, AttributeDecl.DEFAULT_FIELD_NAME, IMPORT_ATTRIBUTE_NAME));

            libNameProperty.Expression.AcceptTranslator(this);
            string libName = WithNativeLibraryExtension(returnedValue.ToString());

            string procName = extDecl.Name;
            PropertyInitializer procNameProperty = importAttribute.GetPropertyInitializer("procName");
            if (procNameProperty != null)
            {
                procNameProperty.Expression.AcceptTranslator(this);
                procName = returnedValue.ToString();
            }

            Type returnType = typeof(void), defaultParamType = typeof(object);

            PropertyInitializer returnTypeProperty = importAttribute.GetPropertyInitializer("returnType");
            if (returnTypeProperty != null)
            {
                returnTypeProperty.Expression.AcceptTranslator(this);
                string returnTypeName = returnedValue.ToString();

                returnType = GetTypeByName(returnTypeName) ??
                    throw new ScriptException(fileName, returnTypeProperty,
                        string.Format(Resources.InvalidTypeReference, returnTypeName));
            }

            var paramTypes = new Type[extDecl.Parameters.Length];
            var args = new Expression[extDecl.Parameters.Length];

            for (int i = 0; i < extDecl.Parameters.Length; ++i)
            {
                ParameterDecl parameter = extDecl.Parameters[i];
                AttributeDecl typeAttribute = parameter.GetAttribute(TYPE_ATTRIBUTE_NAME);

                if (typeAttribute == null)
                    paramTypes[i] = defaultParamType;
                else
                {

                    PropertyInitializer typeNameProperty = typeAttribute.GetPropertyInitializer(AttributeDecl.DEFAULT_FIELD_NAME) ??
                        throw new ScriptException(fileName, typeAttribute,
                            string.Format(Resources.MissingAttributeProperty, AttributeDecl.DEFAULT_FIELD_NAME, TYPE_ATTRIBUTE_NAME));

                    typeNameProperty.Expression.AcceptTranslator(this);
                    string typeName = returnedValue.ToString();

                    Type parameterType = GetTypeByName(typeName) ??
                        throw new ScriptException(fileName, typeAttribute,
                            string.Format(Resources.InvalidTypeReference, typeName));

                    paramTypes[i] = parameterType;
                }

                args[i] = new VariableRef(parameter.Name);
            }

            MethodInfo method = GetPInvokeMethod(libName, procName, returnType, paramTypes);
            var extFnCall = new ExternalFunctionCall(method, args);
            var fnParams = extDecl.Parameters.Select(p => p.ToParameter()).ToArray();
            var function = new Function(fnParams, Block.Return(extFnCall)); // No attribute retention
            rootFrame.RootBlock.PutItem(extDecl.Name, function);
        }

        public void TranslateConstantDecl(ConstantDecl cstDecl)
        {
            try
            {
                foreach (PropertyInitializer initializer in cstDecl.Initializers)
                {
                    MethodFrame frame = currentFrame;
                    IFrameItem frameItem = frame.GetItem(initializer.Name);

                    if (frameItem == null && frame != rootFrame)
                    {
                        frame = rootFrame;
                        frameItem = frame.GetItem(initializer.Name);
                    }

                    if (frameItem == null)
                    {
                        initializer.Expression.AcceptTranslator(this);
                        currentFrame.PutItem(initializer.Name, new Constant(returnedValue));
                    }
                    else if (frame != currentFrame)
                        switch (frameItem.Kind)
                        {
                            case FrameItemKind.Constant:
                            case FrameItemKind.Variable:
                                initializer.Expression.AcceptTranslator(this);
                                currentFrame.PutItem(initializer.Name, new Constant(returnedValue));
                                break;
                            default:
                                throw new ScriptException(fileName, initializer,
                                    string.Format(Resources.NameConflict, initializer.Name));
                        }
                    else
                        throw new ScriptException(fileName, initializer, string.Format(Resources.NameConflict, initializer.Name));
                }
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, cstDecl, ex);
            }
        }

        public void TranslateVariableDecl(VariableDecl varDecl)
        {
            foreach (PropertyInitializer initializer in varDecl.Initializers)
            {
                MethodFrame frame = currentFrame;
                IFrameItem frameItem = frame.GetItem(initializer.Name);

                if (frameItem == null && frame != rootFrame)
                {
                    frame = rootFrame;
                    frameItem = frame.GetItem(initializer.Name);
                }

                if (frameItem == null)
                {
                    if (initializer.Expression == null)
                        returnedValue = Undefined.Value;
                    else
                        initializer.Expression.AcceptTranslator(this);

                    currentFrame.PutItem(initializer.Name, returnedValue);
                }
                else if (frame != currentFrame)
                    switch (frameItem.Kind)
                    {
                        case FrameItemKind.Constant:
                        case FrameItemKind.Variable:
                            if (initializer.Expression == null)
                                returnedValue = Undefined.Value;
                            else
                                initializer.Expression.AcceptTranslator(this);

                            currentFrame.PutItem(initializer.Name, returnedValue);
                            break;
                        default:
                            throw new ScriptException(fileName, initializer,
                                string.Format(Resources.NameConflict, initializer.Name));
                    }
                else
                    throw new ScriptException(fileName, initializer, string.Format(Resources.NameConflict, initializer.Name));
            }
        }

        public void TranslateBlock(Block block)
        {
            currentFrame.PushBlock();

            foreach (KeyValuePair<string, Label> pair in block.Labels)
                RegisterLabel(pair.Key, pair.Value);

            try
            {
                int counter = 0;

                while (counter < block.Statements.Length)
                {
                    block.Statements[counter].AcceptTranslator(this);

                    switch (jumpCode)
                    {
                        case JumpCode.None:
                            ++counter;
                            break;
                        case JumpCode.Goto:
                            if (block.Labels.TryGetValue(lastGoto.LabelName, out Label value))
                            {
                                counter = value.Address;
                                jumpCode = JumpCode.None;
                            }
                            else
                                counter = int.MaxValue;
                            break;
                        default:
                            counter = int.MaxValue;
                            break;
                    }
                }
            }
            finally
            {
                currentFrame.PopBlock();
            }
        }

        public void TranslateAssignment(Assignment assignment)
        {
            try
            {
                if (assignment.Operator == BinaryOperator.None)
                    assignment.RightOperand.AcceptTranslator(this);
                else
                    TranslateBinaryExpression(assignment);

                Assign(assignment.LeftOperand, returnedValue);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, assignment, ex);
            }
        }

        public void TranslateTernaryExpression(TernaryExpression terExpr)
        {
            if (IsTrue(terExpr.Test))
                terExpr.TruePart.AcceptTranslator(this);
            else
                terExpr.FalsePart.AcceptTranslator(this);
        }

        public void TranslateBinaryExpression(BinaryExpression binExpr)
        {
            binExpr.LeftOperand.AcceptTranslator(this);
            DataItem leftOperand = returnedValue;

            if ((binExpr.Operator == BinaryOperator.AndAlso && !leftOperand.AsBoolean) ||
                (binExpr.Operator == BinaryOperator.OrElse && leftOperand.AsBoolean) ||
                (binExpr.Operator == BinaryOperator.IfEmpty && !leftOperand.IsEmpty())) return;

            try
            {
                if (leftOperand.Class.Inherits(Class.Object) && IsOverloadable(binExpr.Operator))
                {
                    string methodName = ClassMethod.GetMethodName(binExpr.Operator);
                    ClassMethod method = leftOperand.Class.GetMethod(methodName);
                    
                    if (method != null)
                    {
                        // Handle overloaded operators
                        CheckAccess(method, binExpr);
                        Invoke(method.Function, methodName, method.Holder, leftOperand, binExpr.RightOperand);
                        return;
                    }

                    if (binExpr.Operator == BinaryOperator.Plus && binExpr.RightOperand is Literal literal &&
                        literal.Value.Class == Class.String)
                    {
                        // Handle string concatenation when the left operand is an object
                        new MethodCall(new Literal(leftOperand), "toString").AcceptTranslator(this);
                        leftOperand = returnedValue;
                    }
                    else if (!(binExpr.Operator == BinaryOperator.Equal || binExpr.Operator == BinaryOperator.NotEqual))
                    {
                        // Handle equality/difference check: the corresponding operators don't have to be overloaded in general
                        throw new RuntimeException(fileName, binExpr, string.Format(Resources.OperatorCantBeApplied,
                            CodeGenerator.BinaryOperatorToString(binExpr.Operator), leftOperand.Class.Name));
                    }
                }

                binExpr.RightOperand.AcceptTranslator(this);

                if (binExpr.Operator == BinaryOperator.AndAlso || binExpr.Operator == BinaryOperator.OrElse ||
                    binExpr.Operator == BinaryOperator.IfEmpty) return;

                DataItem rightOperand = returnedValue;
                returnedValue = leftOperand.ConversionNeeded(rightOperand.Class, binExpr.Operator)
                              ? leftOperand.ConvertTo(rightOperand.Class).BinaryOperation(binExpr.Operator, rightOperand)
                              : leftOperand.BinaryOperation(binExpr.Operator, rightOperand);
            }
            catch (InvalidCastException)
            {
                switch (binExpr.Operator)
                {
                    case BinaryOperator.Equal:
                        returnedValue = Boolean.False;
                        break;
                    case BinaryOperator.NotEqual:
                        returnedValue = Boolean.True;
                        break;
                    default:
                        throw;
                }
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, binExpr, ex);
            }
        }

        public void TranslateUnaryExpression(UnaryExpression unExpr)
        {
            unExpr.Operand.AcceptTranslator(this);
            DataItem operand = returnedValue;

            try
            {
                if (operand.Class.Inherits(Class.Object) && IsOverloadable(unExpr.Operator, out bool postfix))
                {
                    string methodName = ClassMethod.GetMethodName(unExpr.Operator);
                    ClassMethod method = operand.Class.GetMethod(methodName) ??
                        throw new RuntimeException(fileName, unExpr, string.Format(Resources.OperatorCantBeApplied,
                            CodeGenerator.UnaryOperatorToString(unExpr.Operator), operand.Class.Name));

                    CheckAccess(method, unExpr);

                    Expression[] args = postfix ? [new Literal()] : [];
                    Invoke(method.Function, methodName, method.Holder, operand, args);
                }
                else
                    switch (unExpr.Operator)
                    {
                        case UnaryOperator.PreIncrement:
                            Assign(unExpr.Operand, new Integer(operand.AsInt32 + 1));
                            break;
                        case UnaryOperator.PostIncrement:
                            Assign(unExpr.Operand, new Integer(operand.AsInt32 + 1));
                            returnedValue = operand;
                            break;
                        case UnaryOperator.PreDecrement:
                            Assign(unExpr.Operand, new Integer(operand.AsInt32 - 1));
                            break;
                        case UnaryOperator.PostDecrement:
                            Assign(unExpr.Operand, new Integer(operand.AsInt32 - 1));
                            returnedValue = operand;
                            break;
                        case UnaryOperator.NotEmpty:
                            if (operand.IsEmpty())
                                throw new RuntimeException(fileName, unExpr, Resources.ValueShouldNotBeEmpty);
                            break;
                        default:
                            returnedValue = operand.UnaryOperation(unExpr.Operator);
                            break;
                    }
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, unExpr, ex);
            }
        }

        public void TranslateLiteral(Literal literal)
        {
            returnedValue = literal.Value;
        }

        public void TranslateComplexInitializer(ComplexInitializer cplxInit)
        {
            try
            {
                cplxInit.RealPartInitializer.AcceptTranslator(this);
                double realPart = returnedValue.AsDouble;

                cplxInit.ImaginaryPartInitializer.AcceptTranslator(this);
                double imaginaryPart = returnedValue.AsDouble;

                returnedValue = new Complex(realPart, imaginaryPart);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, cplxInit, ex);
            }
        }

        public void TranslateListInitializer(ListInitializer listInit)
        {
            var list = new List<DataItem>();

            foreach (Expression item in listInit.Items)
            {
                try
                {
                    item.AcceptTranslator(this);
                    list.Add(returnedValue);
                }
                catch (ScriptException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ScriptException(fileName, item, ex);
                }
            }

            returnedValue = new List(list);
        }

        public void TranslateMapInitializer(MapInitializer mapInit)
        {
            var dict = new Dictionary<DataItem, DataItem>();

            foreach (MapItemInitializer item in mapInit.ItemInitializers)
            {
                try
                {
                    item.Key.AcceptTranslator(this);
                    DataItem key = returnedValue;

                    item.Value.AcceptTranslator(this);
                    dict.Add(key, returnedValue);
                }
                catch (ScriptException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ScriptException(fileName, item, ex);
                }
            }

            returnedValue = new Map(dict);
        }

        public void TranslateSetInitializer(SetInitializer setInit)
        {
            var hashSet = new HashSet<DataItem>();

            foreach (Expression item in setInit.Items)
            {
                try
                {
                    item.AcceptTranslator(this);
                    hashSet.Add(returnedValue);
                }
                catch (ScriptException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ScriptException(fileName, item, ex);
                }
            }

            returnedValue = new Set(hashSet);
        }

        public void TranslateObjectInitializer(ObjectInitializer objInit)
        {
            var fields = new Dictionary<string, DataItem>();

            foreach (PropertyInitializer propInit in objInit.PropertyInitializers)
            {
                try
                {
                    propInit.Expression.AcceptTranslator(this);
                    fields.Add(propInit.Name, returnedValue);
                }
                catch (ScriptException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ScriptException(fileName, propInit, ex);
                }
            }

            returnedValue = new Object(fields);
        }

        public void TranslateInlineFunction(InlineFunction inlineFn)
        {
            Function function = inlineFn.ToFunction();
            function.DeclaringFrame = currentFrame == rootFrame ? null : currentFrame;

            for (int i = 0; i < function.Parameters.Length; ++i)
                function.Parameters[i].Attributes = ConvertAttributes(inlineFn.Parameters[i].Attributes);

            returnedValue = new Closure(function);
        }

        public void TranslateVariableRef(VariableRef varRef)
        {
            IFrameItem frameItem = currentFrame.GetItem(varRef.Name);
            if (frameItem == null && currentFrame != rootFrame)
                frameItem = rootFrame.GetItem(varRef.Name);

            if (frameItem == null)
                switch (misRefAct)
                {
                    case MissingReferenceAction.Create:
                        currentFrame.PutItem(varRef.Name, returnedValue = Undefined.Value);
                        break;
                    case MissingReferenceAction.Fail:
                        throw new RuntimeException(fileName, varRef, string.Format(Resources.UndefinedVariable, varRef.Name));
                }
            else
                switch (frameItem.Kind)
                {
                    case FrameItemKind.Variable:
                        {
                            var variable = (DataItem)frameItem;
                            if (variable == Undefined.Value && misRefAct == MissingReferenceAction.Fail)
                                throw new RuntimeException(fileName, varRef, string.Format(Resources.UninitializedVariable, varRef.Name));
                            returnedValue = variable;
                        }
                        break;
                    case FrameItemKind.Constant:
                        returnedValue = ((Constant)frameItem).Value;
                        break;
                    case FrameItemKind.Function:
                        returnedValue = new Closure((Function)frameItem);
                        break;
                    default:
                        throw new RuntimeException(fileName, varRef, string.Format(Resources.NotAVariable, varRef.Name));
                }
        }

        public void TranslateItemRef(ItemRef itemRef)
        {
            try
            {
                DataItem itemValue;

                ResolveItemRef(itemRef, out DataItem owner, out DataItem index, out ClassProperty indexer);

                if (owner == null)
                    itemValue = null;
                else if (owner == Void.Value && itemRef.Optional)
                    itemValue = Void.Value;
                else if (indexer == null)
                    itemValue = owner.GetItem(index);
                else
                {
                    if (!indexer.CanRead)
                        throw new RuntimeException(fileName, itemRef, Resources.CannotReadProperty);

                    CheckAccess(indexer.Reader, itemRef);
                    Invoke(indexer.Reader.Function, indexer.Name, indexer.Holder, owner, new Literal(index));
                    return;
                }

                if (itemValue == null && misRefAct != MissingReferenceAction.Ignore)
                    throw new RuntimeException(fileName, itemRef, string.Format(Resources.IndexNotFound, index));

                returnedValue = itemValue;
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, itemRef, ex);
            }
        }

        public void TranslateSliceRef(SliceRef sliceRef)
        {
            try
            {
                int lBound = 0, uBound = 0;

                if (sliceRef.LowerBound != null)
                {
                    sliceRef.LowerBound.AcceptTranslator(this);
                    lBound = returnedValue.AsInt32;
                }

                if (sliceRef.UpperBound != null)
                {
                    sliceRef.UpperBound.AcceptTranslator(this);
                    uBound = returnedValue.AsInt32;
                }

                sliceRef.Owner.AcceptTranslator(this);
                DataItem owner = returnedValue;

                switch (owner.Class.ClassID)
                {
                    case ClassID.Void:
                        if (!sliceRef.Optional) goto default;
                        returnedValue = Void.Value;
                        break;
                    case ClassID.String:
                        {
                            string str = owner.ToString();
                            while (lBound < 0) lBound += str.Length;
                            while (uBound <= 0) uBound += str.Length;
                            returnedValue = new String(str[lBound..uBound]);
                        }
                        break;
                    case ClassID.List:
                        {
                            List<DataItem> lst = owner.AsList;
                            while (lBound < 0) lBound += lst.Count;
                            while (uBound <= 0) uBound += lst.Count;
                            returnedValue = new List(lst[lBound..uBound]);
                        }
                        break;
                    default:
                        throw new RuntimeException(fileName, sliceRef,
                            string.Format(Resources.SlicingNotSupported, owner.Class.Name));
                }
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, sliceRef, ex);
            }
        }

        public void TranslatePropertyRef(PropertyRef propertyRef)
        {
            try
            {
                DataItem propValue;

                ResolvePropertyRef(propertyRef, out DataItem owner, out ClassMember member);

                if (owner == null)
                    propValue = null;
                else if (owner == Void.Value && propertyRef.Optional)
                    propValue = Void.Value;
                else if (member == null)
                    propValue = owner.GetProperty(propertyRef.PropertyName);
                else if (member is ClassField field)
                    propValue = (member.Modifier == Modifier.Static) || (member.Modifier == Modifier.StaticFinal)
                              ? field.SharedValue
                              : owner.GetProperty(member.Name);
                else if (member is ClassMethod method)
                {
                    var parameters = method.Function.Parameters;
                    var args = parameters.Select(p => new VariableRef(p.Name)).ToArray();
                    var fn = new Function(parameters, Block.Return(new MethodCall(new Literal(owner), method.Name, args)));
                    propValue = new Closure(fn);
                }
                else // member is surely a ClassProperty
                {
                    var property = (ClassProperty)member;
                    if (!property.CanRead) throw new RuntimeException(fileName, propertyRef, Resources.CannotReadProperty);

                    CheckAccess(property.Reader, propertyRef);
                    Invoke(property.Reader.Function, property.Name, property.Holder, owner);
                    return;
                }

                if (propValue == null && misRefAct != MissingReferenceAction.Ignore)
                    throw new RuntimeException(fileName, propertyRef,
                        string.Format(Resources.PropertyNotFoundInObject, propertyRef.PropertyName));

                returnedValue = propValue;
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, propertyRef, ex);
            }
        }

        public void TranslateStaticPropertyRef(StaticPropertyRef staticRef)
        {
            object targetProperty = ResolveName(staticRef.Name, staticRef);

            try
            {
                if (targetProperty is ClassField field)
                    returnedValue = field.SharedValue;
                else if (targetProperty is ClassProperty property)
                {
                    if (!property.CanRead)
                        throw new RuntimeException(fileName, staticRef, Resources.CannotReadProperty);

                    CheckAccess(property.Reader, staticRef);
                    Invoke(property.Reader.Function, property.Name, property.Holder, null);
                }
                else if (targetProperty is ClassMethod method)
                    returnedValue = new Closure(method.Function);
                else if (targetProperty is StaticTypeMember member)
                    returnedValue = member.GetValue();
                else
                    throw new RuntimeException(fileName, staticRef, string.Format(Resources.UnresolvedMemberRef, staticRef.Name));
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, staticRef, ex);
            }
        }

        public void TranslateSelfReference(SelfReference selfRef)
        {
            returnedValue = currentFrame.Context.MethodTarget;
        }

        public void TranslateFunctionCall(FunctionCall fnCall)
        {
            try
            {
                Function function = null;
                InvocationContext ctx = currentFrame.Context;

                IFrameItem frameItem = currentFrame.GetItem(fnCall.FunctionName);
                if (frameItem == null && currentFrame != rootFrame)
                    frameItem = rootFrame.GetItem(fnCall.FunctionName);

                if (frameItem != null)
                    switch (frameItem.Kind)
                    {
                        case FrameItemKind.Function:
                            function = (Function)frameItem;
                            break;
                        case FrameItemKind.Variable:
                            if (frameItem is Closure closure)
                            {
                                function = closure.AsFunction;
                                if (function.DeclaringFrame != null)
                                    ctx = function.DeclaringFrame.Context;
                            }
                            break;
                    }

                if (function == null)
                    throw new RuntimeException(fileName, fnCall, string.Format(Resources.UndefinedFunction, fnCall.FunctionName));

                Invoke(function, fnCall.FunctionName, ctx.MethodHolder, ctx.MethodTarget, fnCall.Arguments, fnCall.NamedArgs);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, fnCall, ex);
            }
        }

        public void TranslateAnonymousCall(AnonymousCall anCall)
        {
            try
            {
                anCall.Callee.AcceptTranslator(this);

                DataItem target = returnedValue;
                if (target is not Closure) throw new RuntimeException(fileName, anCall.Callee, Resources.CalleeIsNotClosure);

                Function function = target.AsFunction;
                InvocationContext ctx = function.DeclaringFrame != null ? function.DeclaringFrame.Context : currentFrame.Context;
                Invoke(function, anCall.FunctionName, ctx.MethodHolder, ctx.MethodTarget, anCall.Arguments, anCall.NamedArgs);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, anCall, ex);
            }
        }

        public void TranslateMethodCall(MethodCall methodCall)
        {
            try
            {
                methodCall.Target.AcceptTranslator(this);
                
                DataItem methodTarget = returnedValue;
                if (methodTarget == Void.Value && methodCall.Optional) return;

                ClassMethod method = methodTarget.Class.GetMethod(methodCall.FunctionName);
                Class methodHolder = currentFrame.Context.MethodHolder;
                Function function = null;

                if (method == null)
                    switch (methodTarget.Class.ClassID)
                    {
                        case ClassID.Object:
                            ClassField field = methodTarget.Class.GetField(methodCall.FunctionName);
                            if (field != null) CheckAccess(field, methodCall);

                            DataItem fieldValue = field != null && (field.Modifier == Modifier.Static || field.Modifier == Modifier.StaticFinal)
                                                ? field.SharedValue
                                                : methodTarget.GetProperty(methodCall.FunctionName);

                            if (fieldValue is Closure) function = fieldValue.AsFunction;
                            break;
                        case ClassID.Resource:
                            object target = methodTarget.AsNativeObject;
                            InvokeNative(target.GetType(), methodCall.FunctionName, target, methodCall.Arguments);
                            return; // IMPORTANT!!!
                    }
                else
                {
                    CheckAccess(method, methodCall);
                    function = method.Function;
                    methodHolder = method.Holder;
                }

                if (function == null)
                    throw new RuntimeException(fileName, methodCall,
                        string.Format(Resources.MethodNotFound, methodCall.FunctionName, methodTarget.Class.Name));

                Invoke(function, methodCall.FunctionName, methodHolder, methodTarget, methodCall.Arguments, methodCall.NamedArgs);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, methodCall, ex);
            }
        }

        public void TranslateStaticMethodCall(StaticMethodCall staticCall)
        {
            object targetMethod = ResolveName(staticCall.Name, staticCall);

            try
            {
                if (targetMethod is ClassMethod method)
                    Invoke(method.Function, method.Name, method.Holder, null, staticCall.Arguments, staticCall.NamedArgs);
                else if (targetMethod is ClassField field)
                {
                    Function function = null;
                    if (field.SharedValue is Closure)
                        function = field.SharedValue.AsFunction;

                    if (function == null)
                        throw new RuntimeException(fileName, staticCall, string.Format(Resources.MethodNotFound, field.Name, staticCall.Name));

                    Invoke(function, field.Name, currentFrame.Context.MethodHolder, null, staticCall.Arguments, staticCall.NamedArgs);
                }
                else if (targetMethod is StaticTypeMember member)
                    InvokeNative(member.Type, member.MemberName, null, staticCall.Arguments);
                else
                    throw new RuntimeException(fileName, staticCall, string.Format(Resources.UnresolvedMemberRef, staticCall.Name));
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, staticCall, ex);
            }
        }

        public void TranslateConstructorCall(ConstructorCall ctorCall)
        {
            object targetType = ResolveName(ctorCall.Name, ctorCall);
            
            try
            {
                if (targetType is Class klass)
                {
                    if (klass.Modifier == Modifier.Abstract || klass.Modifier == Modifier.Static)
                        throw new RuntimeException(fileName, ctorCall, string.Format(Resources.CannotCreateInstance, klass.Name));

                    ClassMethod constructor = klass.Constructor;
                    CheckAccess(constructor, ctorCall);

                    DataItem _this = new Object(klass);
                    InitializeFields(_this);
                    Invoke(constructor.Function, constructor.Name, klass, _this, ctorCall.Arguments, ctorCall.NamedArgs);

                    if (ctorCall.PropertyInitializers != null)
                        ApplyPropertyInitializers(ctorCall, _this, ctorCall.PropertyInitializers);

                    returnedValue = _this;
                }
                else if (targetType is Type type)
                {
                    DataItem[] args;

                    if (ctorCall.Arguments == null)
                        args = [];
                    else
                    {
                        args = new DataItem[ctorCall.Arguments.Length];
                        for (int i = 0; i < ctorCall.Arguments.Length; ++i)
                        {
                            ctorCall.Arguments[i].AcceptTranslator(this);
                            args[i] = returnedValue;
                        }
                    }

                    const BindingFlags flags = BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;
                    object obj = type.InvokeMember(null, flags, new DataItemBinder(), null, args);
                    DataItem _this = DataItemFactory.CreateDataItem(obj);

                    if (ctorCall.PropertyInitializers != null)
                        foreach (PropertyInitializer initializer in ctorCall.PropertyInitializers)
                        {
                            initializer.Expression.AcceptTranslator(this);
                            _this.SetProperty(initializer.Name, returnedValue);
                        }

                    returnedValue = _this;
                }
                else
                    throw new RuntimeException(fileName, ctorCall, string.Format(Resources.UndefinedType, ctorCall.Name));
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, ctorCall, ex);
            }
        }

        public void TranslateParentMethodCall(ParentMethodCall pmc)
        {
            try
            {
                DataItem _this = currentFrame.Context.MethodTarget;
                ClassMethod method = _this.Class.SuperClass.GetMethod(pmc.FunctionName) ??
                    throw new RuntimeException(fileName, pmc,
                        string.Format(Resources.MethodNotFound, pmc.FunctionName, _this.Class.SuperClass.Name));
                
                if (method.Modifier == Modifier.Abstract)
                    throw new RuntimeException(fileName, pmc, string.Format(Resources.CannotInvokeAbstractMember, method.FullName));
                
                CheckAccess(method, pmc);
                Invoke(method.Function, pmc.FunctionName, method.Holder, _this, pmc.Arguments, pmc.NamedArgs);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, pmc, ex);
            }
        }

        public void TranslateParentConstructorCall(ParentConstructorCall pcc)
        {
            try
            {
                InvocationContext context = currentFrame.Context;
                ClassMethod constructor = context.MethodHolder.SuperClass.Constructor;
                CheckAccess(constructor, pcc);
                Invoke(constructor.Function, constructor.Name, constructor.Holder, context.MethodTarget, pcc.Arguments, pcc.NamedArgs);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, pcc, ex);
            }
        }

        public void TranslateParentPropertyRef(ParentPropertyRef ppr)
        {
            try
            {
                Class superClass = currentFrame.Context.MethodHolder.SuperClass;
                ClassMember member = superClass.GetMember(ppr.PropertyName, MemberKind.Property | MemberKind.Method) ??
                    throw new RuntimeException(fileName, ppr,
                        string.Format(Resources.PropertyNotFoundInClass, ppr.PropertyName, superClass.Name));

                if (member.Modifier == Modifier.Abstract)
                    throw new RuntimeException(fileName, ppr, string.Format(Resources.CannotInvokeAbstractMember, member.FullName));

                CheckAccess(member, ppr);

                if (member is ClassProperty property)
                {
                    if (!property.CanRead) throw new RuntimeException(fileName, ppr, Resources.CannotReadProperty);

                    Invoke(property.Reader.Function, property.Name, property.Holder, currentFrame.Context.MethodTarget);
                }
                else // member is surely a ClassMethod
                {
                    // Cast 'this' to an instance of 'superClass'
                    var oldTarget = (Object)currentFrame.Context.MethodTarget;
                    var newTarget = new Object(superClass, oldTarget.AsDynamicObject);
                    
                    // Generate arguments from parameters
                    var parameters = ((ClassMethod)member).Function.Parameters;
                    var args = parameters.Select(p => new VariableRef(p.Name)).ToArray();
                    
                    // Create a closure wrapping a function that will invoke the original method's implementation
                    var function = new Function(parameters, Block.Return(new MethodCall(new Literal(newTarget), member.Name, args)));
                    returnedValue = new Closure(function);
                }
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, ppr, ex);
            }
        }

        public void TranslateParentIndexerRef(ParentIndexerRef pir)
        {
            try
            {
                Class superClass = currentFrame.Context.MethodHolder.SuperClass;
                ClassProperty indexer = superClass.Indexer ??
                    throw new RuntimeException(fileName, pir, string.Format(Resources.ClassHasNoIndexer, superClass.Name));

                if (indexer.Modifier == Modifier.Abstract)
                    throw new RuntimeException(fileName, pir, string.Format(Resources.CannotInvokeAbstractMember, indexer.FullName));

                if (!indexer.CanRead)
                    throw new RuntimeException(fileName, pir, Resources.CannotReadProperty);

                CheckAccess(indexer, pir);
                Invoke(indexer.Reader.Function, indexer.Name, indexer.Holder, currentFrame.Context.MethodTarget, pir.Index);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, pir, ex);
            }
        }

        public void TranslateInnerFunctionCall(InnerFunctionCall innerCall)
        {
            var arguments = new DataItem[innerCall.Arguments.Length];

            for (int i = 0; i < arguments.Length; ++i)
            {
                innerCall.Arguments[i].AcceptTranslator(this);
                arguments[i] = returnedValue;
            }

            returnedValue = innerCall.Function.Logic(arguments);
        }

        public void TranslateExternalFunctionCall(ExternalFunctionCall extCall)
        {
            ParameterInfo[] parameters = extCall.Method.GetParameters();
            var args = new object[extCall.Arguments.Length];

            for (int i = 0; i < extCall.Arguments.Length; ++i)
            {
                extCall.Arguments[i].AcceptTranslator(this);
                args[i] = returnedValue.ConvertTo(parameters[i].ParameterType);
            }

            object obj = extCall.Method.Invoke(null, args);
            returnedValue = DataItemFactory.CreateDataItem(obj);
        }

        public void TranslateTypeVerification(TypeVerification typeVerif)
        {
            MissingReferenceAction prevAction = misRefAct;

            try
            {
                if (rootFrame.GetItem(typeVerif.TypeName) is not Class klass)
                    throw new RuntimeException(fileName, typeVerif, string.Format(Resources.UndefinedType, typeVerif.TypeName));

                misRefAct = MissingReferenceAction.Ignore;
                typeVerif.Expression.AcceptTranslator(this);
                DataItem retVal = returnedValue;
                returnedValue = Boolean.FromBool((retVal == null && klass == Class.Void) || retVal.InstanceOf(klass));
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, typeVerif, ex);
            }
            finally
            {
                misRefAct = prevAction;
            }
        }

        public void TranslateTypeOfExpression(TypeOfExpression typeOf)
        {
            if (rootFrame.GetItem(typeOf.TypeName) is not Class klass)
                throw new RuntimeException(fileName, typeOf,
                    string.Format(Resources.UndefinedType, typeOf.TypeName));

            returnedValue = GetTypeInfo(klass);
        }

        public void TranslateConversion(Conversion conversion)
        {
            try
            {
                conversion.Expression.AcceptTranslator(this);

                DataItem converted = returnedValue;
                if (converted.Class.Inherits(Class.Object))
                    throw new RuntimeException(fileName, conversion.Expression,
                        string.Format(Resources.CannotConvertFrom, converted.Class.Name));

                if (rootFrame.GetItem(conversion.TypeName) is not Class klass)
                    throw new RuntimeException(fileName, conversion,
                        string.Format(Resources.UndefinedType, conversion.TypeName));
                
                if (klass.ClassID < ClassID.Boolean || klass.ClassID > ClassID.Object)
                    throw new RuntimeException(fileName, conversion,
                        string.Format(Resources.CannotConvertTo, conversion.TypeName));

                returnedValue = converted.ConvertTo(klass);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, conversion, ex);
            }
        }

        public void TranslateIfElse(IfElse ifElse)
        {
            if (IsTrue(ifElse.Condition))
                ifElse.PositiveAction.AcceptTranslator(this);
            else
                ifElse.NegativeAction?.AcceptTranslator(this);
        }

        public void TranslateSwitchBlock(SwitchBlock switchBlock)
        {
            switchBlock.Expression.AcceptTranslator(this);
            int hashCode = returnedValue.GetHashCode();

            int counter = switchBlock.DefaultCase;
            foreach (CaseLabel caseLabel in switchBlock.Cases)
                if (caseLabel.GetHashCode() == hashCode)
                {
                    counter = caseLabel.Address;
                    break;
                }

            currentFrame.PushBlock();

            foreach (KeyValuePair<string, Label> pair in switchBlock.Labels)
                RegisterLabel(pair.Key, pair.Value);

            try
            {
                while (counter < switchBlock.Statements.Length)
                {
                    switchBlock.Statements[counter].AcceptTranslator(this);

                    switch (jumpCode)
                    {
                        case JumpCode.None:
                            ++counter;
                            break;
                        case JumpCode.Break:
                            jumpCode = JumpCode.None;
                            counter = int.MaxValue;
                            break;
                        case JumpCode.Goto:
                            if (switchBlock.Labels.TryGetValue(lastGoto.LabelName, out Label label))
                            {
                                counter = label.Address;
                                jumpCode = JumpCode.None;
                            }
                            else
                                counter = int.MaxValue;
                            break;
                        default:
                            counter = int.MaxValue;
                            break;
                    }
                }
            }
            finally
            {
                currentFrame.PopBlock();
            }
        }

        public void TranslateForLoop(ForLoop forLoop)
        {
            currentFrame.PushBlock();

            foreach (Statement initializer in forLoop.Initializers)
                initializer.AcceptTranslator(this);

            Expression condition = forLoop.Guard ?? new Literal(Boolean.True);
            while (IsTrue(condition))
            {
                forLoop.Action.AcceptTranslator(this);
                switch (jumpCode)
                {
                    case JumpCode.Continue:
                        jumpCode = JumpCode.None;
                        break;
                    case JumpCode.Break:
                        jumpCode = JumpCode.None;
                        goto EXIT;
                    case JumpCode.Goto:
                    case JumpCode.Return:
                        goto EXIT;
                }

                foreach (Expression updater in forLoop.Updaters)
                    updater.AcceptTranslator(this);
            }

        EXIT:
            currentFrame.PopBlock();
        }

        public void TranslateForEachLoop(ForEachLoop forEach)
        {
            currentFrame.PushBlock();

            var enumerable = GetEnumerable(forEach.Enumerated);
            foreach (KeyValuePair<DataItem, DataItem> pair in enumerable)
            {
                RegisterVariable(forEach.KeyName, pair.Key);
                RegisterVariable(forEach.ValueName, pair.Value);

                forEach.Action.AcceptTranslator(this);

                switch (jumpCode)
                {
                    case JumpCode.Continue:
                        jumpCode = JumpCode.None;
                        break;
                    case JumpCode.Break:
                        jumpCode = JumpCode.None;
                        goto EXIT;
                    case JumpCode.Goto:
                    case JumpCode.Return:
                        goto EXIT;
                }
            }

        EXIT:
            currentFrame.PopBlock();
        }

        public void TranslateWhileLoop(WhileLoop whileLoop)
        {
            while (IsTrue(whileLoop.Guard))
            {
                whileLoop.Action.AcceptTranslator(this);

                switch (jumpCode)
                {
                    case JumpCode.Continue:
                        jumpCode = JumpCode.None;
                        break;
                    case JumpCode.Break:
                        jumpCode = JumpCode.None;
                        return;
                    case JumpCode.Goto:
                    case JumpCode.Return:
                        return;
                }
            }
        }

        public void TranslateDoLoop(DoLoop doLoop)
        {
            do
            {
                doLoop.Action.AcceptTranslator(this);

                switch (jumpCode)
                {
                    case JumpCode.Continue:
                        jumpCode = JumpCode.None;
                        break;
                    case JumpCode.Break:
                        jumpCode = JumpCode.None;
                        return;
                    case JumpCode.Goto:
                    case JumpCode.Return:
                        return;
                }
            } while (IsTrue(doLoop.Guard));
        }

        public void TranslateContinue(Continue _continue)
        {
            jumpCode = JumpCode.Continue;
        }

        public void TranslateBreak(Break _break)
        {
            jumpCode = JumpCode.Break;
        }

        public void TranslateGoto(Goto _goto)
        {
            lastGoto = _goto;
            jumpCode = JumpCode.Goto;
        }

        public void TranslateReturn(Return _return)
        {
            if (_return.Expression == null)
                returnedValue = Void.Value;
            else
                _return.Expression.AcceptTranslator(this);

            jumpCode = JumpCode.Return;
        }

        public void TranslateThrow(Throw _throw)
        {
            _throw.Expression.AcceptTranslator(this);
            DataItem thrown = returnedValue;

            if (thrown.InstanceOf(Class.Exception))
                throw new RuntimeException(fileName, _throw, thrown);

            throw new RuntimeException(fileName, _throw, thrown.ToString());
        }

        public void TranslateTryCatchFinally(TryCatchFinally tcf)
        {
            DataItem resource = null;
            ScriptException finalException = null;

            currentFrame.PushBlock();

            try
            {
                if (tcf.Resource != null)
                {
                    tcf.Resource.AcceptTranslator(this);
                    resource = returnedValue;
                }

                tcf.TryBlock.AcceptTranslator(this);
            }
            catch (ScriptException ex1)
            {
                if (tcf.CatchBlock == null)
                    finalException = ex1;
                else
                    try
                    {
                        currentFrame.PutItem(tcf.ExceptionName, ConvertException(ex1));
                        tcf.CatchBlock.AcceptTranslator(this);
                    }
                    catch (ScriptException ex2)
                    {
                        finalException = ex2;
                    }
            }
            finally
            {
                if (tcf.FinallyBlock != null)
                {
                    DataItem prevValue = returnedValue;
                    JumpCode prevCode = jumpCode;
                    Goto prevGoto = lastGoto;

                    try
                    {
                        jumpCode = JumpCode.None;
                        tcf.FinallyBlock.AcceptTranslator(this);

                        if (jumpCode == JumpCode.Goto)
                            finalException = new RuntimeException(fileName, lastGoto, Resources.CannotJumpOutOfFinallyBlock);
                    }
                    catch (ScriptException ex)
                    {
                        finalException = ex;
                    }
                    finally
                    {
                        returnedValue = prevValue;
                        jumpCode = prevCode;
                        lastGoto = prevGoto;
                    }
                }

                if (resource != null)
                    new MethodCall(new Literal(resource), "dispose").AcceptTranslator(this);

                currentFrame.PopBlock();

                if (finalException != null) throw finalException;
            }
        }

        public void TranslateStringInterpolation(StringInterpolation stringInt)
        {
            try
            {
                var listItems = new List<DataItem>();
                foreach (Expression substitution in stringInt.Substitions)
                {
                    substitution.AcceptTranslator(this);
                    listItems.Add(returnedValue);
                }

                var args = new Expression[] {
                    new Literal(new String(stringInt.Pattern)) ,
                    new Literal(new List(listItems))
                };

                var innerFnCall = new InnerFunctionCall(InnerFunction.Format, args);
                innerFnCall.CopyLocation(stringInt);
                innerFnCall.AcceptTranslator(this);
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, stringInt, ex);
            }
        }

        public void TranslatePatternMatching(PatternMatching patMatch)
        {
            try
            {
                patMatch.Expression.AcceptTranslator(this);

                var testArg = new Literal(returnedValue);

                foreach (MatchCase matchCase in patMatch.MatchCases)
                    if (IsTrue(matchCase.Pattern.GetMatchTest(testArg)))
                        try
                        {
                            currentFrame.PushBlock(new Dictionary<string, IFrameItem> {
                                [ClassProperty.WRITER_PARAMETER_NAME] = testArg.Value
                            });
                            matchCase.Expression.AcceptTranslator(this);
                            return;
                        }
                        finally
                        {
                            currentFrame.PopBlock();
                        }

                returnedValue = Void.Value;
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, patMatch, ex);
            }
        }

        public void TranslateAlteredCopy(AlteredCopy altCopy)
        {
            try
            {
                altCopy.Original.AcceptTranslator(this);

                DataItem original = returnedValue;
                if (original.Class.ClassID != ClassID.Object)
                    throw new RuntimeException(fileName, altCopy, Resources.InvalidOperandForWith);

                var copyFields = new Dictionary<string, DataItem>();

                foreach (var originalField in original.AsDynamicObject)
                    copyFields.Add(originalField.Key, originalField.Value);

                DataItem copy = new Object(original.Class, copyFields);
                ApplyPropertyInitializers(altCopy, copy, altCopy.PropertyInitializers);

                returnedValue = copy;
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, altCopy, ex);
            }
        }

        #endregion

        #region Frames Management

        /// <summary>
        /// Creates the root frame.
        /// </summary>
        private void CreateRootFrame()
        {
            var rootFrameContext = new InvocationContext(null, null, ROOT_FRAME_NAME);
            currentFrame = rootFrame = new MethodFrame(rootFrameContext);
            frames.Push(currentFrame);
        }

        /// <summary>
        /// Adds a frame on top of the stack.
        /// </summary>
        /// <param name="currentClass">The class from which the function is invoked</param>
        /// <param name="currentInstance">The caller of the corresponding function if it's a method</param>
        /// <param name="methodName">The name of the corresponding function</param>
        /// <param name="initialItems">Initial frame's items</param>
        private void PushFrame(Class currentClass, DataItem currentInstance, string methodName,
                               Dictionary<string, IFrameItem> initialItems)
        {
            var callContext = new InvocationContext(currentClass, currentInstance, methodName);
            currentFrame = new MethodFrame(callContext, initialItems);
            frames.Push(currentFrame);
        }

        /// <summary>
        /// Removes the current frame and brings the next one on top of the stack.
        /// </summary>
        private void PopFrame()
        {
            frames.Pop();
            currentFrame = frames.Peek();
        }

        /// <summary>
        /// Registers default items in the current frame.
        /// This should typically be invoked on the root frame
        /// to ensure maximum visibility
        /// </summary>
        /// <param name="moduleName">The value that has to be assigned to the <i>__name</i> constant</param>
        private void RegisterDefaults(string moduleName)
        {
            // Register constants first
            currentFrame.PutItem("MININT", new Constant(int.MinValue));
            currentFrame.PutItem("MAXINT", new Constant(int.MaxValue));
            currentFrame.PutItem("MINFLOAT", new Constant(double.MinValue));
            currentFrame.PutItem("MAXFLOAT", new Constant(double.MaxValue));
            currentFrame.PutItem("NAN", new Constant(double.NaN));
            currentFrame.PutItem("NINFINITY", new Constant(double.NegativeInfinity));
            currentFrame.PutItem("PINFINITY", new Constant(double.PositiveInfinity));
            currentFrame.PutItem("EPSILON", new Constant(double.Epsilon));
            currentFrame.PutItem("PI", new Constant(Math.PI));
            currentFrame.PutItem("E", new Constant(Math.E));
            currentFrame.PutItem("I", new Constant(Complex64.ImaginaryOne));
            currentFrame.PutItem("MINDATE", new Constant(DateTime.MinValue));
            currentFrame.PutItem("MAXDATE", new Constant(DateTime.MaxValue));
            currentFrame.PutItem("NEWLINE", new Constant(Environment.NewLine));
            currentFrame.PutItem(MODULE_NAME_CONSTANT, new Constant(moduleName));

            // Then create a callable wrapper for each global builtin function and register it
            foreach (InnerFunction innerFunc in InnerFunction.Globals)
                currentFrame.PutItem(innerFunc.Name, innerFunc.ToFunction());

            // Register predefined classes
            foreach (Class klass in Class.Predefined)
                currentFrame.PutItem(klass.Name, klass);

            // Register context variables
            foreach (KeyValuePair<string, object> pair in InitialContext.Bindings)
                currentFrame.PutItem(pair.Key, DataItemFactory.CreateDataItem(pair.Value));

            // Makes the context available to the script
            currentFrame.PutItem(CONTEXT_VARIABLE_NAME, new Resource(InitialContext));
        }

        /// <summary>
        /// Registers a variable in the stack.
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <param name="variable">The variable to update</param>
        private void RegisterVariable(string name, DataItem variable)
        {
            MethodFrame frame = currentFrame;
            IFrameItem frameItem = frame.GetItem(name);

            if (frameItem == null && frame != rootFrame)
            {
                frame = rootFrame;
                frameItem = frame.GetItem(name);
            }

            if (frameItem == null)
                currentFrame.PutItem(name, variable);
            else
                switch (frameItem.Kind)
                {
                    case FrameItemKind.Variable:
                        frame.PutItem(name, variable);
                        break;
                    case FrameItemKind.Constant:
                        throw new InvalidOperationException(Resources.CannotAlterConstant);
                    default:
                        throw new InvalidOperationException(string.Format(Resources.NameConflict, name));
                }
        }

        /// <summary>
        /// Registers a label in the current stack frame.
        /// </summary>
        /// <param name="name">The name of the label</param>
        /// <param name="label">The label to register</param>
        private void RegisterLabel(string name, Label label)
        {
            IFrameItem frameItem = currentFrame.GetItem(name);
            if (frameItem != null) throw new ScriptException(fileName, label, string.Format(Resources.NameConflict, name));
            
            currentFrame.PutItem(name, label);
        }

        /// <summary>
        /// Gets the initial frame items for a call.
        /// </summary>
        /// <param name="function">The function that is being called</param>
        /// <param name="functionName">The function's name</param>
        /// <param name="positionalArgs">The list of positional arguments passed to the function</param>
        /// <param name="namedArgs">The collection of named arguments passed to the function</param>
        /// <returns>A dictionary of frame items</returns>
        private Dictionary<string, IFrameItem> GetInitialFrameItems(Function function, string functionName,
                                                                    Expression[] positionalArgs,
                                                                    Dictionary<string, Expression> namedArgs)
        {
            // Make sure we are not dealing with null references
            positionalArgs ??= [];
            namedArgs ??= [];

            int totalArgCount = positionalArgs.Length;
            
            // Check that every named argument matches a parameter declared in the function's header
            // Also compute the total argument count
            foreach (string argName in namedArgs.Keys)
            {
                int paramIndex = Array.FindIndex(function.Parameters, p => p.Name == argName);

                if (paramIndex < 0)
                    throw new ArgumentException(string.Format(Resources.FunctionHasNoParameterNamed, functionName, argName));

                if (paramIndex < positionalArgs.Length)
                    throw new ArgumentException(string.Format(Resources.ParameterSuppliedTwice, argName));

                ++totalArgCount;
            }

            // Check the minimum number of arguments
            int minNumArgs = function.MinNumArgs;
            if (totalArgCount < minNumArgs)
                throw new InvalidProgramException(string.Format(Resources.TooFewArgs, functionName));

            // Check the maximum number of arguments
            int maxNumArgs = function.MaxNumArgs;
            if (totalArgCount > maxNumArgs)
                throw new InvalidProgramException(string.Format(Resources.TooManyArgs, functionName));

            var frameItems = new Dictionary<string, IFrameItem>();
            int counter = 0;
            Parameter parameter;

            // Pass the positional arguments
            while (counter < positionalArgs.Length)
            {
                parameter = function.Parameters[counter];

                if (parameter.VaArgs)
                {
                    // If the current parameter is a variably sized list,
                    // fill it with the remaining arguments
                    DataItem vaList = new List();

                    if (counter == positionalArgs.Length - 1)
                    {
                        positionalArgs[counter].AcceptTranslator(this);
                        DataItem argValue = returnedValue;

                        if (argValue.Class == Class.List)
                            vaList = argValue;
                        else
                            vaList.AsList.Add(argValue);
                    }
                    else
                    {
                        int k = counter;

                        do
                        {
                            positionalArgs[k++].AcceptTranslator(this);
                            vaList.AsList.Add(returnedValue);
                        } while (k < positionalArgs.Length);
                    }

                    frameItems.Add(parameter.Name, vaList);
                    counter = int.MaxValue;
                }
                else
                {
                    // Otherwise, set the value provided to the parameter
                    positionalArgs[counter].AcceptTranslator(this);
                    frameItems.Add(parameter.Name, returnedValue);
                    ++counter;
                }
            }

            // Finish with the named arguments and optional parameters default values
            while (counter < function.Parameters.Length)
            {
                parameter = function.Parameters[counter];

                if (namedArgs.TryGetValue(parameter.Name, out Expression arg))
                {
                    arg.AcceptTranslator(this);
                    frameItems.Add(parameter.Name, returnedValue);
                }
                else if (counter >= minNumArgs)
                    frameItems.Add(parameter.Name, parameter.VaArgs ? new List() : parameter.DefaultValue);
                else
                    throw new ArgumentException(string.Format(Resources.MissingPameter, parameter.Name, functionName));

                ++counter;
            }

            // For inline functions, import the declaring function's local constants and variables
            foreach (KeyValuePair<string, IFrameItem> pair in function.CapturedItems)
                if (!frameItems.ContainsKey(pair.Key))
                    frameItems.Add(pair.Key, pair.Value);

            return frameItems;
        }

        /// <summary>
        /// Copies back the modified value of each <i>byref</i> parameter upon function's completion.<br/>
        /// For inline functions, also updates the variables imported from the declaring function's context.
        /// </summary>
        /// <param name="function">The function for which values are copied back</param>
        /// <param name="arguments">The real arguments of the function</param>
        /// <param name="frameItems">The frame's items to copy back</param>
        private void CopyBackFrameItems(Function function, Expression[] arguments, Dictionary<string, IFrameItem> frameItems)
        {
            var namesToSkip = new HashSet<string>();

            for (int i = 0; i < function.Parameters.Length; ++i)
            {
                Parameter parameter = function.Parameters[i];
                if (parameter.ByRef) Assign(arguments[i], (DataItem)frameItems[parameter.Name]);
                namesToSkip.Add(parameter.Name);
            }

            if (function.DeclaringFrame != null)
            {
                function.UpdateCapturedItems(frameItems);
                function.DeclaringFrame.SyncItems(frameItems, namesToSkip);
            }
        }

        /// <summary>
        /// Invokes a function.
        /// </summary>
        /// <param name="function">The function to invoke</param>
        /// <param name="name">The function's name</param>
        /// <param name="holder">The class in which the function is declared (if it's a method)</param>
        /// <param name="target">The object from which the function is invoked (if it's a method)</param>
        /// <param name="positionalArgs">The list of positional arguments passed to the function</param>
        /// <param name="namedArgs">The collection of named arguments passed to the function</param>
        private void Invoke(Function function, string name, Class holder, DataItem target,
                            Expression[] positionalArgs, Dictionary<string, Expression> namedArgs)
        {
            var frameItems = GetInitialFrameItems(function, name, positionalArgs, namedArgs);
            PushFrame(holder, target, name, frameItems);

            try
            {
                function.Body.AcceptTranslator(this);

                if (jumpCode == JumpCode.Goto)
                    throw new RuntimeException(fileName, lastGoto, string.Format(Resources.MissingLabel, lastGoto.LabelName));
            }
            finally
            {
                PopFrame();

                DataItem result = returnedValue;
                CopyBackFrameItems(function, positionalArgs, frameItems);
                returnedValue = result;

                jumpCode = JumpCode.None;
            }
        }

        /// <summary>
        /// Invokes a function.
        /// </summary>
        /// <param name="function">The function to invoke</param>
        /// <param name="name">The function's name</param>
        /// <param name="holder">The class in which the function is declared (if it's a method)</param>
        /// <param name="target">The object from which the function is invoked (if it's a method)</param>
        /// <param name="arguments">The arguments passed to the function</param>
        private void Invoke(Function function, string name, Class holder, DataItem target, params Expression[] arguments)
        {
            Invoke(function, name, holder, target, arguments, null);
        }

        /// <summary>
        /// Updates the initial context's bindings.
        /// </summary>
        public void UpdateInitialContextBindings()
        {
            string[] names = [.. InitialContext.Bindings.Keys];

            foreach (string name in names)
            {
                var value = (DataItem)rootFrame.GetItem(name);
                InitialContext.Bindings[name] = value.AsNativeObject;
            }
        }

        #endregion

        #region OOP Support

        /// <summary>
        /// Gets if the given unary operator is overloadable or not.
        /// </summary>
        /// <param name="_operator">The given unary operator</param>
        /// <param name="postfix">Tells whether the operator is the postfix variant or not</param>
        /// <returns><b>false</b> for ! and the post-fixed variants of ++ and -- <b>true</b> for any other</returns>
        private static bool IsOverloadable(UnaryOperator _operator, out bool postfix)
        {
            switch (_operator)
            {
                case UnaryOperator.None:
                case UnaryOperator.Not:
                    return postfix = false;
                case UnaryOperator.NotEmpty:
                    postfix = true;
                    return false;
                case UnaryOperator.PostIncrement:
                case UnaryOperator.PostDecrement:
                    return postfix = true;
                default:
                    postfix = false;
                    return true;
            }
        }

        /// <summary>
        /// Gets if the given binary operator is overloadable or not.
        /// </summary>
        /// <param name="_operator">The given unary operator</param>
        /// <returns><b>false</b> for &&, ||, ===, !== and ??; <b>true</b> for any other</returns>
        private static bool IsOverloadable(BinaryOperator _operator)
        {
            return _operator switch
            {
                BinaryOperator.None or BinaryOperator.AndAlso or BinaryOperator.OrElse or
                BinaryOperator.Identical or BinaryOperator.NotIdentical or BinaryOperator.IfEmpty => false,
                _ => true,
            };
        }

        /// <summary>
        /// Initializes the static fields of a class.
        /// </summary>
        /// <param name="klass">A class</param>
        private void InitializeFields(Class klass)
        {
            foreach (ClassField field in klass.Fields)
                if ((field.Modifier == Modifier.Static ||
                    field.Modifier == Modifier.StaticFinal) &&
                    field.Initializer != null)
                {
                    field.Initializer.AcceptTranslator(this);
                    field.SharedValue = returnedValue;
                }
        }

        /// <summary>
        /// Initializes the fields of an instance.
        /// </summary>
        /// <param name="instance">A class instance</param>
        private void InitializeFields(DataItem instance)
        {
            Class klass = instance.Class;

            while (klass != null)
            {
                foreach (ClassField field in klass.Fields)
                    if (field.Modifier != Modifier.Static && field.Modifier != Modifier.StaticFinal)
                        if (field.Initializer == null)
                            instance.SetProperty(field.Name, Void.Value);
                        else
                        {
                            field.Initializer.AcceptTranslator(this);
                            instance.SetProperty(field.Name, returnedValue);
                        }

                klass = klass.SuperClass;
            }
        }

        /// <summary>
        /// Determines if a member is accessible in the current context or not.
        /// </summary>
        /// <param name="member">The member itself</param>
        /// <param name="astNode">The AST node that tries to access to a class member</param>
        private void CheckAccess(ClassMember member, AstNode astNode)
        {
            bool violation = false;
            InvocationContext ctx = currentFrame.Context;

            switch (member.Scope)
            {
                case Scope.Private:
                    violation = ctx.MethodHolder == null || ctx.MethodHolder != member.Holder;
                    break;
                case Scope.Protected:
                    violation = ctx.MethodHolder == null ||
                                (ctx.MethodHolder != member.Holder &&
                                !ctx.MethodHolder.Inherits(member.Holder));
                    break;
            }

            if (violation)
                throw new RuntimeException(fileName, astNode, 
                    string.Format(Resources.AccessDenied, member.FullName,
                    ctx.MethodHolder == null ? "public" : ctx.MethodHolder.Name));
        }

        /// <summary>
        /// Initializes the properties of an instance with the given set of <see cref="PropertyInitializer"/>s.
        /// </summary>
        /// <param name="astNode">The AST node that from witch the properties are getting initialized</param>
        /// <param name="target">The object that's being initialized</param>
        /// <param name="propertyInitializers">The given set of property initializers</param>
        private void ApplyPropertyInitializers(AstNode astNode, DataItem target, PropertyInitializer[] propertyInitializers)
        {
            DataItem savedValue = returnedValue;

            foreach (PropertyInitializer initializer in propertyInitializers)
            {
                ClassMember member = target.Class.GetMember(initializer.Name, MemberKind.Field | MemberKind.Property);
                if (member != null)
                {
                    CheckAccess(member, astNode);
                    if (member.Modifier == Modifier.StaticFinal)
                        throw new ScriptException(fileName, initializer, Resources.CannotWriteFinalField);
                }

                initializer.Expression.AcceptTranslator(this);
                DataItem propValue = returnedValue;

                if (member is ClassProperty property)
                {
                    if (!property.CanWrite) throw new ScriptException(fileName, initializer, Resources.CannotWriteProperty);

                    CheckAccess(property.Writer, initializer.Expression);
                    Invoke(property.Writer.Function, property.Name, property.Holder, target, new Literal(propValue));
                }
                else
                    target.SetProperty(initializer.Name, propValue);
            }

            returnedValue = savedValue;
        }

        /// <summary>
        /// Converts a ScriptException to an AddyScript exception.
        /// </summary>
        /// <param name="sx">The native exception to convert</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem ConvertException(ScriptException sx)
        {
            if (sx is RuntimeException rx && rx.Thrown != null) return rx.Thrown;
            
            var ex = new Object(Class.Exception);
            InitializeFields(ex);

            if (sx.InnerException != null)
                ex.SetProperty("_name", new String(sx.InnerException.GetType().Name));
            
            ex.SetProperty("_message", new String(sx.Message));
            ex.SetProperty("_source", new String(fileName));
            ex.SetProperty("_line", new Integer(sx.Element.Start.LineNumber));

            return ex;
        }

        /// <summary>
        /// Converts declared attributes to runtime attributes.
        /// </summary>
        /// <param name="attributes">The set of declared attributes</param>
        /// <returns>An array of <see cref="DataItem"/></returns>
        private DataItem[] ConvertAttributes(AttributeDecl[] attributes)
        {
            return attributes?.Select(ConvertAttribute).ToArray();
        }

        /// <summary>
        /// Converts a declared attribute to a runtime attribute.
        /// </summary>
        /// <param name="attribute">The declared attribute</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem ConvertAttribute(AttributeDecl attribute)
        {
            var ctorCall = new ConstructorCall(new QualifiedName(Class.Attribute.Name),
                                               [new Literal(new String(attribute.Name))],
                                               null,
                                               attribute.PropertyInitializers);
            ctorCall.CopyLocation(attribute);
            ctorCall.AcceptTranslator(this);

            return returnedValue;
        }

        /// <summary>
        /// Gets a TypeInfo from a class.
        /// </summary>
        /// <param name="klass">The target class</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetTypeInfo(Class klass)
        {
            if (!typeInfoCache.TryGetValue(klass, out DataItem result))
            {
                var typeInfo = new Object(Class.TypeInfo);
                DataItem superType = klass.SuperClass != null ? new String(klass.SuperClass.Name) : Void.Value;
                DataItem indexerInfo = klass.Indexer != null ? GetPropertyInfo(klass.Indexer) : Void.Value;

                InitializeFields(typeInfo);
                typeInfo.SetProperty("_superType", superType);
                typeInfo.SetProperty("_modifier", new String(klass.Modifier.ToString()));
                typeInfo.SetProperty("_name", new String(klass.Name));
                typeInfo.SetProperty("_constructor", GetMethodInfo(klass.Constructor));
                typeInfo.SetProperty("_indexer", indexerInfo);
                typeInfo.SetProperty("_fields", GetFieldInfoMap(klass));
                typeInfo.SetProperty("_properties", GetPropertyInfoMap(klass));
                typeInfo.SetProperty("_methods", GetMethodInfoMap(klass));
                typeInfo.SetProperty("_events", GetEventInfoMap(klass));
                typeInfo.SetProperty("_attributes", GetAttributeList(klass.Attributes));
                typeInfoCache.Add(klass, result = typeInfo);
            }
            
            return result;
        }

        /// <summary>
        /// Gets a MemberInfo from a ClassMember.
        /// </summary>
        /// <param name="member">The ClassMember</param>
        /// <param name="klass">The class for which to create an instance</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetMemberInfo(ClassMember member, Class klass)
        {
            var memberInfo = new Object(klass);

            InitializeFields(memberInfo);
            memberInfo.SetProperty("_scope", new String(member.Scope.ToString()));
            memberInfo.SetProperty("_modifier", new String(member.Modifier.ToString()));
            memberInfo.SetProperty("_name", new String(member.Name));
            memberInfo.SetProperty("_holder", new String(member.Holder.Name));
            memberInfo.SetProperty("_attributes", GetAttributeList(member.Attributes));

            return memberInfo;
        }

        /// <summary>
        /// Gets a FieldInfo from a ClassField.
        /// </summary>
        /// <param name="field">The ClassField</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetFieldInfo(ClassField field)
        {
            DataItem fieldInfo = GetMemberInfo(field, Class.FieldInfo);
            
            fieldInfo.SetProperty("_sharedValue", field.SharedValue ?? Void.Value);

            return fieldInfo;
        }

        /// <summary>
        /// Gets a map of FieldInfo from the fields of a class.
        /// </summary>
        /// <param name="cls">The target class</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetFieldInfoMap(Class cls)
        {
            var fieldInfoMap = new Map();

            foreach (ClassField field in cls.GetMembers(MemberKind.Field))
            {
                DataItem fieldName = new String(field.Name);
                fieldInfoMap.SetItem(fieldName, GetFieldInfo(field));
            }

            return fieldInfoMap;
        }

        /// <summary>
        /// Gets a PropertyInfo from a ClassProperty.
        /// </summary>
        /// <param name="property">The ClassProperty</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetPropertyInfo(ClassProperty property)
        {
            DataItem propertyInfo = GetMemberInfo(property, Class.PropertyInfo);

            propertyInfo.SetProperty("_reader", property.CanRead ? GetMethodInfo(property.Reader) : Void.Value);
            propertyInfo.SetProperty("_writer", property.CanWrite ? GetMethodInfo(property.Writer) : Void.Value);
            
            return propertyInfo;
        }

        /// <summary>
        /// Gets a map of PropertyInfo from the propertys of a class.
        /// </summary>
        /// <param name="cls">The target class</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetPropertyInfoMap(Class cls)
        {
            var propertyInfoMap = new Map();

            foreach (ClassProperty property in cls.GetMembers(MemberKind.Property))
            {
                DataItem propertyName = new String(property.Name);
                propertyInfoMap.SetItem(propertyName, GetPropertyInfo(property));
            }

            return propertyInfoMap;
        }

        /// <summary>
        /// Gets a MethodInfo from a ClassMethod.
        /// </summary>
        /// <param name="method">The ClassMethod</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetMethodInfo(ClassMethod method)
        {
            DataItem methodInfo = GetMemberInfo(method, Class.MethodInfo);
         
            methodInfo.SetProperty("_parameters", GetParameterInfoMap(method.Function.Parameters));

            return methodInfo;
        }

        /// <summary>
        /// Gets a map of MemberInfo from the methods of a class.
        /// </summary>
        /// <param name="cls">The target class</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetMethodInfoMap(Class cls)
        {
            var methodInfoMap = new Map();

            foreach (ClassMethod method in cls.GetMembers(MemberKind.Method))
            {
                DataItem methodName = new String(method.Name);
                methodInfoMap.SetItem(methodName, GetMethodInfo(method));
            }

            return methodInfoMap;
        }

        /// <summary>
        /// Gets an EventInfo from a ClassEvent.
        /// </summary>
        /// <param name="_event">The ClassEvent</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetEventInfo(ClassEvent _event)
        {
            DataItem _eventInfo = GetMemberInfo(_event, Class.EventInfo);
            _eventInfo.SetProperty("_parameters", GetParameterInfoMap(_event.Parameters));
            return _eventInfo;
        }

        /// <summary>
        /// Gets a map of MemberInfo from the events of a class.
        /// </summary>
        /// <param name="cls">The target class</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetEventInfoMap(Class cls)
        {
            var eventInfoMap = new Map();

            foreach (ClassEvent _event in cls.GetMembers(MemberKind.Event))
            {
                DataItem eventName = new String(_event.Name);
                eventInfoMap.SetItem(eventName, GetEventInfo(_event));
            }

            return eventInfoMap;
        }

        /// <summary>
        /// Gets a ParameterInfo from a Parameter.
        /// </summary>
        /// <param name="parameter">The Parameter</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetParameterInfo(Parameter parameter)
        {
            var parameterInfo = new Object(Class.ParameterInfo);

            InitializeFields(parameterInfo);
            parameterInfo.SetProperty("_name", new String(parameter.Name));
            parameterInfo.SetProperty("_byRef", Boolean.FromBool(parameter.ByRef));
            parameterInfo.SetProperty("_vaArgs", Boolean.FromBool(parameter.VaArgs));
            parameterInfo.SetProperty("_defaultValue", parameter.DefaultValue ?? Void.Value);
            parameterInfo.SetProperty("_attributes", GetAttributeList(parameter.Attributes));

            return parameterInfo;
        }

        /// <summary>
        /// Gets a map of ParameterInfo from a parameters set.
        /// </summary>
        /// <param name="parameters">The given parameters set</param>
        /// <returns>A <see cref="DataItem"/></returns>
        private DataItem GetParameterInfoMap(Parameter[] parameters)
        {
            var parameterInfoMap = new Map();

            foreach (Parameter parameter in parameters)
            {
                DataItem paramName = new String(parameter.Name);
                parameterInfoMap.SetItem(paramName, GetParameterInfo(parameter));
            }

            return parameterInfoMap;
        }

        /// <summary>
        /// Gets a list of runtime attributes from a set of data items.
        /// </summary>
        /// <param name="attributes">The given set of data items</param>
        /// <returns>A <see cref="List"/></returns>
        private DataItem GetAttributeList(DataItem[] attributes)
        {
            return attributes != null ? new List(attributes) : new List();
        }

        #endregion

        #region .Net Interop Management

        /// <summary>
        /// Changes the extension of a library's name so that it matches the host platform standards.
        /// </summary>
        /// <param name="libraryName">A library's name</param>
        /// <returns>A <see cref="string"/></returns>
        private static string WithNativeLibraryExtension(string libraryName)
        {
            libraryName = Path.GetFileNameWithoutExtension(libraryName);
            if (OperatingSystem.IsWindows()) return libraryName + ".dll";
            if (OperatingSystem.IsIOS()) return libraryName + ".dynlib";
            return libraryName + ".so"; // Unix/Linux
        }

        /// <summary>
        /// Generates a P/Invoke method for the specified native DLL function.
        /// </summary>
        /// <param name="libraryName">The name of the native DLL</param>
        /// <param name="procedureName">The procedure's name</param>
        /// <param name="returnType">The type of the returned value</param>
        /// <param name="parameterTypes">The types of the parameters</param>
        /// <returns>A <see cref="MethodInfo"/></returns>
        private static MethodInfo GetPInvokeMethod(string libraryName, string procedureName, Type returnType, Type[] parameterTypes)
        {
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(libraryName) + "_" + procedureName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);

            moduleBuilder.DefinePInvokeMethod(procedureName, libraryName, MethodAttributes.Public | MethodAttributes.Static,
                                              CallingConventions.Standard, returnType, parameterTypes, CallingConvention.Winapi,
                                              CharSet.Auto);
            moduleBuilder.CreateGlobalFunctions();

            return moduleBuilder.GetMethod(procedureName);
        }

        /// <summary>
        /// Invokes a dotnet type's method from the script.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> that holds the method</param>
        /// <param name="methodName">The method's name</param>
        /// <param name="target">The dotnet <see cref="object"/> on which the method should be invoked</param>
        /// <param name="arguments">The set of arguments that will be passed to the method</param>
        /// <exception cref="MissingMethodException">
        /// When there is no method with the name <paramref name="methodName"/> in type indicated by <paramref name="type"/>
        /// </exception>
        private void InvokeNative(Type type, string methodName, object target, Expression[] arguments)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                       BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

            arguments ??= [];

            var argValues = new DataItem[arguments.Length];
            var nativeArgValues = new object[argValues.Length];
            object result;

            for (int i = 0; i < arguments.Length; ++i)
            {
                arguments[i].AcceptTranslator(this);
                argValues[i] = returnedValue;
            }
            
            if (type.IsCOMObject)
            {
                for (int i = 0; i < argValues.Length; ++i)
                    nativeArgValues[i] = argValues[i].AsNativeObject;

                result = type.InvokeMember(methodName, flags, null, target, nativeArgValues);
            }
            else
            {
                MethodInfo matchedMethod = DataItemBinder.FindMethod(type, methodName, argValues, flags) ??
                    throw new MissingMethodException(type.FullName, methodName);

                ParameterInfo[] parameters = matchedMethod.GetParameters();
                for (int i = 0; i < argValues.Length; ++i)
                    nativeArgValues[i] = argValues[i].ConvertTo(parameters[i].ParameterType);

                result = matchedMethod.Invoke(target, nativeArgValues);

                for (int i = 0; i < arguments.Length; ++i)
                    if (parameters[i].IsOut || parameters[i].ParameterType.IsArray)
                        try
                        {
                            Assign(arguments[i], DataItemFactory.CreateDataItem(nativeArgValues[i]));
                        }
                        catch (RuntimeException)
                        {
                        }
            }

            returnedValue = DataItemFactory.CreateDataItem(result);
        }

        /// <summary>
        /// Gets the .Net type that matches a particular name
        /// </summary>
        /// <param name="typeName">The given type name</param>
        /// <returns>A <see cref="Type"/></returns>
        private Type GetTypeByName(string typeName)
        {
            foreach (Assembly assembly in InitialContext.References)
            {
                Type type = assembly.GetType(typeName);
                if (type == null) continue;
                return type;
            }

            return ScriptContext.CoreLib.GetType("System." + typeName);
        }

        #endregion

        #region Miscellaneous Utility

        /// <summary>
        /// Evaluates an expression and gets if the returned value is true.
        /// </summary>
        /// <param name="condition">The expression to evaluate</param>
        /// <returns>A boolean value</returns>
        private bool IsTrue(Expression condition)
        {
            try
            {
                condition.AcceptTranslator(this);
                return returnedValue.AsBoolean;
            }
            catch (InvalidCastException ex)
            {
                throw new RuntimeException(fileName, condition, ex);
            }
        }

        /// <summary>
        /// Evaluates an expression and iterates on the returned value,
        /// returning a couple of variables at each step.
        /// </summary>
        /// <param name="expr">The expression to iterate on</param>
        /// <returns>An <see cref="IEnumerable{T}"/></returns>
        private IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable(Expression expr)
        {
            try
            {
                expr.AcceptTranslator(this);
                DataItem enumerated = returnedValue;

                return enumerated.Class.Inherits(Class.Object)
                     ? GetProgrammaticEnumerable(enumerated, expr)
                     : enumerated.GetEnumerable();
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuntimeException(fileName, expr, ex);
            }
        }

        /// <summary>
        /// Gets a programmatic enumerable from a class implementing the iterator protocol.
        /// </summary>
        /// <param name="value">The object to iterate on</param>
        /// <param name="expr">The expression from which <paramref name="value"/> is obtained</param>
        /// <returns>An <see cref="IEnumerable{T}"/></returns>
        private IEnumerable<KeyValuePair<DataItem, DataItem>> GetProgrammaticEnumerable(DataItem value, Expression expr)
        {
            ClassMethod moveFirstMethod = value.Class.GetMethod("moveFirst") ??
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, value.Class.Name));
            CheckAccess(moveFirstMethod, expr);

            ClassMethod hasNextMethod = value.Class.GetMethod("hasNext") ??
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, value.Class.Name));
            CheckAccess(hasNextMethod, expr);

            ClassMethod moveNextMethod = value.Class.GetMethod("moveNext") ??
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, value.Class.Name));
            CheckAccess(moveNextMethod, expr);

            Invoke(moveFirstMethod.Function, moveFirstMethod.Name, moveFirstMethod.Holder, value);
            Invoke(hasNextMethod.Function, hasNextMethod.Name, hasNextMethod.Holder, value);

            int counter = 0;

            while (returnedValue.AsBoolean)
            {
                Invoke(moveNextMethod.Function, moveNextMethod.Name, moveNextMethod.Holder, value);
                yield return new KeyValuePair<DataItem, DataItem>(new Integer(counter++), returnedValue);

                Invoke(hasNextMethod.Function, hasNextMethod.Name, hasNextMethod.Holder, value);
            }
        }

        /// <summary>
        /// Imports another script from whithin the calling one.
        /// </summary>
        /// <param name="scriptName">The name of the script to be imported</param>
        /// <returns><b>true</b> if a script has been imported;<b>false</b> otherwise</returns>
        private bool ImportScript(QualifiedName scriptName)
        {
            var importPaths = new List<string>();

            if (!string.IsNullOrEmpty(fileName)) importPaths.Add(Path.GetDirectoryName(fileName));
            importPaths.Add(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            importPaths.AddRange(InitialContext.ImportPaths);

            string path = null;

            foreach (string directory in importPaths)
            {
                path = Path.Combine(directory, scriptName.ToFilePath() + ".add");
                if (File.Exists(path)) break;
                path = null;
            }

            if (path == null) return false;

            if (!importedModules.Contains(path))
            {
                Goto savedGoto = lastGoto;
                lastGoto = null;

                JumpCode savedJumpCode = jumpCode;
                jumpCode = JumpCode.None;
                
                MissingReferenceAction savedMisRefAct = misRefAct;
                misRefAct = MissingReferenceAction.Fail;
                
                MethodFrame savedRootFrame = rootFrame;
                Stack<MethodFrame> savedFrames = frames;
                frames = new Stack<MethodFrame>();
                CreateRootFrame();
                RegisterDefaults(path);

                using (var reader = new StreamReader(path))
                {
                    Program program = new Parser(new Lexer(reader)).Program();
                    program.AcceptTranslator(this);
                }

                frames = savedFrames;
                // Note: Items may be copied into a module in the future
                savedRootFrame.RootBlock.CopyItemsFrom(rootFrame.RootBlock);
                rootFrame = savedRootFrame;
                currentFrame = frames.Peek();
                misRefAct = savedMisRefAct;
                jumpCode = savedJumpCode;
                lastGoto = savedGoto;
                importedModules.Add(path);
            }

            return true;
        }

        /// <summary>
        /// Imports a native .Net namespace or type.
        /// </summary>
        /// <param name="_namespace">The namespace to be imported</param>
        /// <param name="alias">An eventually given alias</param>
        /// <returns><b>true</b> if a namespace or type has been imported;<b>false</b> otherwise</returns>
        private bool ImportNamespace(QualifiedName _namespace, string alias)
        {
            bool aliasDefined = !string.IsNullOrEmpty(alias);
            string dottedName = _namespace.ToDottedName(false);
            string prefix = dottedName + ".";
            var types = new List<Type>();

            foreach (Assembly assembly in InitialContext.References)
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (type.FullName == dottedName)
                    {
                        CacheType(type, _namespace, aliasDefined ? alias : type.Name);
                        return true;
                    }

                    if (type.FullName.StartsWith(prefix))
                        types.Add(type);
                }

            if (types.Count <= 0) return false;

            if (aliasDefined)
                foreach (Type type in types)
                    CacheType(type, _namespace, alias);
            else
                foreach (Type type in types)
                    CacheType(type, _namespace);

            return true;
        }

        /// <summary>
        /// Resolves a qualified name.
        /// </summary>
        /// <param name="name">A <see cref="QualifiedName"/></param>
        /// <param name="statement">The statement for which to resolve the name</param>
        /// <returns>The object that has the given name</returns>
        private object ResolveName(QualifiedName name, Statement statement)
        {
            if (nameCache.Contains(name)) return nameCache[name];

            IFrameItem frameItem = currentFrame.GetItem(name[0].ToString());
            if (frameItem == null && currentFrame != rootFrame)
                frameItem = rootFrame.GetItem(name[0].ToString());

            if (frameItem is Class klass)
            {
                switch (name.Length)
                {
                    case 1:
                        return klass;
                    case 2:
                        ClassMember member = klass.GetMember(name[1].ToString());
                        if (member == null) return null;

                        CheckAccess(member, statement);
                        if (member.Modifier != Modifier.Static && member.Modifier != Modifier.StaticFinal)
                            throw new RuntimeException(fileName, statement,
                                string.Format(Resources.NonStaticMember, member.FullName));

                        return member;
                    default:
                        return null;
                }
            }

            foreach (Assembly assembly in InitialContext.References)
                for (int k = name.Length; k > 0; --k)
                {
                    Type type = assembly.GetType(name.Subname(0, k).ToDottedName(true));
                    if (type == null) continue;
                    CacheType(type);

                    return nameCache[name];
                }

            return OperatingSystem.IsWindows() ? Type.GetTypeFromProgID(name.ToDottedName(true)) : null;
        }

        /// <summary>
        /// Registers a type in the cache.
        /// </summary>
        /// <param name="type">The type to register in the cache</param>
        /// <param name="_namespace">A prefix to remove from the full type's name</param>
        /// <param name="alias">An alias that will be used to replace the original prefix</param>
        private void CacheType(Type type, QualifiedName _namespace = null, string alias = null)
        {
            QualifiedName originalName = QualifiedName.ParseDottedName(type.FullName);
            QualifiedName newName = originalName;

            if (_namespace != null)
            {
                newName = newName.Subname(_namespace.Length);
                if (!string.IsNullOrEmpty(alias))
                    newName = newName.Prepend(new NamePart(alias));
            }

            if (nameCache.Contains(newName)) return;

            if (type.IsGenericType && !type.IsConstructedGenericType)
                try
                {
                    type = type.MakeGenericType(type.GetGenericArguments().Select(t => typeof(DataItem)).ToArray());
                }
                catch
                {
                    return;
                }

            nameCache.Add(newName, type);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            foreach (MemberInfo member in type.GetMembers(flags))
            {
                QualifiedName memberName = newName.Apppend(new NamePart(member.Name));
                if (!nameCache.Contains(memberName))
                    nameCache.Add(memberName, new StaticTypeMember(type, member.Name));
            }

            foreach (Type nestedType in type.GetNestedTypes())
                CacheType(nestedType, _namespace, alias);
        }

        /// <summary>
        /// Evaluates the <i>Owner</i> and <i>Index</i> members of an ItemRef expression and
        /// returns the corresponding variables. May create the owner if requested.
        /// </summary>
        /// <param name="itemRef">An <see cref="ItemRef"/></param>
        /// <param name="owner">Will contain the owner upon completion</param>
        /// <param name="index">Will contain the index upon completion</param>
        /// <param name="indexer">Will contain an indexer definition upon completion</param>
        private void ResolveItemRef(ItemRef itemRef, out DataItem owner, out DataItem index, out ClassProperty indexer)
        {
            itemRef.Index.AcceptTranslator(this);
            index = returnedValue;

            itemRef.Owner.AcceptTranslator(this);
            owner = returnedValue;

            indexer = null;
            if (owner != null)
            {
                indexer = (ClassProperty)owner.Class.GetMember(ClassProperty.INDEXER_NAME, MemberKind.Indexer);
                if (indexer != null) CheckAccess(indexer, itemRef);
            }
        }

        /// <summary>
        /// Evaluates the <i>Owner</i> member of a PropertyRef expression and
        /// returns the corresponding variable. May create the owner if requested.
        /// </summary>
        /// <param name="propertyRef">A <see cref="PropertyRef"/></param>
        /// <param name="owner">Will contain the owner upon completion</param>
        /// <param name="member">Will contain a member's definition upon completion</param>
        private void ResolvePropertyRef(PropertyRef propertyRef, out DataItem owner, out ClassMember member)
        {
            propertyRef.Owner.AcceptTranslator(this);
            owner = returnedValue;

            member = null;
            if (owner != null)
            {
                member = owner.Class.GetMember(propertyRef.PropertyName, MemberKind.Field | MemberKind.Property | MemberKind.Method);
                if (member != null) CheckAccess(member, propertyRef);
            }
        }

        /// <summary>
        /// Assigns a value to the variable represented by the given expression.
        /// </summary>
        /// <param name="lValue">The expression on the left side of the assignment operator</param>
        /// <param name="rValue">The value of the expression on the right side of the assignment operator</param>
        private void Assign(Expression lValue, DataItem rValue)
        {
            if (lValue is VariableRef varRef)
                RegisterVariable(varRef.Name, rValue);
            else if (lValue is ItemRef itemRef)
            {
                ResolveItemRef(itemRef, out DataItem owner, out DataItem index, out ClassProperty indexer);

                if (indexer == null)
                    owner.SetItem(index, rValue);
                else
                {
                    if (!indexer.CanWrite)
                        throw new RuntimeException(fileName, lValue, Resources.CannotWriteProperty);

                    CheckAccess(indexer.Writer, lValue);
                    Invoke(indexer.Writer.Function, indexer.Name, indexer.Holder, owner, new Literal(index), new Literal(rValue));
                }
            }
            else if (lValue is PropertyRef propertyRef)
            {
                ResolvePropertyRef(propertyRef, out DataItem owner, out ClassMember member);

                if (member == null)
                    owner.SetProperty(propertyRef.PropertyName, rValue);
                else if (member is ClassField field)
                    switch (member.Modifier)
                    {
                        case Modifier.Default:
                            owner.SetProperty(member.Name, rValue);
                            break;
                        case Modifier.Static:
                            field.SharedValue = rValue;
                            break;
                        case Modifier.Final:
                            if (currentFrame.Context.MethodIsConstructor())
                                owner.SetProperty(member.Name, rValue);
                            else
                                throw new RuntimeException(fileName, propertyRef, Resources.CannotWriteFinalField);
                            break;
                        default: // StaticFinal
                            throw new RuntimeException(fileName, propertyRef, Resources.CannotWriteFinalField);
                    }
                else if (member is ClassProperty property)
                {
                    if (!property.CanWrite)
                        throw new RuntimeException(fileName, propertyRef, Resources.CannotWriteProperty);

                    CheckAccess(property.Writer, propertyRef);
                    Invoke(property.Writer.Function, property.Name, property.Holder, owner, new Literal(rValue));
                }
                else
                    throw new RuntimeException(fileName, lValue, Resources.InvalidLValue);
            }
            else if (lValue is StaticPropertyRef staticRef)
            {
                object targetProperty = ResolveName(staticRef.Name, lValue);

                if (targetProperty is ClassField field)
                {
                    if (field.Modifier == Modifier.Static)
                        field.SharedValue = rValue;
                    else // Static + Final
                        throw new RuntimeException(fileName, staticRef, Resources.CannotWriteFinalField);
                }
                else if (targetProperty is ClassProperty property)
                {
                    if (!property.CanWrite)
                        throw new RuntimeException(fileName, staticRef, Resources.CannotWriteProperty);

                    CheckAccess(property.Writer, staticRef);
                    Invoke(property.Writer.Function, property.Name, property.Holder, null, new Literal(rValue));
                }
                else if (targetProperty is StaticTypeMember member)
                    member.SetValue(rValue);
                else
                    throw new RuntimeException(fileName, staticRef, string.Format(Resources.UnresolvedMemberRef, staticRef.Name));
            }
            else if (lValue is ParentPropertyRef ppr)
            {
                Class superClass = currentFrame.Context.MethodHolder.SuperClass;
                ClassProperty property = superClass.GetProperty(ppr.PropertyName) ??
                    throw new RuntimeException(fileName, ppr,
                        string.Format(Resources.PropertyNotFoundInClass, ppr.PropertyName, superClass.Name));

                if (property.Modifier == Modifier.Abstract)
                    throw new RuntimeException(fileName, ppr, string.Format(Resources.CannotInvokeAbstractMember, property.FullName));

                if (!property.CanWrite)
                    throw new RuntimeException(fileName, ppr, Resources.CannotWriteProperty);

                CheckAccess(property, ppr);
                Invoke(property.Writer.Function, property.Name, property.Holder, currentFrame.Context.MethodTarget, new Literal(rValue));
            }
            else if (lValue is ParentIndexerRef pir)
            {
                Class superClass = currentFrame.Context.MethodHolder.SuperClass;
                ClassProperty indexer = superClass.Indexer ??
                    throw new RuntimeException(fileName, pir, string.Format(Resources.ClassHasNoIndexer, superClass.Name));

                if (indexer.Modifier == Modifier.Abstract)
                    throw new RuntimeException(fileName, pir, string.Format(Resources.CannotInvokeAbstractMember, indexer.FullName));

                if (!indexer.CanWrite)
                    throw new RuntimeException(fileName, pir, Resources.CannotWriteProperty);

                CheckAccess(indexer, pir);
                Invoke(indexer.Writer.Function, indexer.Name, indexer.Holder, currentFrame.Context.MethodTarget, pir.Index, new Literal(rValue));
            }
            else
                throw new RuntimeException(fileName, lValue, Resources.InvalidLValue);

            returnedValue = rValue;
        }

        #endregion
    }
}