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


public class Interpreter : ITranslator, IAssignmentProcessor, IIntrospectionHelper
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
    public Interpreter() : this(new ScriptContext()) { }

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
            if (import.HasAlias)
            {
                if (ImportNamespace(import.ModuleName, import.Alias)) return;
                throw new RuntimeError(fileName, import, string.Format(Resources.ModuleNotFound, import.ModuleName));
            }

            if (ImportScript(import.ModuleName) || ImportNamespace(import.ModuleName, null)) return;
            throw new RuntimeError(fileName, import, string.Format(Resources.UndefinedType, import.ModuleName));
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(import);
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, import, ex);
        }
    }

    public void TranslateClassDefinition(ClassDefinition classDef)
    {
        Class superClass = ValidateClassDefinition(classDef);

        ClassMethod constructor = null;
        if (classDef.Constructor != null)
        {
            constructor = (ClassMethod)classDef.Constructor.ToClassMember();
            constructor.Attributes = ConvertAttributes(classDef.Constructor.Attributes);

            var parameters = constructor.Function.Parameters;
            for (var i = 0; i < parameters.Length; ++i)
                parameters[i].Attributes = ConvertAttributes(classDef.Constructor.Parameters[i].Attributes);
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

            var parameters = method.Function.Parameters;
            for (var i = 0; i < parameters.Length; ++i)
                parameters[i].Attributes = ConvertAttributes(m.Parameters[i].Attributes);

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
        if (IsRootItem(fnDecl.Name))
            throw new RuntimeError(fileName, fnDecl, string.Format(Resources.NameConflict, fnDecl.Name));

        Function function = fnDecl.ToFunction();
        function.Attributes = ConvertAttributes(fnDecl.Attributes);

        for (int i = 0; i < function.Parameters.Length; ++i)
            function.Parameters[i].Attributes = ConvertAttributes(fnDecl.Parameters[i].Attributes);

        rootFrame.RootBlock.PutItem(fnDecl.Name, function);
    }

    public void TranslateExternalFunctionDecl(ExternalFunctionDecl extDecl)
    {
        if (IsRootItem(extDecl.Name))
            throw new RuntimeError(fileName, extDecl, string.Format(Resources.NameConflict, extDecl.Name));

        const string importAttributeName = "LibImport";
        const string typeAttributeName = "Type";

        AttributeDecl importAttribute = extDecl.GetAttribute(importAttributeName) ??
            throw new RuntimeError(fileName, extDecl, string.Format(Resources.MissingAttribute,
                                                                    importAttributeName, extDecl.Name));

        VariableSetter libNameField = importAttribute.GetField(AttributeDecl.DEFAULT_FIELD_NAME) ??
            throw new ScriptError(fileName, importAttribute, string.Format(Resources.MissingAttributeProperty,
                                                                           AttributeDecl.DEFAULT_FIELD_NAME,
                                                                           importAttributeName));

        libNameField.Value.AcceptTranslator(this);
        string libName = WithNativeLibraryExtension(returnedValue.ToString());

        string procName = extDecl.Name;
        VariableSetter procNameField = importAttribute.GetField("procName");
        if (procNameField != null)
        {
            procNameField.Value.AcceptTranslator(this);
            procName = returnedValue.ToString();
        }

        var (returnType, defaultParamType) = (typeof(void), typeof(object));

        VariableSetter returnTypeField = importAttribute.GetField("returnType");
        if (returnTypeField != null)
        {
            returnTypeField.Value.AcceptTranslator(this);
            string returnTypeName = returnedValue.ToString();

            returnType = GetTypeByName(returnTypeName) ??
                throw new ScriptError(fileName, returnTypeField, string.Format(Resources.InvalidTypeReference, returnTypeName));
        }

        var parameters = extDecl.Parameters;
        var paramTypes = new Type[parameters.Length];
        var args = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; ++i)
        {
            AttributeDecl typeAttribute = parameters[i].GetAttribute(typeAttributeName);

            if (typeAttribute == null)
                paramTypes[i] = defaultParamType;
            else
            {

                VariableSetter typeNameProperty = typeAttribute.GetField(AttributeDecl.DEFAULT_FIELD_NAME) ??
                    throw new ScriptError(fileName, typeAttribute, string.Format(Resources.MissingAttributeProperty,
                                                                                 AttributeDecl.DEFAULT_FIELD_NAME,
                                                                                 typeAttributeName));

                typeNameProperty.Value.AcceptTranslator(this);
                string typeName = returnedValue.ToString();

                Type parameterType = GetTypeByName(typeName) ??
                    throw new ScriptError(fileName, typeAttribute, string.Format(Resources.InvalidTypeReference, typeName));

                paramTypes[i] = parameterType;
            }

            args[i] = new VariableRef(parameters[i].Name);
        }

        MethodInfo method = GetPInvokeMethod(libName, procName, returnType, paramTypes);
        Parameter[] fnParams = [.. extDecl.Parameters.Select(p => p.ToParameter())];
        Function function = new (fnParams, Block.WithReturn(new ExternalFunctionCall(method, args))); // No attribute retention
        rootFrame.RootBlock.PutItem(extDecl.Name, function);
    }

    public void TranslateConstantDecl(ConstantDecl cstDecl)
    {
        try
        {
            foreach (VariableSetter setter in cstDecl.Setters)
            {
                var (frameItem, frame) = FindFrameItem(setter.Name);

                switch (frameItem)
                {
                    case null:
                    case DataItem or Constant when frame != currentFrame:
                        setter.Value.AcceptTranslator(this);
                        currentFrame.PutItem(setter.Name, new Constant(returnedValue));
                        break;
                    default:
                        throw new ScriptError(fileName, setter, string.Format(Resources.NameConflict, setter.Name));
                }
            }
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(cstDecl);
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, cstDecl, ex);
        }
    }

    public void TranslateVariableDecl(VariableDecl varDecl)
    {
        foreach (VariableSetter setter in varDecl.Setters)
        {
            var (frameItem, frame) = FindFrameItem(setter.Name);

            switch (frameItem)
            {
                case null:
                case DataItem or Constant when frame != currentFrame:
                    if (setter.Value == null)
                        returnedValue = Undefined.Value;
                    else
                        setter.Value.AcceptTranslator(this);

                    currentFrame.PutItem(setter.Name, returnedValue);
                    break;
                default:
                    throw new ScriptError(fileName, setter, string.Format(Resources.NameConflict, setter.Name));
            }
        }
    }

    public void TranslateBlock(Block block)
    {
        currentFrame.PushBlock();

        foreach (var pair in block.Labels)
            RegisterLabel(pair.Key, pair.Value);

        try
        {
            var address = 0;
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
        catch (ScriptError se)
        {
            throw se.LocatedAt(assignment);
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
        var leftOperand = returnedValue;

        BinaryOperator _operator = binExpr.Operator;
        if (IsShortCircuited(_operator, leftOperand)) return;

        try
        {
            if (leftOperand.InstanceOf(Class.Object) && IsOverloadable(_operator))
            {
                var methodName = ClassMethod.GetMethodName(_operator);
                var method = leftOperand.Class.GetMethod(methodName);

                if (method != null)
                {
                    // Handle overloaded operators
                    CheckAccess(method, binExpr);
                    Invoke(method.Function, methodName, method.Holder, leftOperand, binExpr.RightOperand);
                    return;
                }
            }

            binExpr.RightOperand.AcceptTranslator(this);
            if (IsShortCircuiting(_operator)) return;

            var rightOperand = returnedValue;
            returnedValue = leftOperand.ConversionNeeded(rightOperand.Class, _operator)
                          ? leftOperand.ConvertTo(rightOperand.Class).BinaryOperation(_operator, rightOperand)
                          : leftOperand.BinaryOperation(_operator, rightOperand);
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
        catch (ScriptError se)
        {
            throw se.LocatedAt(binExpr);
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, binExpr, ex);
        }
    }

    public void TranslateUnaryExpression(UnaryExpression unExpr)
    {
        unExpr.Operand.AcceptTranslator(this);
        var operandValue = returnedValue;

        var _operator = unExpr.Operator;
        if (_operator == UnaryOperator.NotEmpty)
        {
            if (!operandValue.IsEmpty()) return;
            throw new RuntimeError(fileName, unExpr, Resources.ValueShouldNotBeEmpty);
        }

        try
        {
            if (operandValue.InstanceOf(Class.Object) && IsOverloadable(_operator, out var postfix))
            {
                var methodName = ClassMethod.GetMethodName(_operator);
                var method = operandValue.Class.GetMethod(methodName);

                if (method != null)
                {
                    CheckAccess(method, unExpr);
                    Invoke(method.Function, methodName, method.Holder, operandValue, postfix ? [new Literal()] : []);
                    return;
                }
            }
            else if (operandValue is Integer or Long)
            {
                var literalOne = new Literal(new Integer(1));

                switch (_operator)
                {
                    case UnaryOperator.PreIncrement:
                        new Assignment(BinaryOperator.Plus, unExpr.Operand, literalOne)
                            .AcceptTranslator(this);
                        return;
                    case UnaryOperator.PostIncrement:
                        new Assignment(BinaryOperator.Plus, unExpr.Operand, literalOne)
                            .AcceptTranslator(this);
                        returnedValue = operandValue;
                        return;
                    case UnaryOperator.PreDecrement:
                        new Assignment(BinaryOperator.Minus, unExpr.Operand, literalOne)
                            .AcceptTranslator(this);
                        return;
                    case UnaryOperator.PostDecrement:
                        new Assignment(BinaryOperator.Minus, unExpr.Operand, literalOne)
                            .AcceptTranslator(this);
                        returnedValue = operandValue;
                        return;
                }
            }

            returnedValue = operandValue.UnaryOperation(_operator);
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(unExpr);
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
        catch (ScriptError se)
        {
            throw se.LocatedAt(cplxInit);
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, cplxInit, ex);
        }
    }

    public void TranslateTupleInitializer(TupleInitializer tupleInitializer)
    {
        var (items, _) = ExpandArguments(tupleInitializer.Items);
        returnedValue = new Tuple([.. items]);
    }

    public void TranslateListInitializer(ListInitializer listInit)
    {
        var (items, _) = ExpandArguments(listInit.Items);
        returnedValue = new List(items);
    }

    public void TranslateSetInitializer(SetInitializer setInit)
    {
        var (items, _) = ExpandArguments(setInit.Items);
        returnedValue = new Set(items);
    }

    public void TranslateMapInitializer(MapInitializer mapInit)
    {
        Dictionary<DataItem, DataItem> dict = [];

        foreach (var entry in mapInit.Entries)
        {
            try
            {
                entry.Key.AcceptTranslator(this);
                DataItem key = returnedValue;

                entry.Value.AcceptTranslator(this);
                dict.Add(key, returnedValue);
            }
            catch (ScriptError se)
            {
                throw se.LocatedAt(entry);
            }
            catch (Exception ex)
            {
                throw new ScriptError(fileName, entry, ex);
            }
        }

        returnedValue = new Map(dict);
    }

    public void TranslateObjectInitializer(ObjectInitializer objInit)
    {
        Dictionary<string, DataItem> fields = [];

        foreach (VariableSetter setter in objInit.PropertySetters)
        {
            try
            {
                setter.Value.AcceptTranslator(this);
                fields.Add(setter.Name, returnedValue);
            }
            catch (ScriptError se)
            {
                throw se.LocatedAt(setter);
            }
            catch (Exception ex)
            {
                throw new ScriptError(fileName, setter, ex);
            }
        }

        returnedValue = new Object(fields);
    }

    public void TranslateInlineFunction(InlineFunction inlineFn)
    {
        Function function = inlineFn.ToFunction();
        function.ParentFrame = currentFrame != rootFrame ? currentFrame : null;

        for (var i = 0; i < function.Parameters.Length; ++i)
            function.Parameters[i].Attributes = ConvertAttributes(inlineFn.Parameters[i].Attributes);

        returnedValue = new Closure(function);
    }

    public void TranslateVariableRef(VariableRef varRef)
    {
        var (frameItem, _) = FindFrameItem(varRef.Name);

        switch (frameItem)
        {
            case null when misRefAct == MissingReferenceAction.Create:
                returnedValue = Undefined.Value;
                currentFrame.PutItem(varRef.Name, returnedValue);
                break;
            case null when misRefAct == MissingReferenceAction.Fail:
                throw new RuntimeError(fileName, varRef, string.Format(Resources.UndefinedVariable, varRef.Name));
            case null: // do nothing when misRefAction is Ignore
                break;
            case Undefined when misRefAct == MissingReferenceAction.Fail:
                throw new RuntimeError(fileName, varRef, string.Format(Resources.UninitializedVariable, varRef.Name));
            case DataItem variable: // handles Undefined when misRefAct is not Fail
                returnedValue = variable;
                break;
            case Constant constant:
                returnedValue = constant.Value;
                break;
            case Function function:
                returnedValue = new Closure(function);
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

            switch (owner)
            {
                case null:
                    itemValue = null;
                    break;
                case Void when itemRef.Optional:
                    itemValue = Void.Value;
                    break;
                case not null when indexer is null:
                    itemValue = owner.GetItem(index);
                    break;
                case not null when !indexer.CanRead:
                    throw new RuntimeError(fileName, itemRef, Resources.CannotReadProperty);
                default:
                    CheckAccess(indexer.Reader, itemRef);
                    Invoke(indexer.Reader.Function, indexer.Name, indexer.Holder, owner, new Literal(index));
                    return;
            }

            if (itemValue == null && misRefAct != MissingReferenceAction.Ignore)
                throw new RuntimeError(fileName, itemRef, string.Format(Resources.IndexNotFound, index));

            returnedValue = itemValue;
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(itemRef);
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
        catch (ScriptError se)
        {
            throw se.LocatedAt(sliceRef);
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
                            var methodCall = new MethodCall(new Literal(owner), method.Name, args);
                            var function = new Function(parameters, Block.WithReturn(methodCall));
                            propValue = new Closure(function);
                            break;
                        }
                        case ClassProperty { CanRead: true } property:
                            CheckAccess(property.Reader, propertyRef);
                            Invoke(property.Reader.Function, property.Name, property.Holder, owner);
                            return;
                        default: // member is surely a ClassProperty with CanRead == false
                            throw new RuntimeError(fileName, propertyRef, Resources.CannotReadProperty);

                    }
                    break;

            }

            if (propValue == null && misRefAct != MissingReferenceAction.Ignore)
                throw new RuntimeError(fileName, propertyRef, string.Format(Resources.PropertyNotFoundInObject,
                                                                            propertyRef.PropertyName));

            returnedValue = propValue;
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(propertyRef);
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
                case ClassProperty { CanRead: true } property:
                    CheckAccess(property.Reader, staticRef);
                    Invoke(property.Reader.Function, property.Name, property.Holder, null);
                    break;
                case ClassProperty:
                    throw new RuntimeError(fileName, staticRef, Resources.CannotReadProperty);
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
        catch (ScriptError se)
        {
            throw se.LocatedAt(staticRef);
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
            var (frameItem, _) = FindFrameItem(fnCall.FunctionName);

            Function fn;
            InvocationContext ctx;

            switch (frameItem)
            {
                case Function function:
                    fn = function;
                    ctx = currentFrame.Context;
                    break;
                case Closure closure:
                    fn = closure.AsFunction;
                    ctx = (fn.ParentFrame ?? currentFrame).Context;
                    break;
                default:
                    throw new RuntimeError(fileName, fnCall, string.Format(Resources.UndefinedFunction, fnCall.FunctionName));
            }

            Invoke(fn, fnCall.FunctionName, ctx.MethodHolder, ctx.MethodTarget, fnCall.Arguments, fnCall.NamedArgs);
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(fnCall);
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
            anCall.Called.AcceptTranslator(this);

            DataItem target = returnedValue;
            if (target is not Closure) throw new RuntimeError(fileName, anCall.Called, Resources.CalleeIsNotClosure);

            Function function = target.AsFunction;
            InvocationContext ctx = (function.ParentFrame ?? currentFrame).Context;
            Invoke(function, anCall.FunctionName, ctx.MethodHolder, ctx.MethodTarget, anCall.Arguments, anCall.NamedArgs);
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(anCall);
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
            if (methodTarget is Void && methodCall.Optional) return;

            ClassMethod method = methodTarget.Class.GetMethod(methodCall.FunctionName);
            Class methodHolder = currentFrame.Context.MethodHolder;
            Function function = null;

            if (method == null)
                switch (methodTarget.Class.ClassID)
                {
                    case ClassID.Object:
                    {
                        ClassField field = methodTarget.Class.GetField(methodCall.FunctionName);
                        if (field != null) CheckAccess(field, methodCall);

                        DataItem fieldValue = field is { IsStatic: true }
                                            ? field.SharedValue
                                            : methodTarget.GetProperty(methodCall.FunctionName);

                        if (fieldValue is Closure) function = fieldValue.AsFunction;
                        break;
                    }
                    case ClassID.Resource:
                    {
                        object nativeTarget = methodTarget.AsNativeObject;
                        InvokeNative(nativeTarget.GetType(), methodCall.FunctionName, nativeTarget, methodCall.Arguments);
                        return; // IMPORTANT!!!
                    }
                }
            else
            {
                CheckAccess(method, methodCall);
                function = method.Function;
                methodHolder = method.Holder;
            }

            if (function == null)
                throw new RuntimeError(fileName, methodCall, string.Format(Resources.MethodNotFound,
                                                                           methodCall.FunctionName,
                                                                           methodTarget.Class.Name));

            Invoke(function, methodCall.FunctionName, methodHolder, methodTarget, methodCall.Arguments, methodCall.NamedArgs);
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(methodCall);
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
            switch (targetMethod)
            {
                case ClassMethod method:
                    Invoke(method.Function, method.Name, method.Holder, null, staticCall.Arguments, staticCall.NamedArgs);
                    break;
                case ClassField { SharedValue: Closure closure } field:
                    Invoke(closure.AsFunction, field.Name, currentFrame.Context.MethodHolder, null, staticCall.Arguments, staticCall.NamedArgs);
                    break;
                case ClassField field:
                    throw new RuntimeError(fileName, staticCall, string.Format(Resources.MethodNotFound, field.Name, staticCall.Name));
                case StaticTypeMember member:
                    InvokeNative(member.Type, member.MemberName, null, staticCall.Arguments);
                    break;
                default:
                    throw new RuntimeError(fileName, staticCall, string.Format(Resources.UnresolvedMemberRef, staticCall.Name));
            }
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(staticCall);
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

                    DataItem instance = new Object(klass);
                    InitializeFields(instance);
                    Invoke(constructor.Function, constructor.Name, klass, instance, ctorCall.Arguments, ctorCall.NamedArgs);

                    if (ctorCall.PropertySetters != null)
                        ApplyPropertySetters(ctorCall, instance, ctorCall.PropertySetters);

                    returnedValue = instance;
                    break;
                }
                case Type type:
                {
                    var (args, _) = ExpandArguments(ctorCall.Arguments ?? []);
                    object nativeInstance = Reflector.CreateInstance(type, args);
                    DataItem instance = DataItemFactory.CreateDataItem(nativeInstance);

                    if (ctorCall.PropertySetters != null)
                        foreach (VariableSetter setter in ctorCall.PropertySetters)
                        {
                            setter.Value.AcceptTranslator(this);
                            instance.SetProperty(setter.Name, returnedValue);
                        }

                    returnedValue = instance;
                    break;
                }
                default:
                    throw new RuntimeError(fileName, ctorCall, string.Format(Resources.UndefinedType, ctorCall.Name));
            }
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(ctorCall);
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
                throw new RuntimeError(fileName, pmc, string.Format(Resources.MethodNotFound, pmc.FunctionName,
                                                                    _this.Class.SuperClass.Name));

            if (method.Modifier == Modifier.Abstract)
                throw new RuntimeError(fileName, pmc, string.Format(Resources.CannotInvokeAbstractMember, method.FullName));

            CheckAccess(method, pmc);
            Invoke(method.Function, pmc.FunctionName, method.Holder, _this, pmc.Arguments, pmc.NamedArgs);
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(pmc);
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
        catch (ScriptError se)
        {
            throw se.LocatedAt(pcc);
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
                if (!property.CanRead)
                    throw new RuntimeError(fileName, ppr, Resources.CannotReadProperty);

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
                var methodCall = new MethodCall(new Literal(newTarget), member.Name, args);
                var function = new Function(parameters, Block.WithReturn(methodCall));
                returnedValue = new Closure(function);
            }
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(ppr);
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
        catch (ScriptError se)
        {
            throw se.LocatedAt(pir);
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, pir, ex);
        }
    }

    public void TranslateInnerFunctionCall(InnerFunctionCall innerCall)
    {
        var arguments = new DataItem[innerCall.Arguments.Length];

        for (var i = 0; i < arguments.Length; ++i)
        {
            innerCall.Arguments[i].Value.AcceptTranslator(this);
            arguments[i] = returnedValue;
        }

        returnedValue = innerCall.Function.Logic(arguments);
    }

    public void TranslateExternalFunctionCall(ExternalFunctionCall extCall)
    {
        ParameterInfo[] parameters = extCall.Method.GetParameters();
        var args = new object[extCall.Arguments.Length];

        for (var i = 0; i < extCall.Arguments.Length; ++i)
        {
            extCall.Arguments[i].Value.AcceptTranslator(this);
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
        catch (ScriptError se)
        {
            throw se.LocatedAt(typeVerif);
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

            if (klass.ClassID is < ClassID.Boolean or > ClassID.Object)
                throw new RuntimeError(fileName, conversion, string.Format(Resources.CannotConvertTo, conversion.TypeName));

            returnedValue = converted.ConvertTo(klass);
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(conversion);
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, conversion, ex);
        }
    }

    public void TranslateIfElse(IfElse ifElse)
    {
        if (IsTrue(ifElse.Guard))
            ifElse.Action.AcceptTranslator(this);
        else
            ifElse.AlternativeAction?.AcceptTranslator(this);
    }

    public void TranslateSwitchBlock(SwitchBlock switchBlock)
    {
        switchBlock.Test.AcceptTranslator(this);
        int hashCode = returnedValue.GetHashCode();

        int address = switchBlock.Cases
                                 .Where(label => label.GetHashCode() == hashCode)
                                 .Select(label => label.Address)
                                 .FirstOrDefault(switchBlock.DefaultCase);

        currentFrame.PushBlock();

        foreach (var pair in switchBlock.Labels)
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

        foreach (var initializer in forLoop.Initializers)
            initializer.AcceptTranslator(this);

        var guard = forLoop.Guard ?? new Literal(Boolean.True);

        while (IsTrue(guard))
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

            foreach (var incrementer in forLoop.Incrementers)
                incrementer.AcceptTranslator(this);
        }

    EXIT:
        currentFrame.PopBlock();
    }

    public void TranslateForEachLoop(ForEachLoop forEach)
    {
        currentFrame.PushBlock();

        foreach (var (key, value) in GetEnumerable(forEach.Guard))
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
        ScriptElement finalExceptionLocation = tcf.TryBlock;

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
                    finalExceptionLocation = tcf.CatchBlock;
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
                    finalExceptionLocation = tcf.FinallyBlock;
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

        if (finalException == null) return;
        throw finalException.LocatedAt(finalExceptionLocation);
    }

    public void TranslateStringInterpolation(StringInterpolation stringInt)
    {
        try
        {
            List<DataItem> listItems = [];
            foreach (var substitution in stringInt.Substitions)
            {
                substitution.AcceptTranslator(this);
                listItems.Add(returnedValue);
            }

            Expression[] args =
            [
                new Literal(new String(stringInt.Pattern)) ,
                new Literal(new List(listItems))
            ];

            var innerFnCall = new InnerFunctionCall(InnerFunction.Format, args);
            innerFnCall.CopyLocation(stringInt);
            innerFnCall.AcceptTranslator(this);
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(stringInt);
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
                var matchedCase = patMatch.MatchCases.FirstOrDefault(matchCase => IsMatch(matchCase, testArg));

                if (matchedCase == null)
                    returnedValue = Void.Value;
                else
                {
                    matchedCase.Pattern.GetExtractionAction(testArg)?.AcceptTranslator(this);
                    matchedCase.Expression.AcceptTranslator(this);
                }
            }
            finally
            {
                currentFrame.PopBlock();
            }
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(patMatch);
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, patMatch, ex);
        }
    }

    public void TranslateMutableCopy(MutableCopy mutableCopy)
    {
        try
        {
            mutableCopy.Original.AcceptTranslator(this);
            DataItem original = returnedValue;

            var klass = original.Class;
            if (!klass.Inherits(Class.Record))
                throw new RuntimeError(fileName, mutableCopy, Resources.InvalidOperandForWith);

            var originalRef = new Literal(original);
            var args = klass.Constructor.Function.Parameters.ToDictionary(
                p => p.Name, Expression (p) => new PropertyRef(originalRef, p.Name));

            foreach (var setter in mutableCopy.PropertySetters)
            {
                if (args.ContainsKey(setter.Name))
                    args[setter.Name] = setter.Value;
                else
                    throw new ScriptError(fileName, setter, string.Format(Resources.PropertyNotFoundInClass,
                                                                          setter.Name, original.Class.Name));
            }

            var ctorCall = new ConstructorCall(new QualifiedName(klass.Name), null, args, null);
            ctorCall.CopyLocation(mutableCopy);
            ctorCall.AcceptTranslator(this);
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(mutableCopy);
        }
        catch (Exception ex)
        {
            throw new RuntimeError(fileName, mutableCopy, ex);
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

        switch (indexer)
        {
            case null:
                owner.SetItem(index, rValue);
                break;
            case { CanWrite: false}:
                throw new RuntimeError(fileName, itemRef, Resources.CannotWriteProperty);
            default:
                CheckAccess(indexer.Writer, itemRef);
                Invoke(indexer.Writer.Function, indexer.Name, indexer.Holder, owner, new Literal(index), new Literal(rValue));
                break;
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

        switch (member)
        {
            case null:
                owner.SetProperty(propertyRef.PropertyName, rValue);
                break;
            case ClassField field:
                switch (field.Modifier)
                {
                    case Modifier.Default:
                    case Modifier.Final when currentFrame.Context.MethodIsConstructor():
                        owner.SetProperty(field.Name, rValue);
                        break;
                    case Modifier.Static:
                        field.SharedValue = rValue;
                        break;
                    default: // Final (out of constructor) or StaticFinal
                        throw new RuntimeError(fileName, propertyRef, Resources.CannotWriteFinalField);
                }
                break;
            case ClassProperty { CanWrite: false }:
                throw new RuntimeError(fileName, propertyRef, Resources.CannotWriteProperty);
            case ClassProperty property:
                CheckAccess(property.Writer, propertyRef);
                Invoke(property.Writer.Function, property.Name, property.Holder, owner, new Literal(rValue));
                break;
            default:
                throw new RuntimeError(fileName, propertyRef, Resources.InvalidLValue);
        }
    }

    public void AssignToStaticProperty(StaticPropertyRef staticRef, DataItem rValue)
    {
        switch (ResolveName(staticRef.Name, staticRef))
        {
            case ClassField { Modifier: Modifier.Static } field:
                field.SharedValue = rValue;
                break;
            case ClassField: // StaticFinal field
                throw new RuntimeError(fileName, staticRef, Resources.CannotWriteFinalField);
            case ClassProperty { CanWrite: false }:
                throw new RuntimeError(fileName, staticRef, Resources.CannotWriteProperty);
            case ClassProperty property:
                CheckAccess(property.Writer, staticRef);
                Invoke(property.Writer.Function, property.Name, property.Holder, null, new Literal(rValue));
                break;
            case StaticTypeMember member:
                member.SetValue(rValue);
                break;
            default:
                throw new RuntimeError(fileName, staticRef, string.Format(Resources.UnresolvedMemberRef, staticRef.Name));
        }
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

    public void AssignToTuple(TupleInitializer tupleInit, DataItem rValue)
    {
        DataItem[] rValueItems = rValue.AsArray;

        if (rValueItems.Length != tupleInit.Items.Length)
            throw new RuntimeError(fileName, tupleInit, Resources.ListLengthMismatch);

        for (var i = 0; i < tupleInit.Items.Length; ++i)
        {
            var item = tupleInit.Items[i];
            switch (item)
            {
                case { Spread: true }:
                    throw new ScriptError(fileName, item, Resources.NotAReference);
                case { Value: VariableRef { Name: AlwaysTruePattern.Symbol } }:
                    continue;
                case { Value: IReference reference }:
                    reference.AcceptAssignmentProcessor(this, rValueItems[i]);
                    break;
                default:
                    throw new ScriptError(fileName, item, Resources.NotAReference);
            }
        }
    }

    public void AssignToSet(SetInitializer setInit, DataItem rValue)
    {
        if (setInit.Items.Length == 0)
            throw new RuntimeError(fileName, setInit, Resources.ListCantBeEmpty);

        VariableRef collector = null;
        HashSet<string> excludedMembers = ["type"];
        Literal parentObj = new (rValue);
        PropertyRef propRef;

        foreach (var item in setInit.Items)
            switch (item)
            {
                case { Spread: true, Value: VariableRef varRef } when collector == null:
                    collector = varRef;
                    break;
                case { Spread: true }:
                    throw new ScriptError(fileName, item, Resources.NotAReference);
                case { Value: VariableRef varRef }:
                    excludedMembers.Add(varRef.Name);
                    propRef = new PropertyRef(parentObj, varRef.Name);
                    propRef.CopyLocation(varRef);
                    new Assignment(varRef, propRef).AcceptTranslator(this);
                    break;
                case
                {
                    Value: Assignment
                    {
                        Operator: BinaryOperator.None,
                        RightOperand: VariableRef varRef
                    } assignment
                }:
                    excludedMembers.Add(varRef.Name);
                    propRef = new PropertyRef(parentObj, varRef.Name);
                    propRef.CopyLocation(varRef);
                    new Assignment(
                            assignment.LeftOperand,
                            new Assignment(varRef, propRef))
                        .AcceptTranslator(this);
                    break;
                case
                {
                    Value: Assignment
                    {
                        Operator: BinaryOperator.None,
                        LeftOperand: VariableRef varRef
                    } assignment
                }:
                    excludedMembers.Add(varRef.Name);
                    propRef = new PropertyRef(parentObj, varRef.Name);
                    propRef.CopyLocation(varRef);
                    new Assignment(
                            varRef,
                            new TernaryExpression(
                                new TypeVerification(propRef, Class.Void.Name),
                                assignment.RightOperand,
                                propRef))
                        .AcceptTranslator(this);
                    break;
                default:
                    throw new ScriptError(fileName, item, Resources.NotAReference);
            }

        if (collector == null) return;

        var dataMembers = rValue.Class.GetMembers(MemberKind.Field | MemberKind.Property);

        excludedMembers.UnionWith(dataMembers.Where(m =>
                m.Scope != Scope.Public ||
                m.Modifier is not (Modifier.Default or Modifier.Final) ||
                m is ClassProperty { CanRead: false })
            .Select(m => m.Name)
            .ToHashSet());

        var setters = dataMembers.Where(m =>
                m.Scope == Scope.Public &&
                m.Modifier is Modifier.Default or Modifier.Final &&
                m is ClassField or ClassProperty { CanRead: true } &&
                !excludedMembers.Contains(m.Name))
            .Select(m => new VariableSetter(m.Name, new PropertyRef(parentObj, m.Name)))
            .ToList();

        setters.AddRange([.. rValue.AsDynamicObject
            .Where(pair => rValue.Class.GetField(pair.Key) == null && !excludedMembers.Contains(pair.Key))
            .Select(pair => new VariableSetter(pair.Key, new Literal(pair.Value)))]);

        var initializer = new ObjectInitializer([.. setters]);
        initializer.CopyLocation(collector);
        new Assignment(collector, initializer).AcceptTranslator(this);
    }

    #endregion

    #region Members of IIntrospectionHelper

    public DataItem IsSubclassOf(DataItem sourceTypeInfo, DataItem targetTypeInfo)
    {
        var (sourceClass, targetClass) = (GetClass(sourceTypeInfo), GetClass(targetTypeInfo));
        return Boolean.FromBool(sourceClass.Inherits(targetClass));
    }

    public DataItem IsAssignableTo(DataItem sourceTypeInfo, DataItem targetTypeInfo)
    {
        var (sourceClass, targetClass) = (GetClass(sourceTypeInfo), GetClass(targetTypeInfo));
        return Boolean.FromBool(sourceClass == targetClass ||
                                sourceClass.Inherits(targetClass) ||
                                sourceClass.IsLosslesslyConvertibleTo(targetClass));
    }

    public DataItem NewInstance(DataItem typeInfo, DataItem arguments)
    {
        Expression[] literals = [.. arguments.AsList.Select(arg => new Literal(arg))];
        QualifiedName className = new (GetName(typeInfo));
        new ConstructorCall(className, literals).AcceptTranslator(this);
        return returnedValue;
    }

    public DataItem GetValue(DataItem memberInfo, DataItem target)
    {
        Expression propertyRef = target is Void
                               ? new StaticPropertyRef(GetFullName(memberInfo))
                               : new PropertyRef(new Literal(target), GetName(memberInfo));

        propertyRef.AcceptTranslator(this);
        return returnedValue;
    }

    public DataItem SetValue(DataItem memberInfo, DataItem target, DataItem value)
    {
        Expression propertyRef = target is Void
                               ? new StaticPropertyRef(GetFullName(memberInfo))
                               : new PropertyRef(new Literal(target), GetName(memberInfo));

        new Assignment(propertyRef, new Literal(value)).AcceptTranslator(this);
        return Void.Value;
    }

    public DataItem GetItem(DataItem propertyInfo, DataItem target, DataItem index)
    {
        var itemRef = new ItemRef(new Literal(target), new Literal(index));
        itemRef.AcceptTranslator(this);
        return returnedValue;
    }

    public DataItem SetItem(DataItem propertyInfo, DataItem target, DataItem index, DataItem value)
    {
        var itemRef = new ItemRef(new Literal(target), new Literal(index));
        new Assignment(itemRef, new Literal(value)).AcceptTranslator(this);
        return Void.Value;
    }

    public DataItem Invoke(DataItem methodInfo, DataItem target, DataItem arguments)
    {
        Expression[] args = [.. arguments.AsList.Select(arg => new Literal(arg))];
        Expression methodCall = target is Void
                              ? new StaticMethodCall(GetFullName(methodInfo), args)
                              : new MethodCall(new Literal(target), GetName(methodInfo), args);

        methodCall.AcceptTranslator(this);
        return returnedValue;
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
        rootFrame.PutItem("MININT", new Constant(int.MinValue));
        rootFrame.PutItem("MAXINT", new Constant(int.MaxValue));
        rootFrame.PutItem("MINFLOAT", new Constant(double.MinValue));
        rootFrame.PutItem("MAXFLOAT", new Constant(double.MaxValue));
        rootFrame.PutItem("NAN", new Constant(double.NaN));
        rootFrame.PutItem("NINFINITY", new Constant(double.NegativeInfinity));
        rootFrame.PutItem("PINFINITY", new Constant(double.PositiveInfinity));
        rootFrame.PutItem("EPSILON", new Constant(double.Epsilon));
        rootFrame.PutItem("PI", new Constant(Math.PI));
        rootFrame.PutItem("E", new Constant(Math.E));
        rootFrame.PutItem("MINDATE", new Constant(DateTime.MinValue));
        rootFrame.PutItem("MAXDATE", new Constant(DateTime.MaxValue));
        rootFrame.PutItem("NEWLINE", new Constant(Environment.NewLine));
        rootFrame.PutItem(MODULE_NAME_CONSTANT, new Constant(moduleName));

        // Then create a callable wrapper for each global builtin function and register it
        foreach (InnerFunction innerFunc in InnerFunction.Globals)
            rootFrame.PutItem(innerFunc.Name, innerFunc.ToFunction());

        // Register predefined classes
        foreach (Class klass in Class.Predefined)
            rootFrame.PutItem(klass.Name, klass);

        // Register context variables
        foreach (KeyValuePair<string, object> pair in InitialContext.Bindings)
            rootFrame.PutItem(pair.Key, DataItemFactory.CreateDataItem(pair.Value));

        // Makes the context available to the script
        rootFrame.PutItem(CONTEXT_VARIABLE_NAME, new Resource(InitialContext));
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

        switch (frameItem)
        {
            case null:
                currentFrame.PutItem(name, variable);
                break;
            case DataItem:
                frame.PutItem(name, variable);
                break;
            case Constant:
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
            throw new ScriptError(fileName, label, string.Format(Resources.NameConflict, name));

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
        GetInitialFrameItems(Function function, string functionName, Argument[] positionalArgs,
                             Dictionary<string, Expression> namedArgs)
    {
        // Make sure we are not dealing with null references
        positionalArgs ??= [];
        namedArgs ??= [];

        // Expand the list of positional arguments
        var (argValues, argItems) = ExpandArguments(positionalArgs);
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
            Argument argument = argItems[counter];
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
                counter = int.MaxValue; // Will exit the loop
            }
            else
            {
                // Otherwise, set the value provided to the parameter
                CheckEmptiness(parameter, argValue, argument);
                frameItems.Add(parameter.Name, argValue);
                ++counter;
            }
        }

        List<Expression> expandedArgList = [.. argItems.Select(arg => arg.Value)];

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
        foreach (var pair in function.CapturedItems.Where(kv => !frameItems.ContainsKey(kv.Key)))
            frameItems.Add(pair.Key, pair.Value);

        return (frameItems, expandedArgList);
    }

    /// <summary>
    /// determines if the specified name corresponds to a root frame item.
    /// </summary>
    /// <param name="name">The name to check</param>
    /// <returns>
    /// <b>true</b> if <paramref name="name"/> is defined at the root level in the root frame.
    /// <b>false</b> otherwise.
    /// </returns>
    private bool IsRootItem(string name) => rootFrame.RootBlock.GetItem(name) != null;

    /// <summary>
    /// Searches for a frame item with the specified name in the current method frame, falling back to the root frame if not found.
    /// </summary>
    /// <remarks>
    /// If the frame item is not present in the current frame, the search continues in the root frame.
    /// This method does not throw an exception if the item is not found; callers should check for a null frame item in the result.
    /// </remarks>
    /// <param name="name">The name of the frame item to locate. Cannot be null.</param>
    /// <returns>
    /// A tuple containing the found frame item and the frame in which it was found. If no item is found, the frame item
    /// will be null and the frame will indicate where the search ended.
    /// </returns>
    private (IFrameItem, MethodFrame) FindFrameItem(string name)
    {
        MethodFrame frame = currentFrame;
        IFrameItem frameItem = frame.GetItem(name);

        if (frameItem == null && frame != rootFrame)
        {
            frame = rootFrame;
            frameItem = frame.GetItem(name);
        }

        return (frameItem, frame);
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

        if (function.ParentFrame == null) return;

        function.UpdateCapturedItems(frameItems);
        function.ParentFrame.SyncItems(frameItems, namesToSkip);
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
                        Argument[] positionalArgs, Dictionary<string, Expression> namedArgs)
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
        Invoke(function, name, holder, target, Call.ToArguments(arguments), null);
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
    /// Determines if the given unary operator is overloadable or not.
    /// </summary>
    /// <param name="_operator">The given unary operator</param>
    /// <param name="postfix">Tells whether the operator is the postfix variant or not</param>
    /// <returns>
    /// <b>false</b> for ! and the post-fixed variants of ++ and --. <b>true</b> in any other case
    /// </returns>
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
    /// Determines if the given binary operator is overloadable or not.
    /// </summary>
    /// <param name="_operator">The given unary operator</param>
    /// <returns><b>false</b> for &amp;&amp;, ||, ===, !== and ??. <b>true</b> in any other case</returns>
    private static bool IsOverloadable(BinaryOperator _operator) => _operator is not
        (
            BinaryOperator.None or BinaryOperator.AndAlso or BinaryOperator.OrElse or
            BinaryOperator.Identical or BinaryOperator.NotIdentical or BinaryOperator.IfEmpty
        );

    /// <summary>
    /// Determines if the given binary operator is short-circuiting or not.
    /// </summary>
    /// <param name="_operator">The binary operator to check</param>
    /// <returns><b>true</b> for &amp;&amp;, ||, and ??. <b>false</b> in any other case</returns>
    private static bool IsShortCircuiting(BinaryOperator _operator) =>
        _operator is BinaryOperator.AndAlso or BinaryOperator.OrElse or BinaryOperator.IfEmpty;

    /// <summary>
    /// Determines if the given binary operator is short-circuited based on the left operand's value.
    /// </summary>
    /// <param name="_operator">The binary operator to check</param>
    /// <param name="leftOperand">The left operand's value</param>
    /// <returns>
    /// <b>true</b> if <paramref name="_operator"/> is &amp;&amp; and <paramref name="leftOperand"/> is false,
    /// or <paramref name="_operator"/> is || and <paramref name="leftOperand"/> is true,
    /// or <paramref name="_operator"/> is ?? and <paramref name="leftOperand"/> is not empty.
    /// <b>false</b> in any other case</returns>
    private static bool IsShortCircuited(BinaryOperator _operator, DataItem leftOperand) =>
        (_operator == BinaryOperator.AndAlso && !leftOperand.AsBoolean) ||
        (_operator == BinaryOperator.OrElse && leftOperand.AsBoolean) ||
        (_operator == BinaryOperator.IfEmpty && !leftOperand.IsEmpty());

    /// <summary>
    /// Retrieves the type/member name from the specified data item.
    /// </summary>
    /// <param name="info">The data item containing type or member information. Must not be null and must have a property named "__name".</param>
    /// <returns>A string representing the name extracted from the <paramref name="info"/> data item.</returns>
    private static string GetName(DataItem info) => info.GetProperty("__name").ToString();

    /// <summary>
    /// Retrieves the fully qualified name of a type member from the specified data item.
    /// </summary>
    /// <param name="memberInfo">
    /// The data item containing member information. Must not be null and must have properties named "__name" and "__holder".
    /// </param>
    /// <returns>
    /// A <see cref="QualifiedName"/> representing the fully qualified name extracted from the <paramref name="memberInfo"/> data item.
    /// </returns>
    private static QualifiedName GetFullName(DataItem memberInfo) =>
        new (memberInfo.GetProperty("__holder").ToString(), GetName(memberInfo));

    /// <summary>
    /// Validates a class definition against inheritance and member rules, ensuring it can be safely added to the type system.
    /// </summary>
    /// <remarks>
    /// This method enforces rules such as prohibiting duplicate class names, preventing subclassing of final or static classes,
    /// requiring overrides for abstract members, and ensuring that new members do not hide or conflict with inherited members.
    /// It does not add the class to the type system; it only performs validation.
    /// </remarks>
    /// <param name="classDef">The class definition to validate, including its name, base class, and declared members.</param>
    /// <returns>The superclass of the validated class definition if validation succeeds.</returns>
    /// <exception cref="RuntimeError">
    /// Thrown if the class name conflicts with an existing type, the specified superclass does not exist,
    /// the superclass cannot be subclassed, or required abstract members are not overridden.
    /// </exception>
    /// <exception cref="ScriptError">
    /// Thrown if a field, property, method, or event declaration in the class definition conflicts with or
    /// improperly overrides a member in an ancestor class.
    /// </exception>
    private Class ValidateClassDefinition(ClassDefinition classDef)
    {
        if (IsRootItem(classDef.ClassName))
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
            case Modifier.Abstract when classDef.Modifier != Modifier.Abstract:
            {
                const MemberKind kind = MemberKind.Indexer | MemberKind.Property | MemberKind.Method;

                foreach (var member in superClass.GetMembers(kind))
                {
                    if (member.Modifier != Modifier.Abstract || IsPropertyAccessor(member)) continue;

                    var _override = classDef.GetMembers(kind).FirstOrDefault(m => m.Name == member.Name) ??
                        throw new RuntimeError(fileName, classDef, string.Format(Resources.MustOverride, classDef.ClassName, member.FullName));

                    if (_override.Modifier is Modifier.Abstract or Modifier.Static)
                        throw new ScriptError(fileName, _override, string.Format(Resources.InvalidMemberModifier, _override.Name));
                }
                break;
            }
        }

        foreach (var field in classDef.Fields)
        {
            var superField = superClass.GetField(field.Name);
            if (superField != null)
                throw new ScriptError(fileName, field, string.Format(Resources.FieldDeclaredInAncestor, field.Name, superField.Holder.Name));

            if (superClass.GetMember(field.Name, MemberKind.All & ~MemberKind.Field) != null)
                throw new ScriptError(fileName, field, string.Format(Resources.FieldHidesHomonymous, field.Name));
        }

        foreach (var property in classDef.Properties)
        {
            if (superClass.GetMember(property.Name, MemberKind.All & ~MemberKind.Property) != null)
                throw new ScriptError(fileName, property, string.Format(Resources.PropertyHidesHomonymous, property.Name));

            var overriden = superClass.GetProperty(property.Name);
            if (overriden == null) continue;

            if (overriden.Modifier is Modifier.Final or Modifier.Static)
                throw new ScriptError(fileName, property, string.Format(Resources.MemberCantOverride, overriden.FullName));

            if (!property.MatchesSignature(overriden))
                throw new ScriptError(fileName, property, string.Format(Resources.MustMatchSignature, property.Name, overriden.FullName));
        }

        foreach (var method in classDef.Methods)
        {
            if (superClass.GetMember(method.Name, MemberKind.All & ~MemberKind.Method) != null)
                throw new ScriptError(fileName, method, string.Format(Resources.MethodHidesHomonymous, method.Name));

            var overriden = superClass.GetMethod(method.Name);
            if (overriden == null) continue;

            if (overriden.Modifier is Modifier.Final or Modifier.Static)
                throw new ScriptError(fileName, method, string.Format(Resources.MemberCantOverride, overriden.FullName));

            //Note: I'm not sure if it's wise to check this!
            if (!method.MatchesSignature(overriden))
                throw new ScriptError(fileName, method, string.Format(Resources.MustMatchSignature, method.Name, overriden.FullName));
        }

        foreach (var _event in classDef.Events)
        {
            var superEvent = superClass.GetEvent(_event.Name);
            if (superEvent != null)
                throw new ScriptError(fileName, _event,
                    string.Format(Resources.EventDeclaredInAncestor, _event.Name, superEvent.Holder.Name));

            if (superClass.GetMember(_event.Name, MemberKind.All & ~MemberKind.Event) != null)
                throw new ScriptError(fileName, _event, string.Format(Resources.EventHidesHomonymous, _event.Name));
        }

        return superClass;
    }

    /// <summary>
    /// Checks if the specified member is a property accessor (reader or writer).
    /// </summary>
    /// <param name="member">The member to check</param>
    /// <returns><b>true</b> if the member is a property accessor, <b>false</b> otherwise</returns>
    private static bool IsPropertyAccessor(ClassMember member)
    {
        var accessorNamePattern = StringUtil.ToRegex(@"^__(read|write)_(\w+)$");
        return member is ClassMethod && accessorNamePattern.IsMatch(member.Name);
    }

    /// <summary>
    /// Initializes the static fields of a class.
    /// </summary>
    /// <param name="klass">A class</param>
    private void InitializeFields(Class klass)
    {
        foreach (var field in klass.Fields.Where(f => f.IsStatic && f.Initializer != null))
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
            foreach (var field in klass.Fields.Where(f => !f.IsStatic))
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
    /// Determines whether a member is accessible in the current context or not.
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
                                                            !(ctx.MethodHolder.Inherits(member.Holder) || member.IsOverriden)),
            _ => false
        };

        if (violation)
            throw new RuntimeError(fileName, astNode, string.Format(
                Resources.AccessDenied, member.FullName, ctx.MethodHolder?.Name ?? "global scope"));
    }

    /// <summary>
    /// Initializes the properties of an instance with the given set of <see cref="VariableSetter"/>s.
    /// </summary>
    /// <param name="astNode">The AST node that from witch the properties are getting initialized</param>
    /// <param name="target">The object that's being initialized</param>
    /// <param name="propertySetters">The given set of property setters</param>
    private void ApplyPropertySetters(AstNode astNode, DataItem target, VariableSetter[] propertySetters)
    {
        DataItem savedValue = returnedValue;

        foreach (VariableSetter setter in propertySetters)
        {
            var member = target.Class.GetMember(setter.Name, MemberKind.Field | MemberKind.Property);
            if (member != null)
            {
                CheckAccess(member, astNode);
                if (member.Modifier == Modifier.StaticFinal)
                    throw new ScriptError(fileName, setter, Resources.CannotWriteFinalField);
            }

            setter.Value.AcceptTranslator(this);
            DataItem propValue = returnedValue;

            switch (member)
            {
                case ClassProperty { CanWrite: false }:
                    throw new ScriptError(fileName, setter, Resources.CannotWriteProperty);
                case ClassProperty property:
                    CheckAccess(property.Writer, setter.Value);
                    Invoke(property.Writer.Function, property.Name, property.Holder, target, new Literal(propValue));
                    break;
                default:
                    target.SetProperty(setter.Name, propValue);
                    break;
            }
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
                                           attribute.Fields);
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
        typeInfo.SetProperty("__helper", new Resource(this));
        typeInfo.SetProperty("__modifier", new String(klass.Modifier.ToString()));
        typeInfo.SetProperty("__name", new String(klass.Name));
        typeInfo.SetProperty("__constructor", GetMethodInfo(klass.Constructor));
        typeInfo.SetProperty("__indexer", indexerInfo);
        typeInfo.SetProperty("__fields", GetFieldInfoMap(klass));
        typeInfo.SetProperty("__properties", GetPropertyInfoMap(klass));
        typeInfo.SetProperty("__methods", GetMethodInfoMap(klass));
        typeInfo.SetProperty("__events", GetEventInfoMap(klass));
        typeInfo.SetProperty("__attributes", GetAttributeList(klass.Attributes));
        typeInfo.SetProperty("__isIntegral", Boolean.FromBool(klass.IsIntegral));
        typeInfo.SetProperty("__isNumeric", Boolean.FromBool(klass.IsNumeric));
        typeInfo.SetProperty("__isTemporal", Boolean.FromBool(klass.IsTemporal));
        typeInfo.SetProperty("__isSequential", Boolean.FromBool(klass.IsSequential));
        typeInfo.SetProperty("__isCollection", Boolean.FromBool(klass.IsCollection));
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
        memberInfo.SetProperty("__helper", new Resource(this));
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

    /// <summary>
    /// Retrieves the class definition associated with the specified type information.
    /// </summary>
    /// <param name="typeInfo">The type information used to identify and locate the corresponding class. Cannot be null.</param>
    /// <returns>The class definition that matches the provided type information.</returns>
    private Class GetClass(DataItem typeInfo) =>
        (Class)rootFrame.RootBlock.GetItem(GetName(typeInfo));

    #endregion

    #region .NET Interop Management

    /// <summary>
    /// Changes the extension of a library's name so that it matches the host platform standards.
    /// </summary>
    /// <param name="libraryName">A library's name</param>
    /// <returns>A <see cref="string"/></returns>
    private static string WithNativeLibraryExtension(string libraryName)
    {
        libraryName = Path.GetFileNameWithoutExtension(libraryName);
        if (OperatingSystem.IsWindows()) return $"{libraryName}.dll";
        if (OperatingSystem.IsMacOS()) return $"{libraryName}.dynlib";
        return $"{libraryName}.so"; // Unix|Linux
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
    private void InvokeNative(Type type, string methodName, object target, Argument[] arguments)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                   BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

        object result;
        object[] nativeArgValues;
        var (argValues, argItems) = ExpandArguments(arguments ?? []);

        if (type.IsCOMObject)
        {
            nativeArgValues = [.. argValues.Select(val => val.AsNativeObject)];
            result = type.InvokeMember(methodName, flags, null, target, nativeArgValues);
        }
        else
        {
            MethodInfo matchedMethod = DataItemBinder.FindMethod(type, methodName, argValues, flags) ??
                throw new MissingMethodException(type.FullName, methodName);

            ParameterInfo[] parameters = matchedMethod.GetParameters();
            bool[] isParamOut = [.. parameters.Select(p => p.IsOut || p.IsRetval)];

            for (int i = 0; i < isParamOut.Length; ++i)
                if (isParamOut[i] && argItems[i].Spread)
                    throw new ScriptError(fileName, argItems[i], Resources.InvalidLValue);

            nativeArgValues = [.. argValues.Select((val, i) => val.ConvertTo(parameters[i].ParameterType))];
            result = matchedMethod.Invoke(target, nativeArgValues);

            for (int i = 0; i < isParamOut.Length; ++i)
                if (isParamOut[i])
                    Assign(argItems[i].Value, DataItemFactory.CreateDataItem(nativeArgValues[i]));
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
    /// Determines whether the specified match case is satisfied for the given expression.
    /// </summary>
    /// <param name="matchCase">The match case to evaluate, including its pattern and optional guard condition.</param>
    /// <param name="arg">The expression to test against the match case pattern.</param>
    /// <returns><b>true</b> if the expression matches the pattern and the guard condition (if any); <b>false</b> otherwise.</returns>
    private bool IsMatch(MatchCase matchCase, Expression arg)
    {
        var (pattern, guard) = (matchCase.Pattern, matchCase.Guard);

        try
        {
            return IsTrue(pattern.GetMatchTest(arg)) && (guard == null || IsTrue(guard));
        }
        catch (ScriptError se)
        {
            throw se.LocatedAt(pattern); // Assuming any error from the guard has its location correctly set
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

            return enumerated.InstanceOf(Class.Object)
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
    /// <param name="target">The object to iterate on</param>
    /// <param name="node">The <see cref="AstNode"/> from which <paramref name="target"/> is obtained</param>
    /// <returns>An <see cref="IEnumerable{T}"/></returns>
    private IEnumerable<(DataItem, DataItem)> GetProgrammaticEnumerable(DataItem target, AstNode node)
    {
        Class klass = target.Class;
        ClassMethod iteratorMethod = klass.GetMethod("iterator");
        int counter = 0;

        if (iteratorMethod != null)
        {
            CheckAccess(iteratorMethod, node);
            Invoke(iteratorMethod.Function, iteratorMethod.Name, iteratorMethod.Holder, target);

            foreach (DataItem item in yieldedValues)
                yield return (new Integer(counter++), item);

            yieldedValues.Clear();
        }
        else
        {
            ClassMethod moveFirstMethod = klass.GetMethod("moveFirst") ??
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, klass.Name));
            CheckAccess(moveFirstMethod, node);

            ClassMethod hasNextMethod = klass.GetMethod("hasNext") ??
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, klass.Name));
            CheckAccess(hasNextMethod, node);

            ClassMethod moveNextMethod = klass.GetMethod("moveNext") ??
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, klass.Name));
            CheckAccess(moveNextMethod, node);

            Invoke(moveFirstMethod.Function, moveFirstMethod.Name, moveFirstMethod.Holder, target);
            Invoke(hasNextMethod.Function, hasNextMethod.Name, hasNextMethod.Holder, target);

            while (returnedValue.AsBoolean)
            {
                Invoke(moveNextMethod.Function, moveNextMethod.Name, moveNextMethod.Holder, target);
                yield return (new Integer(counter++), returnedValue);

                Invoke(hasNextMethod.Function, hasNextMethod.Name, hasNextMethod.Holder, target);
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
        HashSet<string> directories = [..InitialContext.ImportPaths];
        var executable = Assembly.GetEntryAssembly();
        if (executable != null) directories.Add(executable.Location);
        if (!string.IsNullOrEmpty(fileName)) directories.Add(Path.GetDirectoryName(fileName));

        string scriptPath = null;
        foreach (var directory in directories)
        {
            scriptPath = Path.Combine(directory, scriptName.ToFilePath() + ".add");
            if (File.Exists(scriptPath)) break;
            scriptPath = null;
        }

        if (scriptPath == null) return false;
        if (importedModules.Contains(scriptPath)) return true;

        using var reader = new StreamReader(scriptPath);
        var program = new Parser(new Lexer(reader)).Program();
        var savedState = GetState();

        Reset(scriptPath);
        program.AcceptTranslator(this);
        RestoreState(savedState);
        importedModules.Add(scriptPath);

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
        List<Type> types = [];

        foreach (var assembly in InitialContext.References)
            foreach (var type in assembly.GetExportedTypes())
            {
                if (type.FullName == dottedName)
                {
                    CacheType(type, _namespace, aliasDefined ? alias : type.Name);
                    return true;
                }

                if (type.FullName?.StartsWith(prefix) ?? false)
                    types.Add(type);
            }

        if (types.Count == 0) return false;

        if (aliasDefined)
            foreach (var type in types)
                CacheType(type, _namespace, alias);
        else
            foreach (var type in types)
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

        var (frameItem, _) = FindFrameItem(name[0].ToString());
        if (frameItem is Class klass)
            switch (name.Length)
            {
                case 1:
                    return klass;
                case 2:
                {
                    var member = klass.GetMember(name[1].ToString());
                    if (member == null) return null;

                    CheckAccess(member, statement);

                    return member.IsStatic
                         ? member
                         : throw new RuntimeError(fileName, statement, string.Format(Resources.NonStaticMember, member.FullName));
                }
                default:
                    return null;
            }

        foreach (var assembly in InitialContext.References)
            for (var k = name.Length; k > 0; --k)
            {
                var type = assembly.GetType(name.Subname(0, k).ToDottedName(true));
                if (type == null) continue;

                CacheType(type);
                return nameCache[name];
            }

        return OperatingSystem.IsWindows()
             ? Type.GetTypeFromProgID(name.ToDottedName(true))
             : null;
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
                type = type.MakeGenericType([.. type.GetGenericArguments()
                                                    .Select(t => typeof(DataItem))]);
            }
            catch
            {
                return;
            }

        nameCache.Add(newName, type);

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        foreach (var name in type.GetMembers(flags).Select(member => member.Name))
        {
            QualifiedName memberName = newName.Apppend(new NamePart(name));
            if (nameCache.Contains(memberName)) continue;
            nameCache.Add(memberName, new StaticTypeMember(type, name));
        }

        foreach (var nestedType in type.GetNestedTypes())
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
        if (owner == null) return;

        indexer = (ClassProperty)owner.Class.GetMember(ClassProperty.INDEXER_NAME, MemberKind.Indexer);
        if (indexer != null) CheckAccess(indexer, itemRef);
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
        if (owner == null) return;

        const MemberKind memberKinds = MemberKind.Field | MemberKind.Property | MemberKind.Method;
        member = owner.Class.GetMember(propertyRef.PropertyName, memberKinds);
        if (member != null) CheckAccess(member, propertyRef);
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
    /// Expands a list potentially containing arguments with the spread operator to its full contents.
    /// </summary>
    /// <param name="arguments">The list of arguments that should be expanded</param>
    /// <returns>A (DataItem[], ListItem[]) tuple</returns>
    /// <exception cref="ScriptError">The evaluation of an argument failed</exception>
    private (DataItem[], Argument[]) ExpandArguments(Argument[] arguments)
    {
        List<DataItem> values = [];
        List<Argument> valueArgs = [];

        foreach (var argument in arguments)
        {
            try
            {
                if (argument.Spread)
                    foreach (var (_, value) in GetEnumerable(argument.Value))
                    {
                        values.Add(value);
                        valueArgs.Add(argument);
                    }
                else
                {
                    argument.Value.AcceptTranslator(this);
                    values.Add(returnedValue);
                    valueArgs.Add(argument);
                }
            }
            catch (ScriptError)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScriptError(fileName, argument, ex);
            }
        }

        return ([.. values], [.. valueArgs]);
    }

    #endregion
}