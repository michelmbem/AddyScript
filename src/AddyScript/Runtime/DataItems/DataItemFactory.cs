using System;
using System.Collections.Generic;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;

namespace AddyScript.Runtime.DataItems;


public static class DataItemFactory
{
    public static DataItem CreateDataItem(object value)
    {
        if (value == null) return Void.Value;
        if (value is DataItem dyna) return dyna;
        if (value is Enum) return new String(value.ToString());
        if (value is char[] chars) return new String(new string(chars));
        if (value is byte[] bytes) return new String(StringUtil.ByteArray2String(bytes));
        if (value is Array array)
        {
            var list = new List<DataItem>();

            foreach (object item in array)
                list.Add(CreateDataItem(item));

            return new List(list);
        }

        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.Boolean:
                return Boolean.FromBool(Convert.ToBoolean(value));
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
                return new Integer(Convert.ToInt32(value));
            case TypeCode.UInt32:
            case TypeCode.Int64:
                return new Long(Convert.ToInt64(value));
            case TypeCode.UInt64:
                return new Long(Convert.ToUInt64(value));
            case TypeCode.Single:
            case TypeCode.Double:
                return new Float(Convert.ToDouble(value));
            case TypeCode.Decimal:
                return new Decimal(Convert.ToDecimal(value));
            case TypeCode.DateTime:
                return new Date(Convert.ToDateTime(value));
            case TypeCode.Char:
            case TypeCode.String:
                return new String(Convert.ToString(value));
            case TypeCode.Object:
                if (value is BigInteger bigint)
                    return new Long(bigint);
                if (value is Rational32 rational)
                    return new Rational(rational);
                if (value is BigDecimal bigdec)
                    return new Decimal(bigdec);
                if (value is Complex64 complex)
                    return new Complex(complex);
                if (value is List<DataItem> list)
                    return new List(list);
                if (value is Dictionary<DataItem, DataItem> dict)
                    return new Map(dict);
                if (value is HashSet<DataItem> set)
                    return new Set(set);
                if (value is Queue<DataItem> queue)
                    return new Queue(queue);
                if (value is Stack<DataItem> stack)
                    return new Stack(stack);
                if (value is Tuple<Class, Dictionary<string, DataItem>> couple)
                    return new Object(couple.Item1, couple.Item2);
                if (value is Function function)
                    return new Closure(function);
                return new Resource(value);
            default:
                return new Resource(value);
        }
    }
}
