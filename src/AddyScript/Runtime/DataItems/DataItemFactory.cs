using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Complex64 = System.Numerics.Complex;

using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public static class DataItemFactory
{
    public static DataItem CreateDataItem(object value)
    {
        if (value is Enum) return new String(value.ToString());

        return Type.GetTypeCode(value?.GetType()) switch
        {
            TypeCode.Empty => Void.Value,
            TypeCode.Boolean => Boolean.FromBool(Convert.ToBoolean(value)),
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
            TypeCode.UInt16 or TypeCode.Int32 => new Integer(Convert.ToInt32(value)),
            TypeCode.UInt32 or TypeCode.Int64 => new Long(Convert.ToInt64(value)),
            TypeCode.UInt64 => new Long(Convert.ToUInt64(value)),
            TypeCode.Single or TypeCode.Double => new Float(Convert.ToDouble(value)),
            TypeCode.Decimal => new Decimal(Convert.ToDecimal(value)),
            TypeCode.DateTime => new Date(Convert.ToDateTime(value)),
            TypeCode.Char or TypeCode.String => new String(Convert.ToString(value)),
            _ => value switch
            {
                DataItem dataItem => dataItem,
                BigInteger bigint => new Long(bigint),
                BigDecimal bigdec => new Decimal(bigdec),
                Fraction rational => new Rational(rational),
                Complex64 complex => new Complex(complex),
                TimeSpan timespan => new Duration(timespan),
                byte[] bytes => new Blob(bytes),
                DataItem[] items => new Tuple(items),
                List<DataItem> list => new List(list),
                HashSet<DataItem> set => new Set(set),
                Queue<DataItem> queue => new Queue(queue),
                Stack<DataItem> stack => new Stack(stack),
                Dictionary<DataItem, DataItem> dict => new Map(dict),
                Dictionary<string, DataItem> fields => new Object(Class.Object, fields),
                (Class klass, Dictionary<string, DataItem> fields) => new Object(klass, fields),
                Function function => new Closure(function),
                DateOnly dateOnly => new Date(dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local)),
                TimeOnly timeOnly => new Duration(timeOnly.ToTimeSpan()),
                char[] chars => new String(new string(chars)),
                ITuple tuple => new Tuple(GetItems(tuple)), // Order matters
                Array => new List(((IEnumerable<object>)value).Select(CreateDataItem)), // Order matters
                _ => new Resource(value),
            },
        };
    }

    private static DataItem[] GetItems(ITuple tuple)
    {
        var items = new DataItem[tuple.Length];

        for (int i = 0; i < tuple.Length; ++i)
            items[i] = CreateDataItem(tuple[i]);
        
        return items;
    }
}
