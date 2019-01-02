using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Compilers;
using AddyScript.Runtime.Dynamics;
using AddyScript.Runtime.Frames;


namespace AddyScript.Runtime
{
    /// <summary>
    /// Represents a function's definition.
    /// </summary>
    public class Function : IFrameItem
    {
        /// <summary>
        /// Represents the empty function (one that does nothing).
        /// </summary>
        public static readonly Function Empty = new Function(Parameter.EmptyArray, Block.Return());

        /// <summary>
        /// Maps method names to corresponding instances of <see cref="Function"/>.
        /// </summary>
        public static readonly Dictionary<string, Function> FunctionMap = new Dictionary<string, Function>();

        private readonly Dictionary<Type, Delegate> delegateCache = new Dictionary<Type, Delegate>();
        private readonly Dictionary<string, IFrameItem> capturedItems = new Dictionary<string, IFrameItem>();
        private MethodFrame declaringFrame;
        
        /// <summary>
        /// Initializes a new instance of Function.
        /// </summary>
        /// <param name="parameters">The function's parameters</param>
        /// <param name="body">The function's body</param>
        public Function(Parameter[] parameters, Block body)
        {
            Parameters = parameters;
            Body = body;
        }

        /// <summary>
        /// Gets the kind of this frame's item.
        /// </summary>
        public FrameItemKind Kind
        {
            get { return FrameItemKind.Function; }
        }

        /// <summary>
        /// The parameters of this function.
        /// </summary>
        public Parameter[] Parameters { get; private set; }

        /// <summary>
        /// The body of this function if it is user defined.
        /// </summary>
        public Block Body { get; private set; }

        /// <summary>
        /// The functions's attributes.
        /// </summary>
        public Attribute[] Attributes { get; set; }

        /// <summary>
        /// For a closure, the frame in which the closure is declared.
        /// </summary>
        public MethodFrame DeclaringFrame
        {
            get { return declaringFrame; }
            set
            {
                declaringFrame = value;
                capturedItems.Clear();
                if (value != null)
                    foreach (string name in value.GetNames())
                        capturedItems.Add(name, value.GetItem(name));
            }
        }

        /// <summary>
        /// Gets a reference to the items that were present in
        /// the declaring frame when this Function was created.
        /// </summary>
        public Dictionary<string, IFrameItem> CapturedItems
        {
            get { return capturedItems; }
        }

        /// <summary>
        /// The minimum number of arguments required by a call to this function.
        /// </summary>
        public int MinNumArgs
        {
            get
            {
                for (int i = 0; i < Parameters.Length; ++i)
                    if (Parameters[i].VaArgs ||
                        Parameters[i].DefaultValue != null)
                        return i;

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
                if (Parameters.Length > 0 &&
                    Parameters[Parameters.Length - 1].VaArgs)
                    return int.MaxValue;

                return Parameters.Length;
            }
        }

        /// <summary>
        /// Verifies that this function has the same signature than another.
        /// </summary>
        /// <param name="other">The other function</param>
        /// <returns>A boolean</returns>
        public bool MatchesSignature(Function other)
        {
            if (Parameters.Length != other.Parameters.Length)
                return false;

            for (int i = 0; i < Parameters.Length; ++i)
            {
                Parameter p1 = Parameters[i];
                Parameter p2 = other.Parameters[i];

                if (p1.ByRef != p2.ByRef ||
                    p1.VaArgs != p2.VaArgs ||
                    (p1.DefaultValue == null && p2.DefaultValue != null) ||
                    (p1.DefaultValue != null && p2.DefaultValue == null))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets an annotation in the list by its name.
        /// </summary>
        /// <param name="name">The name of an attribute</param>
        /// <returns><see cref="Attribute"/></returns>
        public Attribute GetAttribute(string name)
        {
            if (Attributes != null)
                foreach (Attribute attribute in Attributes)
                    if (attribute.Name == name)
                        return attribute;

            return null;
        }

        /// <summary>
        /// Updates the captured items after any call.
        /// </summary>
        /// <param name="frameItems">The current value of captured items</param>
        public void UpdateCapturedItems(Dictionary<string, IFrameItem> frameItems)
        {
            foreach (KeyValuePair<string, IFrameItem> pair in frameItems)
                if (capturedItems.ContainsKey(pair.Key))
                    capturedItems[pair.Key] = pair.Value;
        }

        /// <summary>
        /// Generates a delegate that exposes this function to .Net classes.
        /// </summary>
        /// <param name="delegateType">The target delegate's type</param>
        /// <returns>A <see cref="Delegate"/></returns>
        public Delegate ToDelegate(Type delegateType)
        {
            if (delegateCache.ContainsKey(delegateType))
                return delegateCache[delegateType];

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Static | BindingFlags.Instance;

            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            ParameterInfo[] parameters = invokeMethod.GetParameters();

            var parameterTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; ++i)
                parameterTypes[i] = parameters[i].ParameterType;

            string methodName = "__" + Guid.NewGuid().ToString("N");
            FunctionMap.Add(methodName, this);

            Type fnType = typeof(Function),
                 rsType = typeof(RuntimeServices),
                 interType = typeof(Interpreter),
                 frameType = typeof(MethodFrame),
                 ctxType = typeof(CallContext),
                 resType = typeof(Resource),
                 metaType = typeof(Type),
                 fnMapType = FunctionMap.GetType(),
                 returnType = invokeMethod.ReturnType;

            var method = new DynamicMethod(methodName, returnType, parameterTypes, interType);
            ILGenerator il = method.GetILGenerator();

            il.DeclareLocal(fnType); // Function var0;
            il.DeclareLocal(typeof(Expression[])); // Expression[] var1;
            
            // var0 = Function.FunctionMap[methodName];
            il.Emit(OpCodes.Ldsfld, fnType.GetField("FunctionMap", flags));
            il.Emit(OpCodes.Ldstr, methodName);
            il.Emit(OpCodes.Callvirt, fnMapType.GetMethod("get_Item", flags));
            il.Emit(OpCodes.Stloc_0);

            MethodInfo getInterpreterMethod = rsType.GetMethod("get_Interpreter");

            // push RuntimeServices.CurrentInterpreter on top of the stack
            il.Emit(OpCodes.Call, getInterpreterMethod);

            // push var0 (this function) on top of the stack
            il.Emit(OpCodes.Ldloc_0);

            // push methodName on top of the stack
            il.Emit(OpCodes.Ldstr, methodName);

            FieldInfo currentFrameField = interType.GetField("currentFrame", flags);
            MethodInfo getCallContextMethod = frameType.GetMethod("get_Context", flags);

            // push RuntimeServices.Interpreter.currentFrame.CallContext.Self on top of the stack
            il.Emit(OpCodes.Call, getInterpreterMethod);
            il.Emit(OpCodes.Ldfld, currentFrameField);
            il.Emit(OpCodes.Callvirt, getCallContextMethod);
            il.Emit(OpCodes.Callvirt, ctxType.GetMethod("get_Self", flags));

            // push RuntimeServices.Interpreter.currentFrame.CallContext.This on top of the stack
            il.Emit(OpCodes.Call, getInterpreterMethod);
            il.Emit(OpCodes.Ldfld, currentFrameField);
            il.Emit(OpCodes.Callvirt, getCallContextMethod);
            il.Emit(OpCodes.Callvirt, ctxType.GetMethod("get_This", flags));

            // var1 = new Expression[parameters.Length]
            il.Emit(OpCodes.Ldc_I4, parameters.Length);
            il.Emit(OpCodes.Newarr, typeof(Expression));
            il.Emit(OpCodes.Stloc_1);

            MethodInfo factoryMethod = typeof(DynamicFactory).GetMethod("CreateDynamic", new[] { typeof(object) });
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

            // push the array on top of the stack, then call the Interpreter's Invoke method
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Callvirt, interType.GetMethod("Invoke", flags));

            // eventually push RuntimeServices.CurrentInterpreter.returnedValue on top of the stack
            if (returnType != typeof(void))
            {
                il.Emit(OpCodes.Call, getInterpreterMethod);
                il.Emit(OpCodes.Ldfld, interType.GetField("returnedValue", flags));
                il.Emit(OpCodes.Ldtoken, returnType);
                il.Emit(OpCodes.Call, metaType.GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) }));
                il.Emit(OpCodes.Callvirt, typeof(Dynamic).GetMethod("ConvertTo", new[] { metaType }));
                if (returnType.IsValueType) il.Emit(OpCodes.Unbox_Any, returnType);
            }

            // add a return statement
            il.Emit(OpCodes.Ret);

            for (int i = 0; i < parameters.Length; ++i)
                method.DefineParameter(i + 1,
                                       parameters[i].Attributes,
                                       parameters[i].Name);

            Delegate _delegate = method.CreateDelegate(delegateType);
            delegateCache.Add(delegateType, _delegate);
            
            return _delegate;
        }
    }
}