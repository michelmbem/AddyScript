using System;
using System.Collections;
using System.Globalization;
using System.Reflection;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


/// <summary>
/// A custom binder for our calls to Type.InvokeMember.
/// </summary>
public class DataItemBinder : Binder
{
    #region Public Static Methods

    public static MethodInfo FindMethod(Type type, string methodName, DataItem[] args, BindingFlags flags)
    {
        int index = -1, minimum = int.MaxValue;
        MemberInfo[] members = type.GetMember(methodName, MemberTypes.Method, flags);

        for (int i = 0; i < members.Length; ++i)
        {
            var method = (MethodInfo) members[i];
            ParameterInfo[] parameters = method.GetParameters();
            if (args.Length > parameters.Length) continue;

            int mismatch = 0, j = 0;
            for (; j < args.Length; ++j)
                mismatch += Mismatch(args[j], parameters[j].ParameterType);
            for (; j < parameters.Length; ++j)
                mismatch += 4; // 4 is the maximum value of DynamicBinder.Mismatch
            if (mismatch >= minimum) continue;

            index = i;
            minimum = mismatch;
            if (minimum == 0) break;
        }

        return index < 0 ? null : (MethodInfo) members[index];
    }

    #endregion

    #region Overrides

    public override MethodBase BindToMethod(
        BindingFlags bindingAttr,
        MethodBase[] match,
        ref object[] args,
        ParameterModifier[] modifiers,
        CultureInfo culture,
        string[] names,
        out object state)
    {
        state = new BinderState { args = args };

        int index = -1, minimum = int.MaxValue;

        for (int i = 0; i < match.Length; ++i)
        {
            ParameterInfo[] parameters = match[i].GetParameters();
            if (args.Length > parameters.Length) continue;

            int mismatch = 0, j = 0;
            for (; j < args.Length; ++j)
                mismatch += Mismatch((DataItem)args[j], parameters[j].ParameterType);
            for (; j < parameters.Length; ++j)
                mismatch += 4;
            if (mismatch >= minimum) continue;

            index = i;
            minimum = mismatch;
            if (minimum == 0) break;
        }

        if (index < 0) return null;

        ParameterInfo[] matchParams = match[index].GetParameters();
        var values = new object[args.Length];
        for (int i = 0; i < args.Length; ++i)
            values[i] = ChangeType(args[i], matchParams[i].ParameterType, culture);
        args = values;

        return match[index];
    }

    public override FieldInfo BindToField(
        BindingFlags bindingAttr,
        FieldInfo[] match,
        object value,
        CultureInfo culture)
    {
        return match.Length > 0 ? match[0] : null;
    }

    public override MethodBase SelectMethod(
        BindingFlags bindingAttr,
        MethodBase[] match,
        Type[] types,
        ParameterModifier[] modifiers)
    {
        int minimum = int.MaxValue, index = -1;

        for (int i = 0; i < match.Length; ++i)
        {
            ParameterInfo[] parameters = match[i].GetParameters();
            if (types.Length > parameters.Length) continue;

            int mismatch = 0, j = 0;
            for (; j < types.Length; ++j)
                mismatch += Mismatch(types[j], parameters[j].ParameterType);
            for (; j < parameters.Length; ++j)
                mismatch += 4;

            if (mismatch == 0) return match[i];
            if (mismatch >= minimum) continue;

            minimum = mismatch;
            index = i;
        }

        return match[index];
    }

    public override PropertyInfo SelectProperty(
        BindingFlags bindingAttr,
        PropertyInfo[] match,
        Type returnType,
        Type[] indexes,
        ParameterModifier[] modifiers)
    {
        int minimum = int.MaxValue, index = -1;

        for (int i = 0; i < match.Length; ++i)
        {
            ParameterInfo[] parameters = match[i].GetIndexParameters();
            if (indexes.Length > parameters.Length) continue;

            int mismatch = 0, j = 0;
            for (; j < indexes.Length; ++j)
                mismatch += Mismatch(indexes[j], parameters[j].ParameterType);
            for (; j < parameters.Length; ++j)
                mismatch += 4;

            if (mismatch == 0) return match[i];
            if (mismatch >= minimum) continue;

            minimum = mismatch;
            index = i;
        }

        return match[index];
    }

    public override object ChangeType(object value, Type type, CultureInfo culture)
    {
        return value is DataItem dataItem
             ? dataItem.ConvertTo(type)
             : Convert.ChangeType(value, type);
    }

    public override void ReorderArgumentArray(ref object[] args, object state)
    {
        args = ((BinderState)state).args;
    }

    #endregion

    #region Mismatch Method

    /// <summary>
    /// Determines the degree of incompatibility between an AddyScript variable and a native type.
    /// </summary>
    /// <param name="v">An AddyScript variable</param>
    /// <param name="t">A native CLR <see cref="Type"/></param>
    /// <returns>
    /// <ul>
    /// <li>0: full compatibility</li>
    /// <li>1: <paramref name="v"/> can be converted to <paramref name="t"/> without loss</li>
    /// <li>2: <paramref name="v"/> can be converted to <paramref name="t"/> with possible loss</li>
    /// <li>3: <paramref name="t"/> is the <see cref="string"/> type</li>
    /// <li>4: No possible conversion between both types</li>
    /// </ul>
    /// </returns>
    private static int Mismatch(DataItem v, Type t)
    {
        return v is Resource resource
             ? Mismatch(resource.NativeType, t)
             : Mismatch(v.Class, t);
    }

    /// <summary>
    /// Determines the degree of incompatibility between an AddyScript type and a native type.
    /// </summary>
    /// <param name="c">An AddyScript <see cref="Class"/></param>
    /// <param name="t">A native CLR <see cref="Type"/></param>
    /// <returns>
    /// <ul>
    /// <li>0: full compatibility</li>
    /// <li>1: <paramref name="c"/> can be converted to <paramref name="t"/> without loss</li>
    /// <li>2: <paramref name="c"/> can be converted to <paramref name="t"/> with possible loss</li>
    /// <li>3: <paramref name="t"/> is the <see cref="string"/> type</li>
    /// <li>4: No possible conversion between both types</li>
    /// </ul>
    /// </returns>
    private static int Mismatch(Class c, Type t)
    {
        if (t == typeof(object)) return 1;

        switch (c.ClassID)
        {
            case ClassID.Void:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.Empty => 0,
                    TypeCode.Object or TypeCode.String or TypeCode.DBNull => 1,
                    _ => 4,
                };
            case ClassID.Boolean:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.Boolean => 0,
                    TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or
                    TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
                    TypeCode.Single or TypeCode.Double or TypeCode.Decimal => 1,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case ClassID.Integer:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                    TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.Single => 2,
                    TypeCode.Int32 => 0,
                    TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double or TypeCode.Decimal => 1,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case ClassID.Float:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                    TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                    TypeCode.UInt64 or TypeCode.Single or TypeCode.Decimal => 2,
                    TypeCode.Double => 0,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case ClassID.Long:
            case ClassID.Decimal:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                    TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                    TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal => 2,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case ClassID.Date:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.DateTime => 0,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case ClassID.String:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                    TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                    TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal or
                    TypeCode.DateTime or TypeCode.Char => 2,
                    TypeCode.String => 0,
                    TypeCode.Object => t.IsEnum || t == typeof(char[]) || t == typeof(byte[]) ? 2 : 4,
                    _ => 4,
                };
            case ClassID.List:
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Object:
                        if (t.IsArray || t == typeof(IList) || t == typeof(ICollection) ||
                            t == typeof(IEnumerable)) return 1;
                        return 4;
                    case TypeCode.String:
                        return 3;
                    default:
                        return 4;
                }
            case ClassID.Map:
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Object:
                        if (t == typeof(IDictionary) || t == typeof(ICollection) ||
                            t == typeof(IEnumerable)) return 1;
                        return 4;
                    case TypeCode.String:
                        return 3;
                    default:
                        return 4;
                }
            case ClassID.Set:
            case ClassID.Queue:
            case ClassID.Stack:
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Object:
                        if (t == typeof(ICollection) || t == typeof(IEnumerable)) return 1;
                        return 4;
                    case TypeCode.String:
                        return 3;
                    default:
                        return 4;
                }
            case ClassID.Object:
            case ClassID.Resource:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.DBNull or TypeCode.Object => 1,
                    TypeCode.String => 3,
                    _ => 4,
                };
            default:
                return 4;
        }
    }

    /// <summary>
    /// Determines the degree of incompatibility between two native types.
    /// </summary>
    /// <param name="t1">The source <see cref="Type"/></param>
    /// <param name="t2">The destination <see cref="Type"/></param>
    /// <returns>
    /// <ul>
    /// <li>0: full compatibility</li>
    /// <li>1: <paramref name="t1"/> can be converted to <paramref name="t2"/> without loss</li>
    /// <li>2: <paramref name="t1"/> can be converted to <paramref name="t2"/> with possible loss</li>
    /// <li>3: <paramref name="t2"/> is the <see cref="string"/> type</li>
    /// <li>4: No possible conversion between both types</li>
    /// </ul>
    /// </returns>
    private static int Mismatch(Type t1, Type t2)
    {
        if (t1 == t2) return 0;

        switch (Type.GetTypeCode(t1))
        {
            case TypeCode.Empty:
                return Type.GetTypeCode(t2) switch
                {
                    TypeCode.Empty => 0,
                    TypeCode.Object or TypeCode.String or TypeCode.DBNull => 1,
                    _ => 4,
                };
            case TypeCode.DBNull:
                return Type.GetTypeCode(t2) switch
                {
                    TypeCode.DBNull => 0,
                    _ => 1,
                };
            case TypeCode.Boolean:
                return Type.GetTypeCode(t2) switch
                {
                    TypeCode.Boolean => 0,
                    TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or
                    TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
                    TypeCode.Single or TypeCode.Double or TypeCode.Decimal => 1,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case TypeCode.Int32:
                return Type.GetTypeCode(t2) switch
                {
                    TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                    TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.Single => 2,
                    TypeCode.Int32 => 0,
                    TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double or TypeCode.Decimal => 1,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case TypeCode.Double:
                return Type.GetTypeCode(t2) switch
                {
                    TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                    TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                    TypeCode.UInt64 or TypeCode.Single or TypeCode.Decimal => 2,
                    TypeCode.Double => 0,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case TypeCode.Decimal:
                return Type.GetTypeCode(t2) switch
                {
                    TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                    TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                    TypeCode.UInt64 or TypeCode.Single or TypeCode.Double => 2,
                    TypeCode.Decimal => 0,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case TypeCode.DateTime:
                return Type.GetTypeCode(t2) switch
                {
                    TypeCode.DateTime => 0,
                    TypeCode.String => 3,
                    _ => 4,
                };
            case TypeCode.String:
                return Type.GetTypeCode(t2) switch
                {
                    TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                    TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                    TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal or
                    TypeCode.DateTime => 2,
                    TypeCode.String => 0,
                    _ => 4,
                };
            case TypeCode.Object:
                switch (Type.GetTypeCode(t2))
                {
                    case TypeCode.Object:
                        if (t1.IsSubclassOf(t2) ||
                           (t2.IsInterface && Array.IndexOf(t1.GetInterfaces(), t2) >= 0)) return 1;
                        return 4;
                    case TypeCode.String:
                        return 3;
                    default:
                        return 4;
                }
            default:
                return 4;
        }
    }

    #endregion

    #region Nested class : BinderState

    /// <summary>
    /// An utility type used to store the original arguments of a method.
    /// </summary>
    public class BinderState
    {
        public object[] args;
    }

    #endregion
}