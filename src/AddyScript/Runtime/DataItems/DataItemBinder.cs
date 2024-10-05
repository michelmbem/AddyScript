using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

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
             ? Mismatch(resource.AsNativeObject.GetType(), t)
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
            case ClassID.Long or ClassID.Decimal:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                    TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                    TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal => 2,
                    TypeCode.String => 3,
                    _ => t == typeof(BigInteger) ? 1 : 4,
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
                    _ => t.IsEnum || t == typeof(char[]) ? 2 : 4,
                };
            case ClassID.Blob:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.String => 3,
                    _ => t == typeof(byte[]) ? 0 : 4,
                };
            case ClassID.Tuple or ClassID.List:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.String => 3,
                    _ => t.IsArray || t.IsAssignableTo(typeof(IList)) || t.IsAssignableTo(typeof(ITuple)) ? 1
                       : t.IsAssignableTo(typeof(IEnumerable)) ? 2 : 4,
                };
            case ClassID.Set:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.String => 3,
                    _ => t.IsArray || t.IsAssignableTo(typeof(ISet<>)) ? 1 : t.IsAssignableTo(typeof(IEnumerable)) ? 2 : 4,
                };
            case ClassID.Queue or ClassID.Stack:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.String => 3,
                    _ => t.IsArray || t.IsAssignableTo(typeof(ICollection)) ? 1 : t.IsAssignableTo(typeof(IEnumerable)) ? 2 : 4,
                };
            case ClassID.Map:
                return Type.GetTypeCode(t) switch
                {
                    TypeCode.String => 3,
                    _ => t.IsAssignableTo(typeof(IDictionary)) ? 1 : t.IsAssignableTo(typeof(IEnumerable)) ? 2 : 4,
                };
            case ClassID.Object or ClassID.Resource:
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

        return Type.GetTypeCode(t1) switch
        {
            TypeCode.Empty => Type.GetTypeCode(t2) switch
            {
                TypeCode.Empty => 0,
                TypeCode.Object or TypeCode.String or TypeCode.DBNull => 1,
                _ => 4,
            },
            TypeCode.DBNull => Type.GetTypeCode(t2) switch
            {
                TypeCode.DBNull => 0,
                _ => 1,
            },
            TypeCode.Boolean => Type.GetTypeCode(t2) switch
            {
                TypeCode.Boolean => 0,
                TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or
                TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
                TypeCode.Single or TypeCode.Double or TypeCode.Decimal => 1,
                TypeCode.String => 3,
                _ => 4,
            },
            TypeCode.Int32 => Type.GetTypeCode(t2) switch
            {
                TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.Single => 2,
                TypeCode.Int32 => 0,
                TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double or TypeCode.Decimal => 1,
                TypeCode.String => 3,
                _ => 4,
            },
            TypeCode.Double => Type.GetTypeCode(t2) switch
            {
                TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                TypeCode.UInt64 or TypeCode.Single or TypeCode.Decimal => 2,
                TypeCode.Double => 0,
                TypeCode.String => 3,
                _ => 4,
            },
            TypeCode.Decimal => Type.GetTypeCode(t2) switch
            {
                TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                TypeCode.UInt64 or TypeCode.Single or TypeCode.Double => 2,
                TypeCode.Decimal => 0,
                TypeCode.String => 3,
                _ => 4,
            },
            TypeCode.DateTime => Type.GetTypeCode(t2) switch
            {
                TypeCode.DateTime => 0,
                TypeCode.String => 3,
                _ => 4,
            },
            TypeCode.String => Type.GetTypeCode(t2) switch
            {
                TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
                TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or
                TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal or
                TypeCode.DateTime => 2,
                TypeCode.String => 0,
                _ => 4,
            },
            TypeCode.Object => Type.GetTypeCode(t2) switch
            {
                TypeCode.String => 3,
                _ => t1.IsAssignableTo(t2) ? 1 : 4,
            },
            _ => 4,
        };
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