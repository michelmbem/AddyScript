using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using AddyScript.Ast;
using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Parsers;
using AddyScript.Properties;
using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.Frames;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;
using AddyScript.Translators.Utility;
using Boolean = AddyScript.Runtime.DataItems.Boolean;
using Complex = AddyScript.Runtime.DataItems.Complex;
using Label = AddyScript.Ast.Statements.Label;
using Object = AddyScript.Runtime.DataItems.Object;
using String = AddyScript.Runtime.DataItems.String;
using Tuple = AddyScript.Runtime.DataItems.Tuple;
using Void = AddyScript.Runtime.DataItems.Void;


namespace AddyScript.Translators;


public class Interpreter : ITranslator, IAssignmentProcessor
{
    #region Constants

    internal const string MODULE_NAME_CONSTANT = "__name";
    internal const string MAIN_MODULE_NAME = "main";
    internal const string ROOT_FRAME_NAME = "root";
    internal const string CONTEXT_VARIABLE_NAME = "__context";

    #endregion

    #region Fields

    /**
     * Note: Do not read a state field twice expecting it to have the same value!
     * Invocations of RuntimeServices methods may change the value of state field at any time.
     * This applies more specifically to returnedValue: always make a copy of it for later reuse.
     */

    private readonly HashSet<string> importedModules = [];
    private readonly NameTree nameCache = new ();
    private readonly Dictionary<Class, DataItem> typeInfoCache = [];
    private Stack<MethodFrame> frames = new ();
    private MethodFrame rootFrame, currentFrame;
    private string fileName = string.Empty;
    private MissingReferenceAction misRefAct = MissingReferenceAction.Fail;
    private JumpCode jumpCode = JumpCode.None;
    private LinkedList<DataItem> yieldedValues = [];
    private DataItem returnedValue;
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
    public ScriptContext InitialContext { get; }

    /// <summary>
    /// Gets or sets the value returned by the last evaluated expression.
    /// </summary>
    public DataItem ReturnedValue => returnedValue;

    #endregion

    #region Members of ITranslator

    public void TranslateProgram(Program program)
    {
        string prevFileName = fileName;
        fileName = program.FileName;

        foreach (var pair in program.Labels)
            RegisterLabel(pair.Key, pair.Value);

        try
        {
            int address = 0;

            while (address < program.Statements.Length)
            {
                program.Statements[address].AcceptTranslator(this);
                address = NextAddress(address, program.Labels, false, false);
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
                    throw new RuntimeError(fileName, import, string.Format(Resources.ModuleNotFound, import.ModuleName));
            }
            else if (!ImportNamespace(import.ModuleName, import.Alias))
                throw new RuntimeError(fileName, import, string.Format(Resources.UndefinedType, import.ModuleName));
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, import, ex);
        }
    }

    public void TranslateClassDefinition(ClassDefinition classDef)
    {
        if (rootFrame.RootBlock.GetItem(classDef.ClassName) != null)
            throw new RuntimeError(fileName, classDef, string.Format(Resources.NameConflict, classDef.ClassName));

        Class superClass = Class.Object;
        if (!string.IsNullOrEmpty(classDef.SuperClassName))
        {
            superClass = rootFrame.GetItem(classDef.SuperClassName) as Class ??
                throw new RuntimeError(fileName, classDef, string.Format(Resources.UndefinedType, classDef.SuperClassName));
        }

        switch (superClass.Modifier)
        {
            case Modifier.Final or Modifier.Static:
                throw new RuntimeError(fileName, classDef, string.Format(Resources.CannotCreateSubclass, classDef.SuperClassName));
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
                            throw new RuntimeError(fileName, classDef,
                                string.Format(Resources.MustOverride, classDef.ClassName, member.FullName));

                        if (_override.Modifier is Modifier.Abstract or Modifier.Static)
                            throw new ScriptError(fileName, _override, string.Format(Resources.InvalidMemberModifier, _override.Name));
                    }
                }
                break;
        }

        foreach (ClassFieldDecl field in classDef.Fields)
        {
            ClassField superField = superClass.GetField(field.Name);
            if (superField != null)
                throw new ScriptError(fileName, field,
                    string.Format(Resources.FieldDeclaredInAncestor, field.Name, superField.Holder.Name));

            if (superClass.GetMember(field.Name, MemberKind.All & ~MemberKind.Field) != null)
                throw new ScriptError(fileName, field, string.Format(Resources.FieldHidesHomonymous, field.Name));
        }

        foreach (ClassPropertyDecl property in classDef.Properties)
        {
            if (superClass.GetMember(property.Name, MemberKind.All & ~MemberKind.Property) != null)
                throw new ScriptError(fileName, property, string.Format(Resources.PropertyHidesHomonymous, property.Name));

            ClassProperty overriden = superClass.GetProperty(property.Name);
            if (overriden != null)
            {
                if (overriden.Modifier is Modifier.Final or Modifier.Static)
                    throw new ScriptError(fileName, property, string.Format(Resources.MemberCantOverride, overriden.FullName));

                if (!property.MatchesSignature(overriden))
                    throw new ScriptError(fileName, property, string.Format(Resources.MustMatchSignature,
                                                                                property.Name, overriden.FullName));
            }
        }

        foreach (ClassMethodDecl method in classDef.Methods)
        {
            if (superClass.GetMember(method.Name, MemberKind.All & ~MemberKind.Method) != null)
                throw new ScriptError(fileName, method, string.Format(Resources.MethodHidesHomonymous, method.Name));

            ClassMethod overriden = superClass.GetMethod(method.Name);
            if (overriden != null)
            {
                if (overriden.Modifier is Modifier.Final or Modifier.Static)
                    throw new ScriptError(fileName, method, string.Format(Resources.MemberCantOverride, overriden.FullName));

                //Note: I'm not sure it's wise to check this!
                if (!method.MatchesSignature(overriden))
                    throw new ScriptError(fileName, method,
                        string.Format(Resources.MustMatchSignature, method.Name, overriden.FullName));
            }
        }

        foreach (ClassEventDecl _event in classDef.Events)
        {
            ClassEvent superEvent = superClass.GetEvent(_event.Name);
            if (superEvent != null)
                throw new ScriptError(fileName, _event,
                    string.Format(Resources.EventDeclaredInAncestor, _event.Name, superEvent.Holder.Name));

            if (superClass.GetMember(_event.Name, MemberKind.All & ~MemberKind.Event) != null)
                throw new ScriptError(fileName, _event, string.Format(Resources.EventHidesHomonymous, _event.Name));
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
            throw new RuntimeError(fileName, fnDecl, string.Format(Resources.NameConflict, fnDecl.Name));

        Function function = fnDecl.ToFunction();
        function.Attributes = ConvertAttributes(fnDecl.Attributes);

        for (int i = 0; i < function.Parameters.Length; ++i)
            function.Parameters[i].Attributes = ConvertAttributes(fnDecl.Parameters[i].Attributes);

        rootFrame.RootBlock.PutItem(fnDecl.Name, function);
    }

    public void TranslateExternalFunctionDecl(ExternalFunctionDecl extDecl)
    {
        if (rootFrame.RootBlock.GetItem(extDecl.Name) != null)
            throw new RuntimeError(fileName, extDecl, string.Format(Resources.NameConflict, extDecl.Name));

        const string importAttributeName = "LibImport";
        const string typeAttributeName = "Type";

        AttributeDecl importAttribute = extDecl.GetAttribute(importAttributeName) ??
            throw new RuntimeError(fileName, extDecl,
                string.Format(Resources.MissingAttribute, importAttributeName, extDecl.Name));

        PropertyInitializer libNameProperty = importAttribute.GetPropertyInitializer(AttributeDecl.DEFAULT_FIELD_NAME) ??
            throw new ScriptError(fileName, importAttribute,
                string.Format(Resources.MissingAttributeProperty, AttributeDecl.DEFAULT_FIELD_NAME, importAttributeName));

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
                throw new ScriptError(fileName, returnTypeProperty,
                    string.Format(Resources.InvalidTypeReference, returnTypeName));
        }

        var paramTypes = new Type[extDecl.Parameters.Length];
        var args = new Expression[extDecl.Parameters.Length];

        for (int i = 0; i < extDecl.Parameters.Length; ++i)
        {
            ParameterDecl parameter = extDecl.Parameters[i];
            AttributeDecl typeAttribute = parameter.GetAttribute(typeAttributeName);

            if (typeAttribute == null)
                paramTypes[i] = defaultParamType;
            else
            {

                PropertyInitializer typeNameProperty = typeAttribute.GetPropertyInitializer(AttributeDecl.DEFAULT_FIELD_NAME) ??
                    throw new ScriptError(fileName, typeAttribute,
                        string.Format(Resources.MissingAttributeProperty, AttributeDecl.DEFAULT_FIELD_NAME, typeAttributeName));

                typeNameProperty.Expression.AcceptTranslator(this);
                string typeName = returnedValue.ToString();

                Type parameterType = GetTypeByName(typeName) ??
                    throw new ScriptError(fileName, typeAttribute,
                        string.Format(Resources.InvalidTypeReference, typeName));

                paramTypes[i] = parameterType;
            }

            args[i] = new VariableRef(parameter.Name);
        }

        MethodInfo method = GetPInvokeMethod(libName, procName, returnType, paramTypes);
        var extFnCall = new ExternalFunctionCall(method, args);
        var fnParams = extDecl.Parameters.Select(p => p.ToParameter()).ToArray();
        var function = new Function(fnParams, Block.WithReturn(extFnCall)); // No attribute retention
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
                        case FrameItemKind.Constant or FrameItemKind.Variable:
                            initializer.Expression.AcceptTranslator(this);
                            currentFrame.PutItem(initializer.Name, new Constant(returnedValue));
                            break;
                        default:
                            throw new ScriptError(fileName, initializer, string.Format(Resources.NameConflict, initializer.Name));
                    }
                else
                    throw new ScriptError(fileName, initializer, string.Format(Resources.NameConflict, initializer.Name));
            }
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, cstDecl, ex);
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
                    case FrameItemKind.Constant or FrameItemKind.Variable:
                        if (initializer.Expression == null)
                            returnedValue = Undefined.Value;
                        else
                            initializer.Expression.AcceptTranslator(this);

                        currentFrame.PutItem(initializer.Name, returnedValue);
                        break;
                    default:
                        throw new ScriptError(fileName, initializer, string.Format(Resources.NameConflict, initializer.Name));
                }
            else
                throw new ScriptError(fileName, initializer, string.Format(Resources.NameConflict, initializer.Name));
        }
    }

    public void TranslateBlock(Block block)
    {
        currentFrame.PushBlock();

        foreach (var pair in block.Labels)
            RegisterLabel(pair.Key, pair.Value);

        try
        {
            int address = 0;

            while (address < block.Statements.Length)
            {
                block.Statements[address].AcceptTranslator(this);
                address = NextAddress(address, block.Labels, true, false);
            }
        }
        finally
        {
            currentFrame.PopBlock();
        }
    }

    public void TranslateBlockAsExpression(BlockAsExpression blkAsExpr)
    {
        TranslateBlock(blkAsExpr.Block);
        
        if (yieldedValues.First == null)
            returnedValue = Void.Value;
        else
        {
            returnedValue = yieldedValues.First.Value;
            yieldedValues.Clear();
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

            DataItem rValue = returnedValue;
            Assign(assignment.LeftOperand, rValue);
            returnedValue = rValue;
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, assignment, ex);
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

                if (!(binExpr.Operator == BinaryOperator.Equal || binExpr.Operator == BinaryOperator.NotEqual ||
                     (binExpr.Operator == BinaryOperator.Plus && binExpr.RightOperand is Literal literal &&
                      literal.Value.Class == Class.String)))
                {
                    // Handle equality/difference check: the corresponding operators don't have to be overloaded in general
                    throw new RuntimeError(fileName, binExpr, string.Format(Resources.OperatorCantBeApplied,
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
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, binExpr, ex);
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
                    throw new RuntimeError(fileName, unExpr, string.Format(Resources.OperatorCantBeApplied,
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
                            throw new RuntimeError(fileName, unExpr, Resources.ValueShouldNotBeEmpty);
                        break;
                    default:
                        returnedValue = operand.UnaryOperation(unExpr.Operator);
                        break;
                }
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, unExpr, ex);
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
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, cplxInit, ex);
        }
    }

    public void TranslateTupleInitializer(TupleInitializer tupleInit)
    {
        (var elements, _) = ExpandList(tupleInit.Items);
        returnedValue = new Tuple([.. elements]);
    }

    public void TranslateListInitializer(ListInitializer listInit)
    {
        (var elements, _) = ExpandList(listInit.Items);
        returnedValue = new List(elements);
    }

    public void TranslateSetInitializer(SetInitializer setInit)
    {
        var (elements, _) = ExpandList(setInit.Items);
        returnedValue = new Set(elements);
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
            catch (ScriptError)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScriptError(fileName, item, ex);
            }
        }

        returnedValue = new Map(dict);
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
            catch (ScriptError)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScriptError(fileName, propInit, ex);
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
                    throw new RuntimeError(fileName, varRef, string.Format(Resources.UndefinedVariable, varRef.Name));
            }
        else
            switch (frameItem.Kind)
            {
                case FrameItemKind.Variable:
                    {
                        var variable = (DataItem)frameItem;
                        if (variable == Undefined.Value && misRefAct == MissingReferenceAction.Fail)
                            throw new RuntimeError(fileName, varRef, string.Format(Resources.UninitializedVariable, varRef.Name));
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
                    throw new RuntimeError(fileName, varRef, string.Format(Resources.NotAVariable, varRef.Name));
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
                    throw new RuntimeError(fileName, itemRef, Resources.CannotReadProperty);

                CheckAccess(indexer.Reader, itemRef);
                Invoke(indexer.Reader.Function, indexer.Name, indexer.Holder, owner, new Literal(index));
                return;
            }

            if (itemValue == null && misRefAct != MissingReferenceAction.Ignore)
                throw new RuntimeError(fileName, itemRef, string.Format(Resources.IndexNotFound, index));

            returnedValue = itemValue;
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, itemRef, ex);
        }
    }

    public void TranslateSliceRef(SliceRef sliceRef)
    {
        try
        {
            ResolveSliceRef(sliceRef, out DataItem owner, out int lBound, out int uBound);
            returnedValue = owner.GetItemRange(lBound, uBound);
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, sliceRef, ex);
        }
    }

    public void TranslatePropertyRef(PropertyRef propertyRef)
    {
        try
        {
            DataItem propValue;

            ResolvePropertyRef(propertyRef, out DataItem owner, out ClassMember member);

            switch (owner)
            {
                case null:
                    propValue = null;
                    break;
                case Void when propertyRef.Optional:
                    propValue = Void.Value;
                    break;
                default:
                    switch (member)
                    {
                        case null:
                            propValue = owner.GetProperty(propertyRef.PropertyName);
                            break;
                        case ClassField field:
                            propValue = field.IsStatic ? field.SharedValue : owner.GetProperty(field.Name);
                            break;
                        case ClassMethod method:
                        {
                            var parameters = method.Function.Parameters;
                            var args = parameters.Select(p => new VariableRef(p.Name)).ToArray();
                            var fn = new Function(parameters, Block.WithReturn(new MethodCall(new Literal(owner), method.Name, args)));
                            propValue = new Closure(fn);
                            break;
                        }
                        default: // member is surely a ClassProperty
                        {
                            var property = (ClassProperty)member;
                            if (!property.CanRead) throw new RuntimeError(fileName, propertyRef, Resources.CannotReadProperty);

                            CheckAccess(property.Reader, propertyRef);
                            Invoke(property.Reader.Function, property.Name, property.Holder, owner);
                            return;
                        }
                    }
                    break;

            }

            if (propValue == null && misRefAct != MissingReferenceAction.Ignore)
                throw new RuntimeError(fileName, propertyRef, string.Format(Resources.PropertyNotFoundInObject, propertyRef.PropertyName));

            returnedValue = propValue;
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, propertyRef, ex);
        }
    }

    public void TranslateStaticPropertyRef(StaticPropertyRef staticRef)
    {
        try
        {
            switch (ResolveName(staticRef.Name, staticRef))
            {
                case ClassField field:
                    returnedValue = field.SharedValue;
                    break;
                case ClassProperty property:
                    if (!property.CanRead) throw new RuntimeError(fileName, staticRef, Resources.CannotReadProperty);

                    CheckAccess(property.Reader, staticRef);
                    Invoke(property.Reader.Function, property.Name, property.Holder, null);
                    break;
                case ClassMethod method:
                    returnedValue = new Closure(method.Function);
                    break;
                case StaticTypeMember member:
                    returnedValue = member.GetValue();
                    break;
                default:
                    throw new RuntimeError(fileName, staticRef, string.Format(Resources.UnresolvedMemberRef, staticRef.Name));
            }
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, staticRef, ex);
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
                throw new RuntimeError(fileName, fnCall, string.Format(Resources.UndefinedFunction, fnCall.FunctionName));

            Invoke(function, fnCall.FunctionName, ctx.MethodHolder, ctx.MethodTarget, fnCall.Arguments, fnCall.NamedArgs);
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, fnCall, ex);
        }
    }

    public void TranslateAnonymousCall(AnonymousCall anCall)
    {
        try
        {
            anCall.Callee.AcceptTranslator(this);

            DataItem target = returnedValue;
            if (target is not Closure) throw new RuntimeError(fileName, anCall.Callee, Resources.CalleeIsNotClosure);

            Function function = target.AsFunction;
            InvocationContext ctx = function.DeclaringFrame != null ? function.DeclaringFrame.Context : currentFrame.Context;
            Invoke(function, anCall.FunctionName, ctx.MethodHolder, ctx.MethodTarget, anCall.Arguments, anCall.NamedArgs);
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, anCall, ex);
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

                        DataItem fieldValue = field != null && field.IsStatic
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
                throw new RuntimeError(fileName, methodCall,
                    string.Format(Resources.MethodNotFound, methodCall.FunctionName, methodTarget.Class.Name));

            Invoke(function, methodCall.FunctionName, methodHolder, methodTarget, methodCall.Arguments, methodCall.NamedArgs);
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, methodCall, ex);
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
                if (field.SharedValue is Closure) function = field.SharedValue.AsFunction;

                if (function == null)
                    throw new RuntimeError(fileName, staticCall, string.Format(Resources.MethodNotFound, field.Name, staticCall.Name));

                Invoke(function, field.Name, currentFrame.Context.MethodHolder, null, staticCall.Arguments, staticCall.NamedArgs);
            }
            else if (targetMethod is StaticTypeMember member)
                InvokeNative(member.Type, member.MemberName, null, staticCall.Arguments);
            else
                throw new RuntimeError(fileName, staticCall, string.Format(Resources.UnresolvedMemberRef, staticCall.Name));
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, staticCall, ex);
        }
    }

    public void TranslateConstructorCall(ConstructorCall ctorCall)
    {
        try
        {
            switch (ResolveName(ctorCall.Name, ctorCall))
            {
                case Class klass:
                {
                    if (klass.Modifier is Modifier.Abstract or Modifier.Static)
                        throw new RuntimeError(fileName, ctorCall, string.Format(Resources.CannotCreateInstance, klass.Name));

                    ClassMethod constructor = klass.Constructor;
                    CheckAccess(constructor, ctorCall);

                    DataItem inst = new Object(klass);
                    InitializeFields(inst);
                    Invoke(constructor.Function, constructor.Name, klass, inst, ctorCall.Arguments, ctorCall.NamedArgs);

                    if (ctorCall.PropertyInitializers != null)
                        ApplyPropertyInitializers(ctorCall, inst, ctorCall.PropertyInitializers);

                    returnedValue = inst;
                    break;
                }
                case Type type:
                {
                    (DataItem[] args, _) = ExpandList(ctorCall.Arguments ?? []);
                    object obj = Reflector.CreateInstance(type, args);
                    DataItem inst = DataItemFactory.CreateDataItem(obj);

                    if (ctorCall.PropertyInitializers != null)
                        foreach (PropertyInitializer initializer in ctorCall.PropertyInitializers)
                        {
                            initializer.Expression.AcceptTranslator(this);
                            inst.SetProperty(initializer.Name, returnedValue);
                        }

                    returnedValue = inst;
                    break;
                }
                default:
                    throw new RuntimeError(fileName, ctorCall, string.Format(Resources.UndefinedType, ctorCall.Name));
            }
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, ctorCall, ex);
        }
    }

    public void TranslateParentMethodCall(ParentMethodCall pmc)
    {
        try
        {
            DataItem _this = currentFrame.Context.MethodTarget;
            ClassMethod method = _this.Class.SuperClass.GetMethod(pmc.FunctionName) ??
                throw new RuntimeError(fileName, pmc, string.Format(Resources.MethodNotFound,
                                                                    pmc.FunctionName, _this.Class.SuperClass.Name));
            
            if (method.Modifier == Modifier.Abstract)
                throw new RuntimeError(fileName, pmc, string.Format(Resources.CannotInvokeAbstractMember, method.FullName));
            
            CheckAccess(method, pmc);
            Invoke(method.Function, pmc.FunctionName, method.Holder, _this, pmc.Arguments, pmc.NamedArgs);
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, pmc, ex);
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
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, pcc, ex);
        }
    }

    public void TranslateParentPropertyRef(ParentPropertyRef ppr)
    {
        try
        {
            Class superClass = currentFrame.Context.MethodHolder.SuperClass;
            ClassMember member = superClass.GetMember(ppr.PropertyName, MemberKind.Property | MemberKind.Method) ??
                throw new RuntimeError(fileName, ppr, string.Format(Resources.PropertyNotFoundInClass,
                                                                    ppr.PropertyName, superClass.Name));

            if (member.Modifier == Modifier.Abstract)
                throw new RuntimeError(fileName, ppr, string.Format(Resources.CannotInvokeAbstractMember, member.FullName));

            CheckAccess(member, ppr);

            if (member is ClassProperty property)
            {
                if (!property.CanRead) throw new RuntimeError(fileName, ppr, Resources.CannotReadProperty);

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
                var function = new Function(parameters, Block.WithReturn(new MethodCall(new Literal(newTarget), member.Name, args)));
                returnedValue = new Closure(function);
            }
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, ppr, ex);
        }
    }

    public void TranslateParentIndexerRef(ParentIndexerRef pir)
    {
        try
        {
            Class superClass = currentFrame.Context.MethodHolder.SuperClass;
            ClassProperty indexer = superClass.Indexer ??
                throw new RuntimeError(fileName, pir, string.Format(Resources.ClassHasNoIndexReader, superClass.Name));

            if (indexer.Modifier == Modifier.Abstract)
                throw new RuntimeError(fileName, pir, string.Format(Resources.CannotInvokeAbstractMember, indexer.FullName));

            if (!indexer.CanRead)
                throw new RuntimeError(fileName, pir, Resources.CannotReadProperty);

            CheckAccess(indexer, pir);
            Invoke(indexer.Reader.Function, indexer.Name, indexer.Holder, currentFrame.Context.MethodTarget, pir.Index);
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, pir, ex);
        }
    }

    public void TranslateInnerFunctionCall(InnerFunctionCall innerCall)
    {
        var arguments = new DataItem[innerCall.Arguments.Length];

        for (int i = 0; i < arguments.Length; ++i)
        {
            innerCall.Arguments[i].Expression.AcceptTranslator(this);
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
            extCall.Arguments[i].Expression.AcceptTranslator(this);
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
                throw new RuntimeError(fileName, typeVerif, string.Format(Resources.UndefinedType, typeVerif.TypeName));

            misRefAct = MissingReferenceAction.Ignore;
            typeVerif.Expression.AcceptTranslator(this);
            DataItem retVal = returnedValue;
            returnedValue = Boolean.FromBool((retVal == null && klass == Class.Void) || retVal.InstanceOf(klass));
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, typeVerif, ex);
        }
        finally
        {
            misRefAct = prevAction;
        }
    }

    public void TranslateTypeOfExpression(TypeOfExpression typeOf)
    {
        if (rootFrame.GetItem(typeOf.TypeName) is not Class klass)
            throw new RuntimeError(fileName, typeOf, string.Format(Resources.UndefinedType, typeOf.TypeName));

        returnedValue = GetTypeInfo(klass);
    }

    public void TranslateConversion(Conversion conversion)
    {
        try
        {
            conversion.Expression.AcceptTranslator(this);
            DataItem converted = returnedValue;

            if (converted.Class.Inherits(Class.Object))
                throw new RuntimeError(fileName, conversion.Expression, string.Format(Resources.CannotConvertFrom, converted.Class.Name));

            if (rootFrame.GetItem(conversion.TypeName) is not Class klass)
                throw new RuntimeError(fileName, conversion, string.Format(Resources.UndefinedType, conversion.TypeName));
            
            if (klass.ClassID < ClassID.Boolean || klass.ClassID > ClassID.Object)
                throw new RuntimeError(fileName, conversion, string.Format(Resources.CannotConvertTo, conversion.TypeName));

            returnedValue = converted.ConvertTo(klass);
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, conversion, ex);
        }
    }

    public void TranslateIfElse(IfElse ifElse)
    {
        if (IsTrue(ifElse.Test))
            ifElse.Action.AcceptTranslator(this);
        else
            ifElse.AlternativeAction?.AcceptTranslator(this);
    }

    public void TranslateSwitchBlock(SwitchBlock switchBlock)
    {
        switchBlock.Test.AcceptTranslator(this);
        int hashCode = returnedValue.GetHashCode();

        int address = switchBlock.DefaultCase;
        foreach (CaseLabel caseLabel in switchBlock.Cases)
            if (caseLabel.GetHashCode() == hashCode)
            {
                address = caseLabel.Address;
                break;
            }

        currentFrame.PushBlock();

        foreach (KeyValuePair<string, Label> pair in switchBlock.Labels)
            RegisterLabel(pair.Key, pair.Value);

        try
        {
            while (address < switchBlock.Statements.Length)
            {
                switchBlock.Statements[address].AcceptTranslator(this);
                address = NextAddress(address, switchBlock.Labels, true, true);
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

        Expression test = forLoop.Test ?? new Literal(Boolean.True);

        while (IsTrue(test))
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
                case JumpCode.Goto or JumpCode.Return:
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

        foreach ((DataItem key, DataItem value) in GetEnumerable(forEach.Test))
        {
            RegisterVariable(forEach.KeyName, key);
            RegisterVariable(forEach.ValueName, value);

            forEach.Action.AcceptTranslator(this);

            switch (jumpCode)
            {
                case JumpCode.Continue:
                    jumpCode = JumpCode.None;
                    break;
                case JumpCode.Break:
                    jumpCode = JumpCode.None;
                    goto EXIT;
                case JumpCode.Goto or JumpCode.Return:
                    goto EXIT;
            }
        }

    EXIT:
        currentFrame.PopBlock();
    }

    public void TranslateWhileLoop(WhileLoop whileLoop)
    {
        while (IsTrue(whileLoop.Test))
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
                case JumpCode.Goto or JumpCode.Return:
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
                case JumpCode.Goto or JumpCode.Return:
                    return;
            }
        } while (IsTrue(doLoop.Test));
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

    public void TranslateYield(Yield yield)
    {
        yield.Expression.AcceptTranslator(this);
        yieldedValues.AddLast(returnedValue);
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
            throw new RuntimeError(fileName, _throw, thrown);

        throw new RuntimeError(fileName, _throw, thrown.ToString());
    }

    public void TranslateTryCatchFinally(TryCatchFinally tcf)
    {
        DataItem resource = null;
        ScriptError finalException = null;
        
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
        catch (ScriptError ex1)
        {
            if (tcf.CatchBlock == null)
                finalException = ex1;
            else
                try
                {
                    currentFrame.PutItem(tcf.ExceptionName, ConvertException(ex1));
                    tcf.CatchBlock.AcceptTranslator(this);
                }
                catch (ScriptError ex2)
                {
                    finalException = ex2;
                }
        }
        finally
        {
            if (tcf.FinallyBlock != null)
            {
                var (prevRetValue, prevJumpCode, prevGoto) = (returnedValue, jumpCode, lastGoto);

                try
                {
                    jumpCode = JumpCode.None;
                    tcf.FinallyBlock.AcceptTranslator(this);
                    if (jumpCode == JumpCode.Goto)
                        finalException = new RuntimeError(fileName, lastGoto, Resources.CannotJumpOutOfFinallyBlock);
                }
                catch (ScriptError ex3)
                {
                    finalException = ex3;
                }
                finally
                {
                    // Any return or jump within the finally block is ignored
                    (returnedValue, jumpCode, lastGoto) = (prevRetValue, prevJumpCode, prevGoto);
                }
            }

            resource?.Dispose();
            currentFrame.PopBlock();
        }
        
        if (finalException != null) throw finalException;
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
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, stringInt, ex);
        }
    }

    public void TranslatePatternMatching(PatternMatching patMatch)
    {
        try
        {
            patMatch.Expression.AcceptTranslator(this);

            var testArg = new Literal(returnedValue);
            var frameItems = new Dictionary<string, IFrameItem>
            {
                [ClassProperty.WRITER_PARAMETER_NAME] = testArg.Value,
            };

            currentFrame.PushBlock(frameItems);

            try
            {
                foreach (MatchCase matchCase in patMatch.MatchCases)
                    if (IsTrue(matchCase.Pattern.GetMatchTest(testArg)) &&
                        (matchCase.Guard == null || IsTrue(matchCase.Guard)))
                    {
                        matchCase.Expression.AcceptTranslator(this);
                        return;
                    }
            }
            finally
            {
                currentFrame.PopBlock();
            }

            returnedValue = Void.Value;
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, patMatch, ex);
        }
    }

    public void TranslateAlteredCopy(AlteredCopy altCopy)
    {
        try
        {
            altCopy.Original.AcceptTranslator(this);

            DataItem original = returnedValue;
            if (original.Class.ClassID != ClassID.Object)
                throw new RuntimeError(fileName, altCopy, Resources.InvalidOperandForWith);

            var copyFields = new Dictionary<string, DataItem>();

            foreach (var originalField in original.AsDynamicObject)
                copyFields.Add(originalField.Key, originalField.Value);

            DataItem copy = new Object(original.Class, copyFields);
            ApplyPropertyInitializers(altCopy, copy, altCopy.PropertyInitializers);

            returnedValue = copy;
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, altCopy, ex);
        }
    }

    #endregion

    #region Members of IAssignmentProcessor

    public void AssignToVariable(VariableRef varRef, DataItem rValue)
    {
        RegisterVariable(varRef.Name, rValue);
    }

    public void AssignToItem(ItemRef itemRef, DataItem rValue)
    {
        ResolveItemRef(itemRef, out DataItem owner, out DataItem index, out ClassProperty indexer);

        if (indexer == null)
            owner.SetItem(index, rValue);
        else
        {
            if (!indexer.CanWrite)
                throw new RuntimeError(fileName, itemRef, Resources.CannotWriteProperty);

            CheckAccess(indexer.Writer, itemRef);
            Invoke(indexer.Writer.Function, indexer.Name, indexer.Holder, owner, new Literal(index), new Literal(rValue));
        }
    }

    public void AssignToSlice(SliceRef sliceRef, DataItem rValue)
    {
        ResolveSliceRef(sliceRef, out DataItem owner, out int lBound, out int uBound);
        owner.SetItemRange(lBound, uBound, rValue);
    }

    public void AssignToProperty(PropertyRef propertyRef, DataItem rValue)
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
                        throw new RuntimeError(fileName, propertyRef, Resources.CannotWriteFinalField);
                    break;
                default: // StaticFinal
                    throw new RuntimeError(fileName, propertyRef, Resources.CannotWriteFinalField);
            }
        else if (member is ClassProperty property)
        {
            if (!property.CanWrite) throw new RuntimeError(fileName, propertyRef, Resources.CannotWriteProperty);

            CheckAccess(property.Writer, propertyRef);
            Invoke(property.Writer.Function, property.Name, property.Holder, owner, new Literal(rValue));
        }
        else
            throw new RuntimeError(fileName, propertyRef, Resources.InvalidLValue);
    }

    public void AssignToStaticProperty(StaticPropertyRef staticRef, DataItem rValue)
    {
        object targetProperty = ResolveName(staticRef.Name, staticRef);

        if (targetProperty is ClassField field)
        {
            if (field.Modifier == Modifier.Static)
                field.SharedValue = rValue;
            else // Static + Final
                throw new RuntimeError(fileName, staticRef, Resources.CannotWriteFinalField);
        }
        else if (targetProperty is ClassProperty property)
        {
            if (!property.CanWrite) throw new RuntimeError(fileName, staticRef, Resources.CannotWriteProperty);

            CheckAccess(property.Writer, staticRef);
            Invoke(property.Writer.Function, property.Name, property.Holder, null, new Literal(rValue));
        }
        else if (targetProperty is StaticTypeMember member)
            member.SetValue(rValue);
        else
            throw new RuntimeError(fileName, staticRef, string.Format(Resources.UnresolvedMemberRef, staticRef.Name));
    }

    public void AssignToParentItem(ParentIndexerRef pir, DataItem rValue)
    {
        Class superClass = currentFrame.Context.MethodHolder.SuperClass;
        ClassProperty indexer = superClass.Indexer ??
            throw new RuntimeError(fileName, pir, string.Format(Resources.ClassHasNoIndexWriter, superClass.Name));

        if (indexer.Modifier == Modifier.Abstract)
            throw new RuntimeError(fileName, pir, string.Format(Resources.CannotInvokeAbstractMember, indexer.FullName));

        if (!indexer.CanWrite)
            throw new RuntimeError(fileName, pir, Resources.CannotWriteProperty);

        CheckAccess(indexer, pir);
        Invoke(indexer.Writer.Function, indexer.Name, indexer.Holder, currentFrame.Context.MethodTarget, pir.Index, new Literal(rValue));
    }

    public void AssignToParentProperty(ParentPropertyRef ppr, DataItem rValue)
    {
        Class superClass = currentFrame.Context.MethodHolder.SuperClass;
        ClassProperty property = superClass.GetProperty(ppr.PropertyName) ??
            throw new RuntimeError(fileName, ppr, string.Format(Resources.PropertyNotFoundInClass, ppr.PropertyName, superClass.Name));

        if (property.Modifier == Modifier.Abstract)
            throw new RuntimeError(fileName, ppr, string.Format(Resources.CannotInvokeAbstractMember, property.FullName));

        if (!property.CanWrite)
            throw new RuntimeError(fileName, ppr, Resources.CannotWriteProperty);

        CheckAccess(property, ppr);
        Invoke(property.Writer.Function, property.Name, property.Holder, currentFrame.Context.MethodTarget, new Literal(rValue));
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
        if (frameItem != null) throw new ScriptError(fileName, label, string.Format(Resources.NameConflict, name));
        
        currentFrame.PutItem(name, label);
    }

    /// <summary>
    /// Gets the initial frame items for a call.
    /// </summary>
    /// <param name="function">The function that is being called</param>
    /// <param name="functionName">The function's name</param>
    /// <param name="positionalArgs">The list of positional arguments passed to the function</param>
    /// <param name="namedArgs">The collection of named arguments passed to the function</param>
    /// <returns>A <see cref="(Dictionary<string, IFrameItem>, List<Expression>)"/> tuple</returns>
    private (Dictionary<string, IFrameItem>, List<Expression>)
        GetInitialFrameItems(Function function, string functionName, ListItem[] positionalArgs,
                             Dictionary<string, Expression> namedArgs)
    {
        // Make sure we are not dealing with null references
        positionalArgs ??= [];
        namedArgs ??= [];

        // Expand the list of positional arguments
        var (argValues, argItems) = ExpandList(positionalArgs);
        int totalArgCount = argValues.Length;

        // Check that every named argument matches a parameter declared in the function's header
        // Also compute the total argument count
        foreach (string argName in namedArgs.Keys)
        {
            int paramIndex = Array.FindIndex(function.Parameters, p => p.Name == argName);

            if (paramIndex < 0)
                throw new ArgumentException(string.Format(Resources.FunctionHasNoParameterNamed, functionName, argName));

            if (paramIndex < argValues.Length)
                throw new ArgumentException(string.Format(Resources.ParameterSuppliedTwice, argName));

            ++totalArgCount;
        }

        // Check the minimum number of arguments
        int minNumArgs = function.MinNumArgs;
        if (totalArgCount < minNumArgs)
            throw new InvalidProgramException(string.Format(Resources.TooFewArgs, functionName));

        // Check the maximum number of arguments
        if (totalArgCount > function.MaxNumArgs)
            throw new InvalidProgramException(string.Format(Resources.TooManyArgs, functionName));

        Dictionary<string, IFrameItem> frameItems = [];
        int counter = 0;

        // Pass the positional arguments first
        while (counter < argValues.Length)
        {
            Parameter parameter = function.Parameters[counter];
            ListItem argument = argItems[counter];
            DataItem argValue = argValues[counter];

            // Check that parameters are not passed by reference from expressions with the spread operator
            if (parameter.ByRef && argument.Spread)
                throw new ScriptError(fileName, argument, Resources.InvalidLValue);

            if (parameter.VaList)
            {
                // If the current parameter is a variably sized list,
                // fill it with the remaining arguments
                var vaList = new List(argValues[counter..]);

                CheckEmptiness(parameter, vaList, argument);
                frameItems.Add(parameter.Name, vaList);
                counter = int.MaxValue;
            }
            else
            {
                // Otherwise, set the value provided to the parameter
                CheckEmptiness(parameter, argValue, argument);
                frameItems.Add(parameter.Name, argValue);
                ++counter;
            }
        }

        List<Expression> expandedArgList = argItems
            .Select(argument => argument.Expression)
            .ToList();

        // Then finish with the named arguments and optional parameters default values
        while (counter < function.Parameters.Length)
        {
            Parameter parameter = function.Parameters[counter];

            if (namedArgs.TryGetValue(parameter.Name, out Expression argument))
            {
                argument.AcceptTranslator(this);
                DataItem argValue = returnedValue;

                CheckEmptiness(parameter, argValue, argument);
                frameItems.Add(parameter.Name, argValue);
                expandedArgList.Add(argument);
            }
            else if (counter >= minNumArgs)
                frameItems.Add(parameter.Name, parameter.VaList ? new List() : parameter.DefaultValue);
            else
                throw new ArgumentException(string.Format(Resources.MissingPameter, parameter.Name, functionName));

            ++counter;
        }

        // For inline functions, import the declaring function's local constants and variables
        foreach (var pair in function.CapturedItems)
            if (!frameItems.ContainsKey(pair.Key)) frameItems.Add(pair.Key, pair.Value);

        return (frameItems, expandedArgList);
    }

    /// <summary>
    /// Checks that a parameter that cannot be empty is not actually supplied an empty value.
    /// </summary>
    /// <param name="parameter">The parameter that's receiving a value</param>
    /// <param name="argValue">The value that's being given to the parameter</param>
    /// <param name="argument">The source code element that was evaluated to <paramref name="argValue"/></param>
    /// <exception cref="ScriptError">If <paramref name="argValue"/> is an empty value</exception>
    private void CheckEmptiness(Parameter parameter, DataItem argValue, ScriptElement argument)
    {
        if (!parameter.CanBeEmpty && argValue.IsEmpty())
            throw new ScriptError(fileName, argument, Resources.ValueShouldNotBeEmpty);
    }

    /// <summary>
    /// Copies back the modified value of each <i>byref</i> parameter upon function's completion.<br/>
    /// For inline functions, also updates the variables imported from the declaring function's context.
    /// </summary>
    /// <param name="function">The function for which values are copied back</param>
    /// <param name="arguments">The real arguments of the function</param>
    /// <param name="frameItems">The captured frame items to copy back</param>
    private void CopyBackFrameItems(Function function, List<Expression> arguments,
                                    Dictionary<string, IFrameItem> frameItems)
    {
        HashSet<string> namesToSkip = [];

        for (int i = 0; i < function.Parameters.Length; ++i)
        {
            Parameter parameter = function.Parameters[i];
            namesToSkip.Add(parameter.Name);

            if (parameter.ByRef)
                Assign(arguments[i], (DataItem)frameItems[parameter.Name]);
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
                        ListItem[] positionalArgs, Dictionary<string, Expression> namedArgs)
    {
        var (frameItems, expandedArgList) = GetInitialFrameItems(function, name, positionalArgs, namedArgs);
        PushFrame(holder, target, name, frameItems);

        try
        {
            function.Body.AcceptTranslator(this);

            if (jumpCode == JumpCode.Goto)
                throw new RuntimeError(fileName, lastGoto, string.Format(Resources.MissingLabel, lastGoto.LabelName));
        }
        finally
        {
            PopFrame();

            DataItem result = returnedValue;
            CopyBackFrameItems(function, expandedArgList, frameItems);
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
        Invoke(function, name, holder, target, Call.ToListItems(arguments), null);
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
            case UnaryOperator.PostIncrement or UnaryOperator.PostDecrement:
                return postfix = true;
            case UnaryOperator.None or UnaryOperator.Not:
                return postfix = false;
            case UnaryOperator.NotEmpty:
                postfix = true;
                return false;
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
    private static bool IsOverloadable(BinaryOperator _operator) => _operator is not
        (
            BinaryOperator.None or BinaryOperator.AndAlso or BinaryOperator.OrElse or
            BinaryOperator.Identical or BinaryOperator.NotIdentical or BinaryOperator.IfEmpty
        );

    /// <summary>
    /// Initializes the static fields of a class.
    /// </summary>
    /// <param name="klass">A class</param>
    private void InitializeFields(Class klass)
    {
        foreach (ClassField field in klass.Fields)
            if (field.IsStatic && field.Initializer != null)
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
                if (!field.IsStatic)
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
        InvocationContext ctx = currentFrame.Context;
        
        bool violation = member.Scope switch
        {
            Scope.Private => ctx.MethodHolder == null || ctx.MethodHolder != member.Holder,
            Scope.Protected => ctx.MethodHolder == null || (ctx.MethodHolder != member.Holder &&
                                                            !ctx.MethodHolder.Inherits(member.Holder)),
            _ => false
        };

        if (violation)
            throw new RuntimeError(fileName, astNode, string.Format(
                Resources.AccessDenied, member.FullName, ctx.MethodHolder?.Name ?? "public"));
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
                    throw new ScriptError(fileName, initializer, Resources.CannotWriteFinalField);
            }

            initializer.Expression.AcceptTranslator(this);
            DataItem propValue = returnedValue;

            if (member is ClassProperty property)
            {
                if (!property.CanWrite)
                    throw new ScriptError(fileName, initializer, Resources.CannotWriteProperty);

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
    private DataItem ConvertException(ScriptError sx)
    {
        if (sx is RuntimeError { Thrown: not null } rx)
            return rx.Thrown;
        
        var ex = new Object(Class.Exception);
        InitializeFields(ex);
        
        if (sx.InnerException is { } inex)
            ex.SetProperty("__name", new String(inex.GetType().Name));
        else
            ex.SetProperty("__name", new String(Class.Exception.Name));

        ex.SetProperty("__message", new String(sx.Message));
        ex.SetProperty("__source", new String(fileName));
        ex.SetProperty("__line", new Integer(sx.Element.Start.LineNumber));

        return ex;
    }

    /// <summary>
    /// Converts declared attributes to runtime attributes.
    /// </summary>
    /// <param name="attributes">The set of declared attributes</param>
    /// <returns>An array of <see cref="DataItem"/></returns>
    private DataItem[] ConvertAttributes(AttributeDecl[] attributes) =>
        attributes?.Select(ConvertAttribute).ToArray();

    /// <summary>
    /// Converts a declared attribute to a runtime attribute.
    /// </summary>
    /// <param name="attribute">The declared attribute</param>
    /// <returns>A <see cref="DataItem"/></returns>
    private DataItem ConvertAttribute(AttributeDecl attribute)
    {
        var ctorCall = new ConstructorCall(new QualifiedName(Class.Attribute.Name),
                                           [new (new Literal(new String(attribute.Name)))],
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
        if (typeInfoCache.TryGetValue(klass, out DataItem cached))
            return cached;
        
        var typeInfo = new Object(Class.TypeInfo);
        DataItem superType = klass.SuperClass != null ? new String(klass.SuperClass.Name) : Void.Value;
        DataItem indexerInfo = klass.Indexer != null ? GetPropertyInfo(klass.Indexer) : Void.Value;

        InitializeFields(typeInfo);
        typeInfo.SetProperty("__superType", superType);
        typeInfo.SetProperty("__modifier", new String(klass.Modifier.ToString()));
        typeInfo.SetProperty("__name", new String(klass.Name));
        typeInfo.SetProperty("__constructor", GetMethodInfo(klass.Constructor));
        typeInfo.SetProperty("__indexer", indexerInfo);
        typeInfo.SetProperty("__fields", GetFieldInfoMap(klass));
        typeInfo.SetProperty("__properties", GetPropertyInfoMap(klass));
        typeInfo.SetProperty("__methods", GetMethodInfoMap(klass));
        typeInfo.SetProperty("__events", GetEventInfoMap(klass));
        typeInfo.SetProperty("__attributes", GetAttributeList(klass.Attributes));
        typeInfoCache.Add(klass, typeInfo);
        
        return typeInfo;
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
        memberInfo.SetProperty("__scope", new String(member.Scope.ToString()));
        memberInfo.SetProperty("__modifier", new String(member.Modifier.ToString()));
        memberInfo.SetProperty("__name", new String(member.Name));
        memberInfo.SetProperty("__holder", new String(member.Holder.Name));
        memberInfo.SetProperty("__attributes", GetAttributeList(member.Attributes));

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
        fieldInfo.SetProperty("__sharedValue", field.SharedValue ?? Void.Value);
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
        propertyInfo.SetProperty("__reader", property.CanRead ? GetMethodInfo(property.Reader) : Void.Value);
        propertyInfo.SetProperty("__writer", property.CanWrite ? GetMethodInfo(property.Writer) : Void.Value);
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
        methodInfo.SetProperty("__parameters", GetParameterInfoMap(method.Function.Parameters));
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
        _eventInfo.SetProperty("__parameters", GetParameterInfoMap(_event.Parameters));
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
        parameterInfo.SetProperty("__name", new String(parameter.Name));
        parameterInfo.SetProperty("__byRef", Boolean.FromBool(parameter.ByRef));
        parameterInfo.SetProperty("__vaList", Boolean.FromBool(parameter.VaList));
        parameterInfo.SetProperty("__defaultValue", parameter.DefaultValue ?? Void.Value);
        parameterInfo.SetProperty("__canBeEmpty", Boolean.FromBool(parameter.CanBeEmpty));
        parameterInfo.SetProperty("__attributes", GetAttributeList(parameter.Attributes));

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
    private DataItem GetAttributeList(DataItem[] attributes) =>
        attributes != null ? new List(attributes) : new List();

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
    private void InvokeNative(Type type, string methodName, object target, ListItem[] arguments)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                   BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

        arguments ??= [];

        (DataItem[] argValues, ListItem[] argItems) = ExpandList(arguments);
        object[] nativeArgValues;
        object result;
        
        if (type.IsCOMObject)
        {
            nativeArgValues = argValues.Select(val => val.AsNativeObject).ToArray();
            result = type.InvokeMember(methodName, flags, null, target, nativeArgValues);
        }
        else
        {
            MethodInfo matchedMethod = DataItemBinder.FindMethod(type, methodName, argValues, flags) ??
                throw new MissingMethodException(type.FullName, methodName);

            ParameterInfo[] parameters = matchedMethod.GetParameters();
            bool[] isParamOut = parameters.Select(p => p.IsOut || p.IsRetval).ToArray();

            for (int i = 0; i < isParamOut.Length; ++i)
                if (isParamOut[i] && argItems[i].Spread)
                    throw new ScriptError(fileName, argItems[i], Resources.InvalidLValue);

            nativeArgValues = argValues.Select((val, i) => val.ConvertTo(parameters[i].ParameterType)).ToArray();
            result = matchedMethod.Invoke(target, nativeArgValues);

            for (int i = 0; i < isParamOut.Length; ++i)
                if (isParamOut[i])
                    Assign(argItems[i].Expression, DataItemFactory.CreateDataItem(nativeArgValues[i]));
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
    /// <param name="test">The expression to evaluate</param>
    /// <returns>A boolean value</returns>
    private bool IsTrue(Expression test)
    {
        try
        {
            test.AcceptTranslator(this);
            return returnedValue.AsBoolean;
        }
        catch (InvalidCastException ex)
        {
            throw new RuntimeError(fileName, test, ex);
        }
    }

    /// <summary>
    /// Returns the position of the next statement to be executed in a program or a block according to jumpCode.
    /// </summary>
    /// <param name="address">The position of the current statement</param>
    /// <param name="labels">The set of labels declared in the current program of block</param>
    /// <param name="canJumpOut">Tells if a goto statement can jump out of the current block or not</param>
    /// <param name="handleBreak">Tells if jumpCode should restored on breaks</param>
    /// <returns>An integer</returns>
    /// <exception cref="RuntimeError">The code is trying to jump out of a program</exception>
    private int NextAddress(int address, Dictionary<string, Label> labels, bool canJumpOut, bool handleBreak)
    {
        switch (jumpCode)
        {
            case JumpCode.None:
                return address + 1;
            case JumpCode.Break:
                if (handleBreak) jumpCode = JumpCode.None;
                return int.MaxValue;
            case JumpCode.Goto:
                if (!labels.TryGetValue(lastGoto.LabelName, out var label))
                    return canJumpOut
                         ? int.MaxValue
                         : throw new RuntimeError(fileName, lastGoto,
                            string.Format(Resources.MissingLabel, lastGoto.LabelName));
                
                jumpCode = JumpCode.None;
                return label.Address;
            default:
                return int.MaxValue;
        }
    }

    /// <summary>
    /// Evaluates an expression and iterates on the returned value,
    /// returning a couple of variables at each step.
    /// </summary>
    /// <param name="expr">The expression to iterate on</param>
    /// <returns>An <see cref="IEnumerable{T}"/></returns>
    private IEnumerable<(DataItem, DataItem)> GetEnumerable(Expression expr)
    {
        try
        {
            expr.AcceptTranslator(this);
            DataItem enumerated = returnedValue;

            return enumerated.Class.Inherits(Class.Object)
                 ? GetProgrammaticEnumerable(enumerated, expr)
                 : enumerated.GetEnumerable();
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, expr, ex);
        }
    }

    /// <summary>
    /// Gets a programmatic enumerable from a class implementing the iterator protocol.
    /// </summary>
    /// <param name="value">The object to iterate on</param>
    /// <param name="expr">The expression from which <paramref name="value"/> is obtained</param>
    /// <returns>An <see cref="IEnumerable{T}"/></returns>
    private IEnumerable<(DataItem, DataItem)> GetProgrammaticEnumerable(DataItem value, Expression expr)
    {
        Class klass = value.Class;
        ClassMethod iteratorMethod = klass.GetMethod("iterator");
        int counter = 0;

        if (iteratorMethod != null)
        {
            CheckAccess(iteratorMethod, expr);
            Invoke(iteratorMethod.Function, iteratorMethod.Name, iteratorMethod.Holder, value);

            foreach (DataItem item in yieldedValues)
                yield return (new Integer(counter++), item);

            yieldedValues.Clear();
        }
        else
        {
            ClassMethod moveFirstMethod = klass.GetMethod("moveFirst") ??
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, klass.Name));
            CheckAccess(moveFirstMethod, expr);

            ClassMethod hasNextMethod = klass.GetMethod("hasNext") ??
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, klass.Name));
            CheckAccess(hasNextMethod, expr);

            ClassMethod moveNextMethod = klass.GetMethod("moveNext") ??
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, klass.Name));
            CheckAccess(moveNextMethod, expr);

            Invoke(moveFirstMethod.Function, moveFirstMethod.Name, moveFirstMethod.Holder, value);
            Invoke(hasNextMethod.Function, hasNextMethod.Name, hasNextMethod.Holder, value);

            while (returnedValue.AsBoolean)
            {
                Invoke(moveNextMethod.Function, moveNextMethod.Name, moveNextMethod.Holder, value);
                yield return (new Integer(counter++), returnedValue);

                Invoke(hasNextMethod.Function, hasNextMethod.Name, hasNextMethod.Holder, value);
            }
        }
    }

    /// <summary>
    /// Captures the state of the interpreter at a particular time.
    /// </summary>
    /// <returns>An <see cref="InterpreterState"/></returns>
    private InterpreterState GetState()
        => new (frames, rootFrame, fileName, misRefAct, jumpCode, yieldedValues, lastGoto);

    /// <summary>
    /// Restores a the interpreter to an initially captured state.
    /// </summary>
    /// <param name="savedState">An <see cref="InterpreterState"/></param>
    private void RestoreState(InterpreterState savedState)
    {
        frames = new (savedState.frames);
        // Note: Items may be copied into a module in the future
        savedState.rootFrame.RootBlock.CopyItemsFrom(rootFrame.RootBlock);
        rootFrame = savedState.rootFrame;
        currentFrame = frames.Peek();
        fileName = savedState.fileName;
        misRefAct = savedState.misRefAct;
        jumpCode = savedState.jumpCode;
        yieldedValues = savedState.yieldedValues;
        lastGoto = savedState.lastGoto;
    }

    /// <summary>
    /// Resets the interpreter to its initial state.
    /// </summary>
    /// <param name="fileName">The value to set to the fieldName field</param>
    private void Reset(string fileName)
    {
        jumpCode = JumpCode.None;
        misRefAct = MissingReferenceAction.Fail;
        lastGoto = null;
        yieldedValues.Clear();
        frames.Clear();
        CreateRootFrame();
        RegisterDefaults(fileName);
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
            InterpreterState savedState = GetState();
            Reset(path);

            using (var reader = new StreamReader(path))
            {
                Program program = new Parser(new Lexer(reader)).Program();
                program.AcceptTranslator(this);
            }

            RestoreState(savedState);
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
                        throw new RuntimeError(fileName, statement,
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
    /// returns the corresponding variables.
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
    /// Evaluates the <i>Owner</i>, <i>LowerBound</i>, and <i>UpperBound</i> members of a SliceRef expression and
    /// returns the corresponding variables.
    /// </summary>
    /// <param name="sliceRef"></param>
    /// <param name="owner">Will contain the owner upon completion</param>
    /// <param name="lBound">Will contain the range's lower bound upon completion</param>
    /// <param name="uBound">Will contain the range's upper bound upon completion</param>
    private void ResolveSliceRef(SliceRef sliceRef, out DataItem owner, out int lBound, out int uBound)
    {
        if (sliceRef.LowerBound == null)
            lBound = 0;
        else
        {
            sliceRef.LowerBound.AcceptTranslator(this);
            lBound = returnedValue.AsInt32;
        }

        if (sliceRef.UpperBound == null)
            uBound = int.MaxValue;
        else
        {
            sliceRef.UpperBound.AcceptTranslator(this);
            uBound = returnedValue.AsInt32;
        }

        sliceRef.Owner.AcceptTranslator(this);
        owner = returnedValue;
    }

    /// <summary>
    /// Evaluates the <i>Owner</i> member of a PropertyRef expression and
    /// returns the corresponding variable.
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
    /// Assigns <paramref name="rValue"/> to the memory location represented by <paramref name="lValue"/>.
    /// </summary>
    /// <param name="lValue">The memory location to set</param>
    /// <param name="rValue">The value that should be assigned</param>
    /// <exception cref="RuntimeError">lValue is not a valid memory location</exception>
    private void Assign(Expression lValue, DataItem rValue)
    {
        if (lValue is IReference reference)
            reference.AcceptAssignmentProcessor(this, rValue);
        else
            throw new RuntimeError(fileName, lValue, Resources.InvalidLValue);

    }

    /// <summary>
    /// Expands a list potentially containing items with the spread operator to its full contents.
    /// </summary>
    /// <param name="listItems">The list that should be expanded</param>
    /// <returns>A <see cref="(DataItem[], ListItem[])"/> tuple</returns>
    /// <exception cref="ScriptError">The evaluation of an argument failed</exception>
    private (DataItem[], ListItem[]) ExpandList(ListItem[] listItems)
    {
        List<DataItem> values = [];
        List<ListItem> valueItems = [];

        foreach (ListItem item in listItems)
        {
            try
            {
                item.Expression.AcceptTranslator(this);

                if (item.Spread)
                    foreach (DataItem value in returnedValue.AsList)
                    {
                        values.Add(value);
                        valueItems.Add(item);
                    }
                else
                {
                    values.Add(returnedValue);
                    valueItems.Add(item);
                }
            }
            catch (ScriptError)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScriptError(fileName, item, ex);
            }
        }

        return ([.. values], [.. valueItems]);
    }

    #endregion
}