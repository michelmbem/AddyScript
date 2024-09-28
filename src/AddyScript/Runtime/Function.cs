using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Translators;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.Frames;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime;


/// <summary>
/// Represents a function's definition.
/// </summary>
/// <remarks>
/// Initializes a new instance of Function.
/// </remarks>
/// <param name="parameters">The function's parameters</param>
/// <param name="body">The function's body</param>
public class Function(Parameter[] parameters, Block body) : IFrameItem
{
    /// <summary>
    /// Represents the empty function (one that does nothing).
    /// </summary>
    public static readonly Function Empty = new ([], Block.Return());

    /// <summary>
    /// Maps method names to corresponding instances of <see cref="Function"/>.
    /// </summary>
    public static readonly Dictionary<string, Function> Map = [];

    private readonly Dictionary<Type, Delegate> delegateCache = [];
    private readonly Dictionary<string, IFrameItem> capturedItems = [];
    private MethodFrame declaringFrame;

    /// <summary>
    /// Gets the kind of frame item a function is.
    /// </summary>
    public FrameItemKind Kind => FrameItemKind.Function;

    /// <summary>
    /// The parameters of this function.
    /// </summary>
    public Parameter[] Parameters { get; private set; } = parameters;

    /// <summary>
    /// The body of this function if it is user defined.
    /// </summary>
    public Block Body { get; private set; } = body;

    /// <summary>
    /// The function's attributes.
    /// </summary>
    public DataItem[] Attributes { get; set; }

    /// <summary>
    /// For a function declared inline, holds a reference to the parent function's frame if any.
    /// </summary>
    public MethodFrame DeclaringFrame
    {
        get => declaringFrame;
        set
        {
            declaringFrame = value;
            capturedItems.Clear();

            if (value == null) return;

            foreach (string name in value.GetNames())
                capturedItems.Add(name, value.GetItem(name));
        }
    }

    /// <summary>
    /// Gets a reference to the items that were present in
    /// the parent function's frame when this function was created.
    /// </summary>
    public Dictionary<string, IFrameItem> CapturedItems => capturedItems;

    /// <summary>
    /// The minimum number of arguments required by a call to this function.
    /// </summary>
    public int MinNumArgs
    {
        get
        {
            for (int i = 0; i < Parameters.Length; ++i)
                if (Parameters[i].Optional) return i;

            return Parameters.Length;
        }
    }

    /// <summary>
    /// The maximum number of arguments required by a call to this function.
    /// </summary>
    public int MaxNumArgs
    {
        get
        {
            if (Parameters.Length > 0 && Parameters[Parameters.Length - 1].VaArgs)
                return int.MaxValue;

            return Parameters.Length;
        }
    }

    /// <summary>
    /// Updates the captured items after any call.
    /// </summary>
    /// <param name="frameItems">The items of the frame that was allocated for the function's call</param>
    public void UpdateCapturedItems(Dictionary<string, IFrameItem> frameItems)
    {
        foreach (var pair in frameItems)
            if (capturedItems.ContainsKey(pair.Key))
                capturedItems[pair.Key] = pair.Value;
    }

    /// <summary>
    /// Generates a delegate that exposes this function to .Net classes.
    /// </summary>
    /// <param name="delegateType">The type of delegate to generate</typeparam>
    /// <returns>
    /// A <see cref="Delegate"/> that invokes <i>RuntimeServices.Interpreter.Invoke(
    ///     [this-function],
    ///     [automatically-generated-method-name],
    ///     RuntimeServices.Interpreter.currentFrame.MethodHolder,
    ///     RuntimeServices.Interpreter.currentFrame.MethodTarget,
    ///     [an-array-of-expressions]
    /// )</i>.
    /// Each expression at index <i>i</i> in the 5th argument is obtained by invoking
    /// <i>new Literal(DataItemFactory.CreateDataItem(<paramref name="delegateType"/>.GetParameters()[i]))</i>.
    /// The delegate returns <i>RuntimeServices.Interpreter.returnedValue.ConvertTo(RuntimeTypeHandle
    /// .GetTypeFromHandle(<paramref name="delegateType"/>.GetMethod("Invoke").ReturnType))</i>
    /// </returns>
    public Delegate ToDelegate(Type delegateType)
    {
        if (delegateCache.TryGetValue(delegateType, out Delegate value))
            return value;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Static | BindingFlags.Instance;

        MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
        ParameterInfo[] parameters = invokeMethod.GetParameters();

        var parameterTypes = new Type[parameters.Length];
        for (int i = 0; i < parameters.Length; ++i)
            parameterTypes[i] = parameters[i].ParameterType;

        string methodName = "__" + Guid.NewGuid().ToString("N");
        Map.Add(methodName, this);

        Type funType = typeof(Function),
             rsType = typeof(RuntimeServices),
             interType = typeof(Interpreter),
             frameType = typeof(MethodFrame),
             ctxType = typeof(InvocationContext),
             resType = typeof(Resource),
             metaType = typeof(Type),
             mapType = Map.GetType(),
             returnType = invokeMethod.ReturnType;

        var method = new DynamicMethod(methodName, returnType, parameterTypes, interType);
        ILGenerator il = method.GetILGenerator();

        il.DeclareLocal(funType); // Function var0;
        il.DeclareLocal(typeof(Expression[])); // Expression[] var1;
        
        // var0 = Function.Map[methodName];
        il.Emit(OpCodes.Ldsfld, funType.GetField("Map", flags));
        il.Emit(OpCodes.Ldstr, methodName);
        il.Emit(OpCodes.Callvirt, mapType.GetMethod("get_Item", flags));
        il.Emit(OpCodes.Stloc_0);

        MethodInfo getInterpreterMethod = rsType.GetMethod("get_Interpreter");

        // push RuntimeServices.Interpreter on top of the stack
        il.Emit(OpCodes.Call, getInterpreterMethod);

        // push var0 (this function) on top of the stack
        il.Emit(OpCodes.Ldloc_0);

        // push methodName on top of the stack
        il.Emit(OpCodes.Ldstr, methodName);

        FieldInfo currentFrameField = interType.GetField("currentFrame", flags);
        MethodInfo getContextMethod = frameType.GetMethod("get_Context", flags);

        // push RuntimeServices.Interpreter.currentFrame.Context.MethodHolder on top of the stack
        il.Emit(OpCodes.Call, getInterpreterMethod);
        il.Emit(OpCodes.Ldfld, currentFrameField);
        il.Emit(OpCodes.Callvirt, getContextMethod);
        il.Emit(OpCodes.Callvirt, ctxType.GetMethod("get_MethodHolder", flags));

        // push RuntimeServices.Interpreter.currentFrame.Context.MethodTarget on top of the stack
        il.Emit(OpCodes.Call, getInterpreterMethod);
        il.Emit(OpCodes.Ldfld, currentFrameField);
        il.Emit(OpCodes.Callvirt, getContextMethod);
        il.Emit(OpCodes.Callvirt, ctxType.GetMethod("get_MethodTarget", flags));

        // var1 = new Expression[parameters.Length]
        il.Emit(OpCodes.Ldc_I4, parameters.Length);
        il.Emit(OpCodes.Newarr, typeof(Expression));
        il.Emit(OpCodes.Stloc_1);

        MethodInfo factoryMethod = typeof(DataItemFactory).GetMethod("CreateDataItem", new[] { typeof(object) });
        ConstructorInfo literalCtor = typeof(Literal).GetConstructor(new[] { resType });

        // populate the array with dynamically created literals
        for (int i = 0; i < parameters.Length; ++i)
        {
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i);
            if (parameterTypes[i].IsValueType)
                il.Emit(OpCodes.Box, parameterTypes[i]);
            il.Emit(OpCodes.Call, factoryMethod);
            il.Emit(OpCodes.Newobj, literalCtor);
            il.Emit(OpCodes.Stelem_Ref);
        }

        // we need to clearly identify which overload of Interpreter.Invoke we want to call
        var interInvokeArgTypes = new[] { funType, typeof(string), typeof(Class), typeof(DataItem), typeof(Expression[]) };

        // push the array on top of the stack, then call the Interpreter's Invoke method
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Callvirt, interType.GetMethod("Invoke", flags, null, interInvokeArgTypes, null));

        // eventually push RuntimeServices.CurrentInterpreter.returnedValue on top of the stack
        if (returnType != typeof(void))
        {
            il.Emit(OpCodes.Call, getInterpreterMethod);
            il.Emit(OpCodes.Ldfld, interType.GetField("returnedValue", flags));
            il.Emit(OpCodes.Ldtoken, returnType);
            il.Emit(OpCodes.Call, metaType.GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) }));
            il.Emit(OpCodes.Callvirt, typeof(DataItem).GetMethod("ConvertTo", new[] { metaType }));
            if (returnType.IsValueType) il.Emit(OpCodes.Unbox_Any, returnType);
        }

        // add a return statement
        il.Emit(OpCodes.Ret);

        for (int i = 0; i < parameters.Length; ++i)
            method.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);

        var _delegate = method.CreateDelegate(delegateType);
        delegateCache.Add(delegateType, _delegate);
        
        return _delegate;
    }
}