#region 'using' Directives

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
using AddyScript.Compilers.Utility;
using AddyScript.Runtime;
using AddyScript.Runtime.Dynamics;
using AddyScript.Runtime.Frames;
using AddyScript.Runtime.Utilities;
using AddyScript.Parsers;
using AddyScript.Properties;
using Attribute = AddyScript.Runtime.Attribute;
using Boolean = AddyScript.Runtime.Dynamics.Boolean;
using Complex = AddyScript.Runtime.Dynamics.Complex;
using Label = AddyScript.Ast.Statements.Label;
using Object = AddyScript.Runtime.Dynamics.Object;
using String = AddyScript.Runtime.Dynamics.String;
using Void = AddyScript.Runtime.Dynamics.Void;

#endregion

namespace AddyScript.Compilers
{
    public class Interpreter : ICompiler
    {
        #region Constants

        internal const string MODULE_NAME_CONSTANT = "__name";
        internal const string MAIN_MODULE_NAME = "main";
        internal const string ROOT_FRAME_NAME = "root";
        internal const string CONTEXT_VARIABLE_NAME = "__context";

        #endregion

        #region Fields

        private readonly HashSet<string> importedModules = new HashSet<string>();
        private readonly NameTree nameCache = new NameTree();
        private readonly Dictionary<Class, Dynamic> typeInfoCache = new Dictionary<Class, Dynamic>();
        private Stack<MethodFrame> frames = new Stack<MethodFrame>();
        private MethodFrame rootFrame, currentFrame;
        private string fileName = string.Empty;
        private MissingReferenceAction misRefAct = MissingReferenceAction.Fail;
        private JumpCode jumpCode = JumpCode.None;
        private Dynamic returnedValue;
        private Goto lastGoto;
        
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an instance of <see cref="Interpreter"/>.
        /// </summary>
        /// <param name="context">The initial context of this instance</param>
        public Interpreter(ScriptContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            InitialContext = context;
            CreateRootFrame();
            RegisterDefaults(MAIN_MODULE_NAME);
        }

        /// <summary>
        /// Initializes an instance of <see cref="Interpreter"/>.
        /// </summary>
        public Interpreter()
            : this(new ScriptContext())
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
        public Dynamic ReturnedValue
        {
            get { return returnedValue; }
        }

        #endregion

        #region Members of ICompiler

        public void CompileProgram(Program program)
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
                    program.Statements[counter].AcceptCompiler(this);
                    switch (jumpCode)
                    {
                        case JumpCode.None:
                            ++counter;
                            break;
                        case JumpCode.Goto:
                            if (program.Labels.ContainsKey(lastGoto.LabelName))
                            {
                                counter = program.Labels[lastGoto.LabelName].Address;
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

        public void CompileImportDirective(ImportDirective import)
        {
            try
            {
                if (string.IsNullOrEmpty(import.Alias))
                {
                    if (!(ImportScript(import.ModuleName) ||
                          ImportNamespace(import.ModuleName, null)))
                        throw new RuntimeException(fileName, import,
                            string.Format(Resources.ModuleNotFound, import.ModuleName));
                }
                else if (!ImportNamespace(import.ModuleName, import.Alias))
                    throw new RuntimeException(fileName, import,
                        string.Format(Resources.UndefinedType, import.ModuleName));
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

        public void CompileClassDefinition(ClassDefinition classDef)
        {
            if (rootFrame.GetRootItem(classDef.ClassName) != null)
                throw new RuntimeException(fileName, classDef,
                    string.Format(Resources.NameConflict, classDef.ClassName));

            Class superClass = Class.Object;
            if (!string.IsNullOrEmpty(classDef.SuperClassName))
            {
                superClass = rootFrame.GetItem(classDef.SuperClassName) as Class;
                if (superClass == null)
                    throw new RuntimeException(fileName, classDef,
                        string.Format(Resources.UndefinedType, classDef.SuperClassName));
            }

            switch (superClass.Modifier)
            {
                case Modifier.Final:
                case Modifier.Static:
                    throw new RuntimeException(fileName, classDef,
                        string.Format(Resources.CannotCreateSubclass, classDef.SuperClassName));
                case Modifier.Abstract:
                    if (classDef.Modifier != Modifier.Abstract)
                    {
                        const MemberKind kind = MemberKind.Property | MemberKind.Method;
                        
                        foreach (ClassMember member in superClass.GetMembers(kind))
                        {
                            if (member.Modifier != Modifier.Abstract) continue;

                            ClassMember _override = null;
                            foreach (ClassMember m in classDef.GetMembers(kind))
                                if (m.Name == member.Name)
                                {
                                    _override = m;
                                    break;
                                }

                            if (_override == null)
                                throw new RuntimeException(fileName, classDef,
                                    string.Format(Resources.MustOverride, classDef.ClassName, member.FullName));

                            if (_override.Modifier == Modifier.Abstract || _override.Modifier == Modifier.Static)
                                throw new ScriptException(fileName, _override, 
                                    string.Format(Resources.InvalidMemberModifier, _override.Name));
                        }
                    }
                    break;
            }

            foreach (ClassField field in classDef.Fields)
            {
                ClassField superField = superClass.GetField(field.Name);
                if (superField != null)
                    throw new ScriptException(fileName, field,
                        string.Format(Resources.FieldDeclaredInAncestor, field.Name, superField.Definer.Name));

                if (superClass.GetMember(field.Name, MemberKind.All & ~MemberKind.Field) != null)
                    throw new ScriptException(fileName, field, string.Format(Resources.FieldHidesHomonymous, field.Name));
            }

            foreach (ClassProperty property in classDef.Properties)
            {
                if (superClass.GetMember(property.Name, MemberKind.All & ~MemberKind.Property) != null)
                    throw new ScriptException(fileName, property,
                        string.Format(Resources.PropertyHidesHomonymous, property.Name));

                ClassProperty overriden = superClass.GetProperty(property.Name);
                if (overriden != null)
                {
                    if (overriden.Modifier == Modifier.Final || overriden.Modifier == Modifier.Static)
                        throw new ScriptException(fileName, property, string.Format(Resources.MemberCantOverride, overriden.FullName));

                    if (!property.MatchesSignature(overriden))
                        throw new ScriptException(fileName, property, string.Format(Resources.MustMatchSignature, property.Name, overriden.FullName));
                }
            }

            foreach (ClassMethod method in classDef.Methods)
            {
                if (superClass.GetMember(method.Name, MemberKind.All & ~MemberKind.Method) != null)
                    throw new ScriptException(fileName, method,
                        string.Format(Resources.MethodHidesHomonymous, method.Name));

                ClassMethod overriden = superClass.GetMethod(method.Name);
                if (overriden != null)
                {
                    if (overriden.Modifier == Modifier.Final || overriden.Modifier == Modifier.Static)
                        throw new ScriptException(fileName, method, string.Format(Resources.MemberCantOverride, overriden.FullName));

                    //Note: I'm not sure it's wise to check this!
                    if (!method.MatchesSignature(overriden))
                        throw new ScriptException(fileName, method, string.Format(Resources.MustMatchSignature, method.Name, overriden.FullName));
                }
            }

            foreach (ClassEvent _event in classDef.Events)
            {
                ClassEvent superEvent = superClass.GetEvent(_event.Name);
                if (superEvent != null)
                    throw new ScriptException(fileName, _event,
                        string.Format(Resources.EventDeclaredInAncestor, _event.Name, superEvent.Definer.Name));

                if (superClass.GetMember(_event.Name, MemberKind.All & ~MemberKind.Event) != null)
                    throw new ScriptException(fileName, _event, string.Format(Resources.EventHidesHomonymous, _event.Name));
            }

            var klass = new Class(superClass, classDef.ClassName, classDef.Modifier,
                                  classDef.Constructor, classDef.Fields,
                                  classDef.Properties, classDef.Methods,
                                  classDef.Events)
            {
                Attributes = classDef.Attributes
            };

            rootFrame.PutRootItem(classDef.ClassName, klass);
            InitializeFields(klass);
        }

        public void CompileFunctionDecl(FunctionDecl fnDecl)
        {
            if (rootFrame.GetRootItem(fnDecl.Name) != null)
                throw new RuntimeException(fileName, fnDecl, string.Format(Resources.NameConflict, fnDecl.Name));

            fnDecl.Function.Attributes = fnDecl.Attributes;
            rootFrame.PutRootItem(fnDecl.Name, fnDecl.Function);
        }

        public void CompileExternalFunctionDecl(ExternalFunctionDecl extDecl)
        {
            if (rootFrame.GetRootItem(extDecl.Name) != null)
                throw new RuntimeException(fileName, extDecl, string.Format(Resources.NameConflict, extDecl.Name));

            Attribute procAttribute = extDecl.GetAttribute("Procedure");
            if (procAttribute == null)
                throw new RuntimeException(fileName, extDecl,
                    string.Format(Resources.MissingAttribute, "Procedure", extDecl.Name));

            AttributeProperty libProperty = procAttribute.GetProperty("library");
            if (libProperty == null)
                throw new ScriptException(fileName, procAttribute,
                    string.Format(Resources.MissingAttributeProperty, "library", "Procedure"));
            string libName = libProperty.Value.ToString();
            if (!libName.EndsWith(".dll")) libName += ".dll";

            string procName = extDecl.Name;
            AttributeProperty nameProperty = procAttribute.GetProperty("name");
            if (nameProperty != null) procName = nameProperty.Value.ToString();

            Type defaultType = typeof(object), returnType = typeof(void);
            
            AttributeProperty returnTypeProperty = procAttribute.GetProperty("returnType");
            if (returnTypeProperty != null)
            {
                returnType = GetTypeByName(returnTypeProperty.Value.ToString());
                if (returnType == null)
                    throw new ScriptException(fileName, returnTypeProperty,
                        string.Format(Resources.InvalidTypeReference, returnTypeProperty.Value));
            }

            var paramTypes = new Type[extDecl.Parameters.Length];
            var args = new Expression[extDecl.Parameters.Length];

            for (int i = 0; i < extDecl.Parameters.Length; ++i)
            {
                Parameter parameter = extDecl.Parameters[i];
                Attribute paramAttribute = parameter.GetAttribute("Parameter");

                if (paramAttribute == null)
                    paramTypes[i] = defaultType;
                else
                {
                    AttributeProperty typeProperty = paramAttribute.GetProperty("type");
                    if (typeProperty == null)
                        throw new ScriptException(fileName, paramAttribute,
                            string.Format(Resources.MissingAttributeProperty, "type", "Parameter"));

                    Type paramType = GetTypeByName(typeProperty.Value.ToString());
                    if (paramType == null)
                        throw new ScriptException(fileName, typeProperty,
                            string.Format(Resources.InvalidTypeReference, typeProperty.Value));

                    paramTypes[i] = paramType;
                }

                args[i] = new VariableRef(parameter.Name);
            }

            MethodInfo method = GetPInvokeMethod(libName, procName, returnType, paramTypes);
            var efc = new ExternalFunctionCall(method, args);
            var function = new Function(extDecl.Parameters, new Block(new Return(efc))) { Attributes = extDecl.Attributes };
            rootFrame.PutRootItem(extDecl.Name, function);
        }

        public void CompileConstantDecl(ConstantDecl cstDecl)
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
                        initializer.Expression.AcceptCompiler(this);
                        currentFrame.PutItem(initializer.Name, new Constant(returnedValue));
                    }
                    else if (frame != currentFrame)
                        switch (frameItem.Kind)
                        {
                            case FrameItemKind.Constant:
                            case FrameItemKind.Variable:
                                initializer.Expression.AcceptCompiler(this);
                                currentFrame.PutItem(initializer.Name, new Constant(returnedValue));
                                break;
                            default:
                                throw new ScriptException(fileName, initializer,
                                    string.Format(Resources.NameConflict, initializer.Name));
                        }
                    else
                        throw new ScriptException(fileName, initializer,
                            string.Format(Resources.NameConflict, initializer.Name));
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

        public void CompileVariableDecl(VariableDecl varDecl)
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
                        initializer.Expression.AcceptCompiler(this);
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
                                initializer.Expression.AcceptCompiler(this);
                            currentFrame.PutItem(initializer.Name, returnedValue);
                            break;
                        default:
                            throw new ScriptException(fileName, initializer,
                                string.Format(Resources.NameConflict, initializer.Name));
                    }
                else
                    throw new ScriptException(fileName, initializer,
                        string.Format(Resources.NameConflict, initializer.Name));
            }
        }

        public void CompileBlock(Block block)
        {
            currentFrame.PushBlock();

            foreach (KeyValuePair<string, Label> pair in block.Labels)
                RegisterLabel(pair.Key, pair.Value);

            try
            {
                int counter = 0;
                while (counter < block.Statements.Length)
                {
                    block.Statements[counter].AcceptCompiler(this);
                    switch (jumpCode)
                    {
                        case JumpCode.None:
                            ++counter;
                            break;
                        case JumpCode.Goto:
                            if (block.Labels.ContainsKey(lastGoto.LabelName))
                            {
                                counter = block.Labels[lastGoto.LabelName].Address;
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

        public void CompileAssignment(Assignment assignment)
        {
            try
            {
                switch (assignment.Operator)
                {
                    case BinaryOperator.IfNull:
                        assignment.LeftOperand.AcceptCompiler(this);
                        if (returnedValue.Class == Class.Void)
                            assignment.RightOperand.AcceptCompiler(this);
                        return; // IMPORTANT!!!
                    case BinaryOperator.None:
                        assignment.RightOperand.AcceptCompiler(this);
                        break;
                    default:
                        CompileBinaryExpression(assignment);
                        break;
                }

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

        public void CompileTernaryExpression(TernaryExpression terExpr)
        {
            if (IsTrue(terExpr.Test))
                terExpr.TruePart.AcceptCompiler(this);
            else
                terExpr.FalsePart.AcceptCompiler(this);
        }

        public void CompileBinaryExpression(BinaryExpression binExpr)
        {
            binExpr.LeftOperand.AcceptCompiler(this);

            try
            {
                if (returnedValue.Class.Inherits(Class.Object) && IsOverloadable(binExpr.Operator))
                {
                    string methodName = ClassMethod.GetMethodName(binExpr.Operator);
                    ClassMethod method = returnedValue.Class.GetMethod(methodName);
                    if (method == null)
                        throw new RuntimeException(fileName, binExpr, string.Format(Resources.OperatorCantBeApplied,
                            CodeGenerator.BinaryOperatorToString(binExpr.Operator), returnedValue.Class.Name));
                    CheckAccess(method, binExpr);
                    Invoke(method.Function, methodName, method.Definer, returnedValue, binExpr.RightOperand);
                }
                else
                {
                    if ((binExpr.Operator == BinaryOperator.AndAlso && !returnedValue.AsBoolean) ||
                       (binExpr.Operator == BinaryOperator.OrElse && returnedValue.AsBoolean)) return;

                    Dynamic leftOperand = returnedValue;

                    binExpr.RightOperand.AcceptCompiler(this);
                    if (binExpr.Operator == BinaryOperator.AndAlso ||
                        binExpr.Operator == BinaryOperator.OrElse) return;

                    returnedValue = leftOperand.ConversionNeeded(returnedValue.Class, binExpr.Operator)
                                  ? leftOperand.ConvertTo(returnedValue.Class).BinaryOperation(binExpr.Operator, returnedValue)
                                  : leftOperand.BinaryOperation(binExpr.Operator, returnedValue);
                }
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

        public void CompileUnaryExpression(UnaryExpression unExpr)
        {
            unExpr.Operand.AcceptCompiler(this);

            try
            {
                if (returnedValue.Class.Inherits(Class.Object) && IsOverloadable(unExpr.Operator))
                {
                    string methodName = ClassMethod.GetMethodName(unExpr.Operator);
                    ClassMethod method = returnedValue.Class.GetMethod(methodName);
                    if (method == null)
                        throw new RuntimeException(fileName, unExpr, string.Format(Resources.OperatorCantBeApplied,
                            CodeGenerator.UnaryOperatorToString(unExpr.Operator), returnedValue.Class.Name));
                    CheckAccess(method, unExpr);
                    Invoke(method.Function, methodName, method.Definer, returnedValue);
                }
                else
                    switch (unExpr.Operator)
                    {
                        case UnaryOperator.PreIncrement:
                            Assign(unExpr.Operand, new Integer(returnedValue.AsInt32 + 1));
                            break;
                        case UnaryOperator.PostIncrement:
                            Dynamic beforeIncrement = returnedValue;
                            Assign(unExpr.Operand, new Integer(returnedValue.AsInt32 + 1));
                            returnedValue = beforeIncrement;
                            break;
                        case UnaryOperator.PreDecrement:
                            Assign(unExpr.Operand, new Integer(returnedValue.AsInt32 - 1));
                            break;
                        case UnaryOperator.PostDecrement:
                            Dynamic beforeDecrement = returnedValue;
                            Assign(unExpr.Operand, new Integer(returnedValue.AsInt32 - 1));
                            returnedValue = beforeDecrement;
                            break;
                        default:
                            returnedValue = returnedValue.UnaryOperation(unExpr.Operator);
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

        public void CompileLiteral(Literal literal)
        {
            returnedValue = literal.Value;
        }

        public void CompileComplexInitializer(ComplexInitializer cplxInit)
        {
            try
            {
                cplxInit.RealPartInitializer.AcceptCompiler(this);
                double realPart = returnedValue.AsDouble;
                cplxInit.ImaginaryPartInitializer.AcceptCompiler(this);
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

        public void CompileListInitializer(ListInitializer listInit)
        {
            var list = new List<Dynamic>();

            foreach (Expression item in listInit.Items)
            {
                try
                {
                    item.AcceptCompiler(this);
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

        public void CompileMapInitializer(MapInitializer mapInit)
        {
            var dict = new Dictionary<Dynamic, Dynamic>();

            foreach (MapItemInitializer item in mapInit.ItemInitializers)
            {
                try
                {
                    item.Key.AcceptCompiler(this);
                    Dynamic key = returnedValue;
                    item.Value.AcceptCompiler(this);
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

        public void CompileSetInitializer(SetInitializer setInit)
        {
            var hashSet = new HashSet<Dynamic>();

            foreach (Expression item in setInit.Items)
            {
                try
                {
                    item.AcceptCompiler(this);
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

        public void CompileObjectInitializer(ObjectInitializer objInit)
        {
            var fields = new Dictionary<string, Dynamic>();

            foreach (PropertyInitializer propInit in objInit.PropertyInitializers)
            {
                try
                {
                    propInit.Expression.AcceptCompiler(this);
                    fields[propInit.Name] = returnedValue;
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

        public void CompileInlineFunction(InlineFunction inlineFn)
        {
            inlineFn.Function.DeclaringFrame = currentFrame == rootFrame ? null : currentFrame;
            returnedValue = new Closure(inlineFn.Function);
        }

        public void CompileVariableRef(VariableRef varRef)
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
                        returnedValue = (Dynamic) frameItem;
                        if (returnedValue == Undefined.Value && misRefAct == MissingReferenceAction.Fail)
                            throw new RuntimeException(fileName, varRef,
                                string.Format(Resources.UninitializedVariable, varRef.Name));
                        break;
                    case FrameItemKind.Constant:
                        returnedValue = ((Constant) frameItem).GetValue();
                        break;
                    case FrameItemKind.Function:
                        returnedValue = new Closure((Function) frameItem);
                        break;
                    default:
                        throw new RuntimeException(fileName, varRef,
                            string.Format(Resources.NotAVariable, varRef.Name));
                }
        }

        public void CompileItemRef(ItemRef itemRef)
        {
            try
            {
                Dynamic owner, index;
                ResolveItemRef(itemRef, out owner, out index);

                returnedValue = owner == null ? null : owner.GetItem(index);
                if (returnedValue == null && misRefAct != MissingReferenceAction.Ignore)
                    throw new RuntimeException(fileName, itemRef, string.Format(Resources.IndexNotFound, index));
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

        public void CompilePropertyRef(PropertyRef propertyRef)
        {
            try
            {
                Dynamic owner;
                ClassMember member;
                ResolvePropertyRef(propertyRef, out owner, out member);

                if (owner == null)
                    returnedValue = null;
                else if (member is ClassField)
                    returnedValue = (member.Modifier == Modifier.Static) || (member.Modifier == Modifier.StaticFinal)
                                  ? ((ClassField) member).SharedValue
                                  : owner.GetProperty(member.Name);
                else if (member is ClassProperty)
                {
                    var property = (ClassProperty) member;
                    if (!property.CanRead)
                        throw new RuntimeException(fileName, propertyRef, Resources.CannotReadProperty);
                    
                    CheckAccess(property.Reader, propertyRef);
                    Invoke(property.Reader.Function, property.Name, property.Definer, owner);
                }
                else if (member is ClassMethod)
                {
                    var method = (ClassMethod)member;
                    var args = method.Function.Parameters.Select(p => new VariableRef(p.Name)).ToArray();
                    var fn = new Function(method.Function.Parameters, Block.Return(new MethodCall(propertyRef.Owner, method.Name, args)))
                    {
                        DeclaringFrame = currentFrame == rootFrame ? null : currentFrame
                    };
                    returnedValue = new Closure(fn);
                }
                else // member is certainly null, trying to get an undeclared field
                    returnedValue = owner.GetProperty(propertyRef.PropertyName);

                if (returnedValue == null && misRefAct != MissingReferenceAction.Ignore)
                    throw new RuntimeException(fileName, propertyRef,
                        string.Format(Resources.PropertyNotFoundInObject, propertyRef.PropertyName));
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

        public void CompileStaticPropertyRef(StaticPropertyRef staticRef)
        {
            object targetProperty = ResolveName(staticRef.Name, staticRef);

            try
            {
                if (targetProperty is ClassField)
                    returnedValue = ((ClassField) targetProperty).SharedValue;
                else if (targetProperty is ClassProperty)
                {
                    var property = (ClassProperty) targetProperty;
                    if (!property.CanRead)
                        throw new RuntimeException(fileName, staticRef, Resources.CannotReadProperty);

                    CheckAccess(property.Reader, staticRef);
                    Invoke(property.Reader.Function, property.Name, property.Definer, null);
                }
                else if (targetProperty is ClassMethod)
                {
                    var method = (ClassMethod)targetProperty;
                    returnedValue = new Closure(method.Function);
                }
                else if (targetProperty is StaticTypeMember)
                    returnedValue = ((StaticTypeMember) targetProperty).GetValue();
                else
                    throw new RuntimeException(fileName, staticRef,
                        string.Format(Resources.UnresolvedMemberRef, staticRef.Name));
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

        public void CompileThisReference(ThisReference thisRef)
        {
            returnedValue = currentFrame.Context.This;
        }

        public void CompileFunctionCall(FunctionCall fnCall)
        {
            try
            {
                Function function = null;
                CallContext ctx = currentFrame.Context;

                IFrameItem frameItem = currentFrame.GetItem(fnCall.FunctionName);
                if (frameItem == null && currentFrame != rootFrame)
                    frameItem = rootFrame.GetItem(fnCall.FunctionName);

                if (frameItem != null)
                    switch (frameItem.Kind)
                    {
                        case FrameItemKind.Function:
                            function = (Function) frameItem;
                            break;
                        case FrameItemKind.Variable:
                            if (frameItem is Closure)
                            {
                                function = ((Closure) frameItem).AsFunction;
                                if (function.DeclaringFrame != null)
                                    ctx = function.DeclaringFrame.Context;
                            }
                            break;
                    }

                if (function == null)
                    throw new RuntimeException(fileName, fnCall,
                        string.Format(Resources.UndefinedFunction, fnCall.FunctionName));

                Invoke(function, fnCall.FunctionName, ctx.Self, ctx.This, fnCall.Arguments);
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

        public void CompileAnonymousCall(AnonymousCall anCall)
        {
            try
            {
                anCall.Callee.AcceptCompiler(this);
                if (!(returnedValue is Closure))
                    throw new RuntimeException(fileName, anCall.Callee, Resources.CalleeIsNotClosure);

                Function function = returnedValue.AsFunction;
                CallContext ctx = currentFrame.Context;
                if (function.DeclaringFrame != null)
                    ctx = function.DeclaringFrame.Context;

                Invoke(function, anCall.FunctionName, ctx.Self, ctx.This, anCall.Arguments);
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

        public void CompileMethodCall(MethodCall methodCall)
        {
            try
            {
                methodCall.Caller.AcceptCompiler(this);
                Dynamic _this = returnedValue;
                ClassMethod method = _this.Class.GetMethod(methodCall.FunctionName);
                Class self = currentFrame.Context.Self;
                Function function = null;

                if (method == null)
                    switch (_this.Class.ClassID)
                    {
                        case ClassID.Object:
                            ClassField field = _this.Class.GetField(methodCall.FunctionName);
                            if (field != null) CheckAccess(field, methodCall);

                            Dynamic fieldValue = field != null && (field.Modifier == Modifier.Static || field.Modifier == Modifier.StaticFinal)
                                               ? field.SharedValue
                                               : _this.GetProperty(methodCall.FunctionName);

                            if (fieldValue is Closure) function = fieldValue.AsFunction;
                            break;
                        case ClassID.Resource:
                            object target = _this.AsNativeObject;
                            InvokeNative(target.GetType(), methodCall.FunctionName, target, methodCall.Arguments);
                            return; // IMPORTANT!!!
                    }
                else
                {
                    CheckAccess(method, methodCall);
                    function = method.Function;
                    self = method.Definer;
                }

                if (function == null)
                    throw new RuntimeException(fileName, methodCall,
                        string.Format(Resources.MethodNotFound, methodCall.FunctionName, _this.Class.Name));

                Invoke(function, methodCall.FunctionName, self, _this, methodCall.Arguments);
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

        public void CompileStaticMethodCall(StaticMethodCall staticCall)
        {
            object targetMethod = ResolveName(staticCall.Name, staticCall);

            try
            {
                if (targetMethod is ClassMethod)
                {
                    var method = (ClassMethod) targetMethod;
                    Invoke(method.Function, method.Name, method.Definer, null, staticCall.Arguments);
                }
                else if (targetMethod is ClassField)
                {
                    var field = (ClassField) targetMethod;
                    
                    Function function = null;
                    if (field.SharedValue is Closure)
                        function = field.SharedValue.AsFunction;

                    if (function == null)
                        throw new RuntimeException(fileName, staticCall,
                            string.Format(Resources.MethodNotFound, field.Name, staticCall.Name));

                    Invoke(function, field.Name, currentFrame.Context.Self, null, staticCall.Arguments);
                }
                else if (targetMethod is StaticTypeMember)
                {
                    var method = (StaticTypeMember) targetMethod;
                    InvokeNative(method.Type, method.MemberName, null, staticCall.Arguments);
                }
                else
                    throw new RuntimeException(fileName, staticCall,
                        string.Format(Resources.UnresolvedMemberRef, staticCall.Name));
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

        public void CompileConstructorCall(ConstructorCall ctorCall)
        {
            object targetType = ResolveName(ctorCall.Name, ctorCall);
            
            try
            {
                if (targetType is Class)
                {
                    var klass = (Class) targetType;
                    if (klass.Modifier == Modifier.Abstract || klass.Modifier == Modifier.Static)
                        throw new RuntimeException(fileName, ctorCall,
                            string.Format(Resources.CannotCreateInstance, klass.Name));

                    ClassMethod constructor = klass.Constructor;
                    CheckAccess(constructor, ctorCall);

                    Dynamic _this = new Object(klass);
                    InitializeFields(_this);
                    Invoke(constructor.Function, constructor.Name, klass, _this, ctorCall.Arguments);

                    if (ctorCall.PropertyInitializers != null)
                        foreach (PropertyInitializer initializer in ctorCall.PropertyInitializers)
                        {
                            ClassMember member = klass.GetMember(initializer.Name, MemberKind.Field | MemberKind.Property);
                            if (member != null)
                            {
                                CheckAccess(member, ctorCall);
                                if (member.Modifier == Modifier.StaticFinal)
                                    throw new ScriptException(fileName, initializer, Resources.CannotWriteFinalField);
                            }
                            
                            initializer.Expression.AcceptCompiler(this);

                            if (member is ClassProperty)
                            {
                                var property = (ClassProperty) member;
                                if (!property.CanWrite)
                                    throw new ScriptException(fileName, initializer, Resources.CannotWriteProperty);

                                CheckAccess(property.Writer, initializer.Expression);
                                Invoke(property.Writer.Function, property.Name, property.Definer, _this, new Literal(returnedValue));
                            }
                            else
                                _this.SetProperty(initializer.Name, returnedValue);
                        }

                    returnedValue = _this;
                }
                else if (targetType is Type)
                {
                    var type = (Type) targetType;

                    var args = new Dynamic[ctorCall.Arguments.Length];
                    for (int i = 0; i < ctorCall.Arguments.Length; ++i)
                    {
                        ctorCall.Arguments[i].AcceptCompiler(this);
                        args[i] = returnedValue;
                    }

                    const BindingFlags flags = BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;
                    object obj = type.InvokeMember(null, flags, new DynamicBinder(), null, args);
                    Dynamic _this = DynamicFactory.CreateDynamic(obj);

                    if (ctorCall.PropertyInitializers != null)
                        foreach (PropertyInitializer initializer in ctorCall.PropertyInitializers)
                        {
                            initializer.Expression.AcceptCompiler(this);
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

        public void CompileParentMethodCall(ParentMethodCall pmc)
        {
            try
            {
                Dynamic _this = currentFrame.Context.This;
                ClassMethod method = _this.Class.SuperClass.GetMethod(pmc.FunctionName);
                
                if (method == null)
                    throw new RuntimeException(fileName, pmc,
                        string.Format(Resources.MethodNotFound, pmc.FunctionName, _this.Class.SuperClass.Name));
                
                if (method.Modifier == Modifier.Abstract)
                    throw new RuntimeException(fileName, pmc,
                        string.Format(Resources.CannotInvokeAbstractMethod, method.FullName));
                
                CheckAccess(method, pmc);
                Invoke(method.Function, pmc.FunctionName, method.Definer, _this, pmc.Arguments);
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

        public void CompileParentConstructorCall(ParentConstructorCall pcc)
        {
            try
            {
                CallContext context = currentFrame.Context;
                ClassMethod constructor = context.Self.SuperClass.Constructor;
                CheckAccess(constructor, pcc);
                Invoke(constructor.Function, constructor.Name, constructor.Definer, context.This, pcc.Arguments);
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

        public void CompileInnerFunctionCall(InnerFunctionCall innerCall)
        {
            var arguments = new Dynamic[innerCall.Arguments.Length];

            for (int i = 0; i < arguments.Length; ++i)
            {
                innerCall.Arguments[i].AcceptCompiler(this);
                arguments[i] = returnedValue;
            }

            returnedValue = innerCall.Function.Logic(arguments);
        }

        public void CompileExternalFunctionCall(ExternalFunctionCall extCall)
        {
            ParameterInfo[] parameters = extCall.Method.GetParameters();
            var args = new object[extCall.Arguments.Length];

            for (int i = 0; i < extCall.Arguments.Length; ++i)
            {
                extCall.Arguments[i].AcceptCompiler(this);
                args[i] = returnedValue.ConvertTo(parameters[i].ParameterType);
            }

            object obj = extCall.Method.Invoke(null, args);
            returnedValue = DynamicFactory.CreateDynamic(obj);
        }

        public void CompileTypeVerification(TypeVerification typeVerif)
        {
            MissingReferenceAction prevAction = misRefAct;

            try
            {
                var klass = rootFrame.GetItem(typeVerif.TypeName) as Class;
                if (klass == null)
                    throw new RuntimeException(fileName, typeVerif,
                        string.Format(Resources.UndefinedType, typeVerif.TypeName));

                misRefAct = MissingReferenceAction.Ignore;
                typeVerif.Expression.AcceptCompiler(this);
                bool b = returnedValue == null
                       ? klass == Class.Void
                       : returnedValue.InstanceOf(klass);
                returnedValue = Boolean.FromBool(b);
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

        public void CompileTypeOfExpression(TypeOfExpression typeOf)
        {
            var klass = rootFrame.GetItem(typeOf.TypeName) as Class;
            if (klass == null)
                throw new RuntimeException(fileName, typeOf,
                    string.Format(Resources.UndefinedType, typeOf.TypeName));

            returnedValue = GetTypeInfo(klass);
        }

        public void CompileConversion(Conversion conversion)
        {
            try
            {
                conversion.Expression.AcceptCompiler(this);
                if (returnedValue.Class.Inherits(Class.Object))
                    throw new RuntimeException(fileName, conversion.Expression,
                        string.Format(Resources.CannotConvertFrom, returnedValue.Class.Name));

                var klass = rootFrame.GetItem(conversion.TypeName) as Class;
                if (klass == null)
                    throw new RuntimeException(fileName, conversion,
                        string.Format(Resources.UndefinedType, conversion.TypeName));
                if (klass.ClassID < ClassID.Boolean || klass.ClassID > ClassID.Object)
                    throw new RuntimeException(fileName, conversion,
                        string.Format(Resources.CannotConvertTo, conversion.TypeName));

                returnedValue = returnedValue.ConvertTo(klass);
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

        public void CompileIfThenElse(IfThenElse ifThenElse)
        {
            if (IsTrue(ifThenElse.Condition))
                ifThenElse.IfBlock.AcceptCompiler(this);
            else if (ifThenElse.ElseBlock != null)
                ifThenElse.ElseBlock.AcceptCompiler(this);
        }

        public void CompileSwitchBlock(SwitchBlock switchBlock)
        {
            switchBlock.Expression.AcceptCompiler(this);
            int hashCode = returnedValue.GetHashCode();

            int counter = switchBlock.DefaultCase;
            foreach (CaseLabel caseLbl in switchBlock.Cases)
            {
                if (caseLbl.GetHashCode() != hashCode) continue;
                counter = caseLbl.Address;
                break;
            }

            currentFrame.PushBlock();

            foreach (KeyValuePair<string, Label> pair in switchBlock.Labels)
                RegisterLabel(pair.Key, pair.Value);

            try
            {
                while (counter < switchBlock.Statements.Length)
                {
                    switchBlock.Statements[counter].AcceptCompiler(this);
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
                            if (switchBlock.Labels.ContainsKey(lastGoto.LabelName))
                            {
                                counter = switchBlock.Labels[lastGoto.LabelName].Address;
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

        public void CompileForLoop(ForLoop forLoop)
        {
            currentFrame.PushBlock();

            foreach (Statement initializer in forLoop.Initializers)
                initializer.AcceptCompiler(this);

            Expression condition = forLoop.Guard ?? new Literal(Boolean.True);
            while (IsTrue(condition))
            {
                forLoop.Body.AcceptCompiler(this);
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
                    updater.AcceptCompiler(this);
            }

        EXIT:
            currentFrame.PopBlock();
        }

        public void CompileForEachLoop(ForEachLoop forEach)
        {
            currentFrame.PushBlock();

            var enumerable = GetEnumerable(forEach.Enumerated);
            foreach (KeyValuePair<Dynamic, Dynamic> pair in enumerable)
            {
                RegisterVariable(forEach.KeyName, pair.Key);
                RegisterVariable(forEach.ValueName, pair.Value);

                forEach.Body.AcceptCompiler(this);
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

        public void CompileWhileLoop(WhileLoop whileLoop)
        {
            while (IsTrue(whileLoop.Guard))
            {
                whileLoop.Body.AcceptCompiler(this);
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

        public void CompileDoLoop(DoLoop doLoop)
        {
            do
            {
                doLoop.Body.AcceptCompiler(this);
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

        public void CompileContinue(Continue _continue)
        {
            jumpCode = JumpCode.Continue;
        }

        public void CompileBreak(Break _break)
        {
            jumpCode = JumpCode.Break;
        }

        public void CompileGoto(Goto _goto)
        {
            lastGoto = _goto;
            jumpCode = JumpCode.Goto;
        }

        public void CompileReturn(Return _return)
        {
            if (_return.Expression == null)
                returnedValue = Void.Value;
            else
                _return.Expression.AcceptCompiler(this);

            jumpCode = JumpCode.Return;
        }

        public void CompileThrow(Throw _throw)
        {
            _throw.Expression.AcceptCompiler(this);
            if (returnedValue.InstanceOf(Class.Exception))
                throw new RuntimeException(fileName, _throw, returnedValue);
            throw new RuntimeException(fileName, _throw, returnedValue.ToString());
        }

        public void CompileTryCatchFinally(TryCatchFinally tcf)
        {
            currentFrame.PushBlock();

            try
            {
                tcf.TryBlock.AcceptCompiler(this);
            }
            catch (ScriptException ex)
            {
                currentFrame.PutItem(tcf.ExceptionName, ConvertException(ex));
                tcf.CatchBlock.AcceptCompiler(this);
            }
            finally
            {
                if (tcf.FinallyBlock != null)
                {
                    JumpCode prevCode = jumpCode;
                    Dynamic prevValue = returnedValue;
                    Goto prevGoto = lastGoto;

                    try
                    {
                        jumpCode = JumpCode.None;
                        tcf.FinallyBlock.AcceptCompiler(this);
                        if (jumpCode == JumpCode.Goto)
                            throw new RuntimeException(fileName, lastGoto, Resources.CannotJumpOutOfFinallyBlock);
                    }
                    finally
                    {
                        lastGoto = prevGoto;
                        returnedValue = prevValue;
                        jumpCode = prevCode;
                    }
                }

                currentFrame.PopBlock();
            }
        }

        #endregion

        #region Frames Management

        /// <summary>
        /// Creates the root frame.
        /// </summary>
        private void CreateRootFrame()
        {
            var rootFrameContext = new CallContext(null, null, ROOT_FRAME_NAME);
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
        private void PushFrame(Class currentClass, Dynamic currentInstance, string methodName,
                               Dictionary<string, IFrameItem> initialItems)
        {
            var callContext = new CallContext(currentClass, currentInstance, methodName);
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
            foreach (KeyValuePair<string, object> pair in InitialContext.Variables)
                currentFrame.PutItem(pair.Key, DynamicFactory.CreateDynamic(pair.Value));

            // Makes the context available to the script
            currentFrame.PutItem(CONTEXT_VARIABLE_NAME, new Resource(InitialContext));
        }

        /// <summary>
        /// Registers a variable in the stack.
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <param name="variable">The variable to update</param>
        private void RegisterVariable(string name, Dynamic variable)
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
            if (frameItem != null)
                throw new ScriptException(fileName, label,
                    string.Format(Resources.NameConflict, name));
            
            currentFrame.PutItem(name, label);
        }

        /// <summary>
        /// Gets the initial frame items for a call.
        /// </summary>
        /// <param name="function">The function we are calling</param>
        /// <param name="functionName">The function's name</param>
        /// <param name="arguments">The arguments passed to that function</param>
        /// <returns>A dictionary of frame items</returns>
        private Dictionary<string, IFrameItem> GetInitialFrameItems(Function function, string functionName, Expression[] arguments)
        {
            // Check the minimum number of arguments
            int minNumArgs = function.MinNumArgs;
            if (arguments.Length < minNumArgs)
                throw new InvalidProgramException(string.Format(Resources.TooFewArgs, functionName));

            // Check the maximum number of arguments
            int maxNumArgs = function.MaxNumArgs;
            if (maxNumArgs < arguments.Length)
                throw new InvalidProgramException(string.Format(Resources.TooManyArgs, functionName));

            var frameItems = new Dictionary<string, IFrameItem>();
            int counter = 0;

            // Pass the required arguments
            while (counter < minNumArgs)
            {
                arguments[counter].AcceptCompiler(this);
                frameItems.Add(function.Parameters[counter].Name, returnedValue);
                ++counter;
            }

            // Continue with optional arguments
            while (counter < arguments.Length)
            {
                if (function.Parameters[counter].VaArgs)
                {
                    // If the current parameter is a variably sized list,
                    // fill it with the remaining arguments
                    Dynamic vaList = new List();

                    if (counter == arguments.Length - 1)
                    {
                        arguments[counter].AcceptCompiler(this);
                        if (returnedValue.Class == Class.List)
                            vaList = returnedValue;
                        else
                            vaList.AsList.Add(returnedValue);
                    }
                    else
                    {
                        int k = counter;

                        do
                        {
                            arguments[k++].AcceptCompiler(this);
                            vaList.AsList.Add(returnedValue);
                        } while (k < arguments.Length);
                    }

                    frameItems.Add(function.Parameters[counter].Name, vaList);
                    counter = int.MaxValue;
                }
                else
                {
                    // Otherwise, set the value provided to the optional parameter
                    arguments[counter].AcceptCompiler(this);
                    frameItems.Add(function.Parameters[counter].Name, returnedValue);
                    ++counter;
                }
            }

            // Finish with the values that have not been explicitly provided
            while (counter < function.Parameters.Length)
            {
                Parameter parameter = function.Parameters[counter];
                frameItems.Add(parameter.Name, parameter.VaArgs ? new List() : parameter.DefaultValue);
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
                if (parameter.ByRef) Assign(arguments[i], (Dynamic) frameItems[parameter.Name]);
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
        /// <param name="self">The class in which the function is declared (if it's a method)</param>
        /// <param name="_this">The function's caller (if it's a method)</param>
        /// <param name="arguments">The arguments passed to the function</param>
        private void Invoke(Function function, string name, Class self, Dynamic _this, params Expression[] arguments)
        {
            var frameItems = GetInitialFrameItems(function, name, arguments);
            PushFrame(self, _this, name, frameItems);
            
            try
            {
                function.Body.AcceptCompiler(this);
            }
            finally
            {
                PopFrame();

                Dynamic result = returnedValue;
                CopyBackFrameItems(function, arguments, frameItems);
                returnedValue = result;

                if (jumpCode == JumpCode.Goto)
                    throw new RuntimeException(fileName, lastGoto, string.Format(Resources.MissingLabel, lastGoto.LabelName));
                jumpCode = JumpCode.None;
            }
        }

        /// <summary>
        /// Updates the initial context's variables.
        /// </summary>
        public void UpdateInitialContextVariables()
        {
            var names = new List<string>(InitialContext.Variables.Keys);
            foreach (string name in names)
            {
                var value = (Dynamic) rootFrame.GetItem(name);
                InitialContext.Variables[name] = value.AsNativeObject;
            }
        }

        #endregion

        #region OOP Support

        /// <summary>
        /// Gets if the given unary operator is overloadable or not.
        /// </summary>
        /// <param name="_operator">The given unary operator</param>
        /// <returns><b>false</b> for !; <b>true</b> for any other</returns>
        private static bool IsOverloadable(UnaryOperator _operator)
        {
            switch (_operator)
            {
                case UnaryOperator.None:
                case UnaryOperator.Not:
                case UnaryOperator.PostIncrement:
                case UnaryOperator.PostDecrement:
                    return false;
                default:
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
            switch (_operator)
            {
                case BinaryOperator.None:
                case BinaryOperator.AndAlso:
                case BinaryOperator.OrElse:
                case BinaryOperator.Identical:
                case BinaryOperator.NotIdentical:
                case BinaryOperator.IfNull:
                    return false;
                default:
                    return true;
            }
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
                    field.Initializer.AcceptCompiler(this);
                    field.SharedValue = returnedValue;
                }
        }

        /// <summary>
        /// Initializes the fields of an instance.
        /// </summary>
        /// <param name="instance">A class instance</param>
        private void InitializeFields(Dynamic instance)
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
                            field.Initializer.AcceptCompiler(this);
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
            CallContext ctx = currentFrame.Context;

            switch (member.Scope)
            {
                case Scope.Private:
                    violation = ctx.Self == null || ctx.Self != member.Definer;
                    break;
                case Scope.Protected:
                    violation = ctx.Self == null ||
                                (ctx.Self != member.Definer &&
                                !ctx.Self.Inherits(member.Definer));
                    break;
            }

            if (violation)
                throw new RuntimeException(fileName, astNode, 
                    string.Format(Resources.AccessDenied, member.FullName,
                    ctx.Self == null ? "public" : ctx.Self.Name));
        }

        /// <summary>
        /// Converts a ScriptException to an AddyScript exception.
        /// </summary>
        /// <param name="sx">The native exception to convert</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic ConvertException(ScriptException sx)
        {
            if (sx is RuntimeException)
            {
                var rx = (RuntimeException) sx;
                if (rx.Thrown != null) return rx.Thrown;
            }

            Dynamic ex = new Object(Class.Exception);
            InitializeFields(ex);
            if (sx.InnerException != null)
                ex.SetProperty("_name", new String(sx.InnerException.GetType().Name));
            ex.SetProperty("_message", new String(sx.Message));
            ex.SetProperty("_source", new String(fileName));
            ex.SetProperty("_line", new Integer(sx.ScriptElement.Start.LineNumber));

            return ex;
        }

        /// <summary>
        /// Gets a TypeInfo from a class.
        /// </summary>
        /// <param name="klass">The target class</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetTypeInfo(Class klass)
        {
            if (!typeInfoCache.ContainsKey(klass))
            {
                Dynamic typeInfo = new Object(Class.TypeInfo);
                Dynamic superType = klass.SuperClass == null
                                  ? (Dynamic) Void.Value
                                  : new String(klass.SuperClass.Name);

                InitializeFields(typeInfo);
                typeInfo.SetProperty("_superType", superType);
                typeInfo.SetProperty("_modifier", new String(klass.Modifier.ToString()));
                typeInfo.SetProperty("_name", new String(klass.Name));
                typeInfo.SetProperty("_constructor", GetMethodInfo(klass.Constructor));
                typeInfo.SetProperty("_fields", GetFieldInfoMap(klass));
                typeInfo.SetProperty("_properties", GetPropertyInfoMap(klass));
                typeInfo.SetProperty("_methods", GetMethodInfoMap(klass));
                typeInfo.SetProperty("_events", GetEventInfoMap(klass));
                
                typeInfoCache.Add(klass, typeInfo);
            }

            return typeInfoCache[klass];
        }

        /// <summary>
        /// Gets a MemberInfo from a ClassMember.
        /// </summary>
        /// <param name="member">The ClassMember</param>
        /// <param name="klass">The class for which to create an instance</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetMemberInfo(ClassMember member, Class klass)
        {
            Dynamic memberInfo = new Object(klass);

            InitializeFields(memberInfo);
            memberInfo.SetProperty("_scope", new String(member.Scope.ToString()));
            memberInfo.SetProperty("_modifier", new String(member.Modifier.ToString()));
            memberInfo.SetProperty("_name", new String(member.Name));
            memberInfo.SetProperty("_definer", new String(member.Definer.Name));

            return memberInfo;
        }

        /// <summary>
        /// Gets a FieldInfo from a ClassField.
        /// </summary>
        /// <param name="field">The ClassField</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetFieldInfo(ClassField field)
        {
            Dynamic fieldInfo = GetMemberInfo(field, Class.FieldInfo);
            
            fieldInfo.SetProperty("_sharedValue", field.SharedValue ?? Void.Value);

            return fieldInfo;
        }

        /// <summary>
        /// Gets a map of FieldInfo from the fields of a class.
        /// </summary>
        /// <param name="cls">The target class</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetFieldInfoMap(Class cls)
        {
            var fieldInfoMap = new Map();

            foreach (ClassField field in cls.GetMembers(MemberKind.Field))
            {
                Dynamic fieldName = new String(field.Name);
                fieldInfoMap.SetItem(fieldName, GetFieldInfo(field));
            }

            return fieldInfoMap;
        }

        /// <summary>
        /// Gets a PropertyInfo from a ClassProperty.
        /// </summary>
        /// <param name="property">The ClassProperty</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetPropertyInfo(ClassProperty property)
        {
            Dynamic propertyInfo = GetMemberInfo(property, Class.PropertyInfo);

            propertyInfo.SetProperty("_reader", property.CanRead ? GetMethodInfo(property.Reader) : Void.Value);
            propertyInfo.SetProperty("_writer", property.CanWrite ? GetMethodInfo(property.Writer) : Void.Value);
            
            return propertyInfo;
        }

        /// <summary>
        /// Gets a map of PropertyInfo from the propertys of a class.
        /// </summary>
        /// <param name="cls">The target class</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetPropertyInfoMap(Class cls)
        {
            var propertyInfoMap = new Map();

            foreach (ClassProperty property in cls.GetMembers(MemberKind.Property))
            {
                Dynamic propertyName = new String(property.Name);
                propertyInfoMap.SetItem(propertyName, GetPropertyInfo(property));
            }

            return propertyInfoMap;
        }

        /// <summary>
        /// Gets a MethodInfo from a ClassMethod.
        /// </summary>
        /// <param name="method">The ClassMethod</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetMethodInfo(ClassMethod method)
        {
            Dynamic methodInfo = GetMemberInfo(method, Class.MethodInfo);
            
            methodInfo.SetProperty("_parameters",
                GetParameterInfoMap(method.Function.Parameters));

            return methodInfo;
        }

        /// <summary>
        /// Gets a map of MemberInfo from the methods of a class.
        /// </summary>
        /// <param name="cls">The target class</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetMethodInfoMap(Class cls)
        {
            var methodInfoMap = new Map();

            foreach (ClassMethod method in cls.GetMembers(MemberKind.Method))
            {
                Dynamic methodName = new String(method.Name);
                methodInfoMap.SetItem(methodName, GetMethodInfo(method));
            }

            return methodInfoMap;
        }

        /// <summary>
        /// Gets an EventInfo from a ClassEvent.
        /// </summary>
        /// <param name="_event">The ClassEvent</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetEventInfo(ClassEvent _event)
        {
            Dynamic _eventInfo = GetMemberInfo(_event, Class.EventInfo);

            _eventInfo.SetProperty("_parameters",
                GetParameterInfoMap(_event.Parameters));

            return _eventInfo;
        }

        /// <summary>
        /// Gets a map of MemberInfo from the events of a class.
        /// </summary>
        /// <param name="cls">The target class</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetEventInfoMap(Class cls)
        {
            var eventInfoMap = new Map();

            foreach (ClassEvent _event in cls.GetMembers(MemberKind.Event))
            {
                Dynamic eventName = new String(_event.Name);
                eventInfoMap.SetItem(eventName, GetEventInfo(_event));
            }

            return eventInfoMap;
        }

        /// <summary>
        /// Gets a ParameterInfo from a Parameter.
        /// </summary>
        /// <param name="parameter">The Parameter</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetParameterInfo(Parameter parameter)
        {
            Dynamic parameterInfo = new Object(Class.ParameterInfo);

            InitializeFields(parameterInfo);
            parameterInfo.SetProperty("_name", new String(parameter.Name));
            parameterInfo.SetProperty("_byRef", Boolean.FromBool(parameter.ByRef));
            parameterInfo.SetProperty("_vaArgs", Boolean.FromBool(parameter.VaArgs));
            parameterInfo.SetProperty("_defaultValue", parameter.DefaultValue ?? Void.Value);

            return parameterInfo;
        }

        /// <summary>
        /// Gets a map of ParameterInfo from a parameters set.
        /// </summary>
        /// <param name="parameters">The given parameters set</param>
        /// <returns>A <see cref="Dynamic"/></returns>
        private Dynamic GetParameterInfoMap(Parameter[] parameters)
        {
            var parameterInfoMap = new Map();

            foreach (Parameter parameter in parameters)
            {
                Dynamic paramName = new String(parameter.Name);
                parameterInfoMap.SetItem(paramName, GetParameterInfo(parameter));
            }

            return parameterInfoMap;
        }

        #endregion

        #region .Net Interop Management

        private void InvokeNative(Type type, string methodName, object target, Expression[] arguments)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                       BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

            object result;
            var dynamics = new Dynamic[arguments.Length];
            var values = new object[dynamics.Length];
            
            for (int i = 0; i < arguments.Length; ++i)
            {
                arguments[i].AcceptCompiler(this);
                dynamics[i] = returnedValue;
            }
            
            if (type.IsCOMObject)
            {
                for (int i = 0; i < dynamics.Length; ++i)
                    values[i] = dynamics[i].AsNativeObject;
                result = type.InvokeMember(methodName, flags, null, target, values);
            }
            else
            {
                MethodInfo match = DynamicBinder.FindMethod(type, methodName, dynamics, flags);
                if (match == null) throw new MissingMethodException(type.FullName, methodName);

                ParameterInfo[] parameters = match.GetParameters();
                for (int i = 0; i < dynamics.Length; ++i)
                    values[i] = dynamics[i].ConvertTo(parameters[i].ParameterType);

                result = match.Invoke(target, values);

                for (int i = 0; i < arguments.Length; ++i)
                    if (parameters[i].IsOut)
                        try
                        {
                            Assign(arguments[i], DynamicFactory.CreateDynamic(values[i]));
                        }
                        catch (RuntimeException)
                        {
                        }
            }

            returnedValue = DynamicFactory.CreateDynamic(result);
        }

        /// <summary>
        /// Generates a P/Invoke method for the specified native DLL function.
        /// </summary>
        /// <param name="libName">The name of the native DLL</param>
        /// <param name="procName">The procedure's name</param>
        /// <param name="returnType">The type of the returned value</param>
        /// <param name="paramTypes">The types of the parameters</param>
        /// <returns>A <see cref="MethodInfo"/></returns>
        private MethodInfo GetPInvokeMethod(string libName, string procName, Type returnType, Type[] paramTypes)
        {
            var aName = new AssemblyName(Path.GetFileNameWithoutExtension(libName) + "_" + procName);
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);

            mb.DefinePInvokeMethod(procName, libName, MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard, returnType, paramTypes, CallingConvention.Winapi, CharSet.Auto);
            mb.CreateGlobalFunctions();

            return mb.GetMethod(procName);
        }

        /// <summary>
        /// Gets the .Net type that meets a particular name
        /// </summary>
        /// <param name="typeName">The given type name</param>
        /// <returns>A <see cref="System.Type"/></returns>
        private Type GetTypeByName(string typeName)
        {
            foreach (Assembly asm in InitialContext.References)
            {
                Type type = asm.GetType(typeName);
                if (type == null) continue;
                return type;
            }

            return ScriptContext.Mscorlib.GetType("System." + typeName);
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
                condition.AcceptCompiler(this);
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
        private IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetEnumerable(Expression expr)
        {
            try
            {
                expr.AcceptCompiler(this);
                return returnedValue.Class.Inherits(Class.Object)
                     ? GetProgrammaticEnumerable(returnedValue, expr)
                     : returnedValue.GetEnumerable();
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
        private IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetProgrammaticEnumerable(Dynamic value, Expression expr)
        {
            ClassMethod moveFirstMethod = value.Class.GetMethod("moveFirst");
            if (moveFirstMethod == null)
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, value.Class.Name));
            CheckAccess(moveFirstMethod, expr);

            ClassMethod hasNextMethod = value.Class.GetMethod("hasNext");
            if (hasNextMethod == null)
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, value.Class.Name));
            CheckAccess(hasNextMethod, expr);

            ClassMethod moveNextMethod = value.Class.GetMethod("moveNext");
            if (moveNextMethod == null)
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, value.Class.Name));
            CheckAccess(moveNextMethod, expr);

            Invoke(moveFirstMethod.Function, moveFirstMethod.Name, moveFirstMethod.Definer, value);
            Invoke(hasNextMethod.Function, hasNextMethod.Name, hasNextMethod.Definer, value);

            int counter = 0;
            while (returnedValue.AsBoolean)
            {
                Invoke(moveNextMethod.Function, moveNextMethod.Name, moveNextMethod.Definer, value);
                yield return new KeyValuePair<Dynamic, Dynamic>(new Integer(counter++), returnedValue);

                Invoke(hasNextMethod.Function, hasNextMethod.Name, hasNextMethod.Definer, value);
            }
        }

        /// <summary>
        /// Imports another script from whithin the calling one.
        /// </summary>
        /// <param name="scriptName">The name of the script to be imported</param>
        /// <returns><b>true</b> if a script has been imported;<b>false</b> otherwise</returns>
        private bool ImportScript(QualifiedName scriptName)
        {
            var searchDirs = new List<string>();
            if (!string.IsNullOrEmpty(fileName)) searchDirs.Add(Path.GetDirectoryName(fileName));
            searchDirs.Add(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            searchDirs.AddRange(InitialContext.SearchPath);

            string path = null;
            foreach (string directory in searchDirs)
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
                    program.AcceptCompiler(this);
                }

                frames = savedFrames;
                savedRootFrame.CopyRootItems(rootFrame);  // Note: Items may be copied into a module in the future
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
            string dottedName = _namespace.ToDottedName();
            bool aliasDefined = !string.IsNullOrEmpty(alias);

            foreach (Assembly asm in InitialContext.References)
                foreach (Type type in asm.GetExportedTypes())
                    if (type.FullName == dottedName)
                    {
                        CacheType(type, _namespace, aliasDefined ? alias : type.Name);
                        return true;
                    }

            string prefix = dottedName + ".";
            var types = new List<Type>();

            foreach (Assembly asm in InitialContext.References)
                foreach (Type type in asm.GetExportedTypes())
                    if (type.FullName.StartsWith(prefix))
                        types.Add(type);

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

            IFrameItem frameItem = currentFrame.GetItem(name[0]);
            if (frameItem == null && currentFrame != rootFrame)
                frameItem = rootFrame.GetItem(name[0]);

            if (frameItem is Class)
            {
                var klass = (Class) frameItem;

                switch (name.Length)
                {
                    case 1:
                        return klass;
                    case 2:
                        ClassMember member = klass.GetMember(name[1]);
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

            foreach (Assembly asm in InitialContext.References)
                for (int k = name.Length; k > 0; --k)
                {
                    Type type = asm.GetType(name.Subname(0, k).ToDottedName());
                    if (type == null) continue;
                    CacheType(type);

                    return nameCache[name];
                }

            return Type.GetTypeFromProgID(name.ToDottedName());
        }

        /// <summary>
        /// Registers a type in the cache with a short name.<br/>
        /// </summary>
        /// <param name="type">The type to be registered to the cache</param>
        /// <param name="_namespace">A prefix to be removed from the full type's name</param>
        /// <param name="alias">An alias that will be used to replace the original prefix</param>
        private void CacheType(Type type, QualifiedName _namespace, string alias)
        {
            QualifiedName originalName = QualifiedName.ParseDottedName(type.FullName.Replace('+', '.'));
            QualifiedName newName = originalName.Subname(_namespace.Length).Prepend(alias);
            if (nameCache.Contains(newName)) return;
            
            nameCache.Add(newName, type);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            foreach (MemberInfo member in type.GetMembers(flags))
            {
                QualifiedName memberName = newName.Apppend(member.Name);
                if (!nameCache.Contains(memberName))
                    nameCache.Add(memberName, new StaticTypeMember(type, member.Name));
            }

            foreach (Type nestedType in type.GetNestedTypes())
                CacheType(nestedType, _namespace, alias);
        }

        /// <summary>
        /// Registers a type in the cache with a short name.<br/>
        /// </summary>
        /// <param name="type">The type to be registered to the cache</param>
        /// <param name="_namespace">A prefix to be removed from the full type's name</param>
        private void CacheType(Type type, QualifiedName _namespace)
        {
            QualifiedName originalName = QualifiedName.ParseDottedName(type.FullName.Replace('+', '.'));
            QualifiedName newName = originalName.Subname(_namespace.Length);
            if (nameCache.Contains(newName)) return;

            nameCache.Add(newName, type);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            foreach (MemberInfo member in type.GetMembers(flags))
            {
                QualifiedName memberName = newName.Apppend(member.Name);
                if (!nameCache.Contains(memberName))
                    nameCache.Add(memberName, new StaticTypeMember(type, member.Name));
            }

            foreach (Type nestedType in type.GetNestedTypes())
                CacheType(nestedType, _namespace);
        }

        /// <summary>
        /// Registers a type in the cache as well as all its members and nested types
        /// to make them quickly accessible on further calls.
        /// </summary>
        /// <param name="type">The type to register to the cache</param>
        private void CacheType(Type type)
        {
            QualifiedName typeName = QualifiedName.ParseDottedName(type.FullName.Replace('+', '.'));
            if (nameCache.Contains(typeName)) return;

            nameCache.Add(typeName, type);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            foreach (MemberInfo member in type.GetMembers(flags))
            {
                QualifiedName memberName = typeName.Apppend(member.Name);
                if (!nameCache.Contains(memberName))
                    nameCache.Add(memberName, new StaticTypeMember(type, member.Name));
            }

            foreach (Type nestedType in type.GetNestedTypes())
                CacheType(nestedType);
        }

        /// <summary>
        /// Evaluates the <i>Owner</i> and <i>Index</i> members of an ItemRef expression and
        /// returns the corresponding variables. May create the owner if requested.
        /// </summary>
        /// <param name="itemRef">An <see cref="ItemRef"/></param>
        /// <param name="owner">Will contain the owner upon completion</param>
        /// <param name="index">Will contain the index upon completion</param>
        private void ResolveItemRef(ItemRef itemRef, out Dynamic owner, out Dynamic index)
        {
            itemRef.Index.AcceptCompiler(this);
            index = returnedValue;

            itemRef.Owner.AcceptCompiler(this);
            owner = returnedValue;
        }

        /// <summary>
        /// Evaluates the <i>Owner</i> member of a PropertyRef expression and
        /// returns the corresponding variable. May create the owner if requested.
        /// </summary>
        /// <param name="propertyRef">A <see cref="PropertyRef"/></param>
        /// <param name="owner">Will contain the owner upon completion</param>
        /// <param name="member">Will contain a member's definition upon completion</param>
        private void ResolvePropertyRef(PropertyRef propertyRef, out Dynamic owner, out ClassMember member)
        {
            propertyRef.Owner.AcceptCompiler(this);
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
        private void Assign(Expression lValue, Dynamic rValue)
        {
            if (lValue is VariableRef)
                RegisterVariable(((VariableRef) lValue).Name, returnedValue = rValue);
            else if (lValue is ItemRef)
            {
                Dynamic owner, index;
                ResolveItemRef((ItemRef) lValue, out owner, out index);
                owner.SetItem(index, returnedValue = rValue);
            }
            else if (lValue is PropertyRef)
            {
                Dynamic owner;
                ClassMember member;

                var propertyRef = (PropertyRef) lValue;
                ResolvePropertyRef(propertyRef, out owner, out member);

                if (member == null)
                    owner.SetProperty(propertyRef.PropertyName, returnedValue = rValue);
                else if (member is ClassField)
                    switch (member.Modifier)
                    {
                        case Modifier.Default:
                            owner.SetProperty(member.Name, returnedValue = rValue);
                            break;
                        case Modifier.Static:
                            ((ClassField)member).SharedValue = returnedValue = rValue;
                            break;
                        case Modifier.Final:
                            if (currentFrame.Context.IsConstructor())
                                owner.SetProperty(member.Name, returnedValue = rValue);
                            else
                                throw new RuntimeException(fileName, propertyRef, Resources.CannotWriteFinalField);
                            break;
                        default: // StaticFinal
                            throw new RuntimeException(fileName, propertyRef, Resources.CannotWriteFinalField);
                    }
                else if (member is ClassProperty)
                {
                    var property = (ClassProperty)member;
                    if (!property.CanWrite)
                        throw new RuntimeException(fileName, propertyRef, Resources.CannotWriteProperty);

                    CheckAccess(property.Writer, propertyRef);
                    Invoke(property.Writer.Function, property.Name, property.Definer, owner, new Literal(rValue));
                }
                else
                    throw new RuntimeException(fileName, lValue, Resources.InvalidLValue);
            }
            else if (lValue is StaticPropertyRef)
            {
                var staticRef = (StaticPropertyRef) lValue;
                object targetProperty = ResolveName(staticRef.Name, lValue);

                if (targetProperty is ClassField)
                {
                    var field = (ClassField)targetProperty;
                    if (field.Modifier == Modifier.Static)
                        field.SharedValue = returnedValue = rValue;
                    else // Static + Final
                        throw new RuntimeException(fileName, staticRef, Resources.CannotWriteFinalField);
                }
                else if (targetProperty is ClassProperty)
                {
                    var property = (ClassProperty)targetProperty;
                    if (!property.CanWrite)
                        throw new RuntimeException(fileName, staticRef, Resources.CannotWriteProperty);

                    CheckAccess(property.Writer, staticRef);
                    Invoke(property.Writer.Function, property.Name, property.Definer, null, new Literal(rValue));
                }
                else if (targetProperty is StaticTypeMember)
                    ((StaticTypeMember)targetProperty).SetValue(rValue);
                else
                    throw new RuntimeException(fileName, staticRef, string.Format(Resources.UnresolvedMemberRef, staticRef.Name));
            }
            else
                throw new RuntimeException(fileName, lValue, Resources.InvalidLValue);
        }

        #endregion
    }
}