using System;
using System.Collections.Generic;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.Utilities;

namespace AddyScript.Runtime.Dynamics
{
    public static class DynamicFactory
    {
        public static Dynamic CreateDynamic(object value)
        {
            if (value == null) return Void.Value;
            if (value is Dynamic) return (Dynamic) value;
            if (value is Enum) return new String(value.ToString());
            if (value is char[]) return new String(new string((char[]) value));
            if (value is byte[]) return new String(StringUtil.ByteArray2String((byte[]) value));
            if (value is Array)
            {
                var array = (Array) value;
                var list = new List<Dynamic>();

                foreach (object item in array)
                    list.Add(CreateDynamic(item));

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
                    if (value is BigInteger)
                        return new Long((BigInteger) value);
                    if (value is Rational32)
                        return new Rational((Rational32) value);
                    if (value is BigDecimal)
                        return new Decimal((BigDecimal) value);
                    if (value is Complex64)
                        return new Complex((Complex64) value);
                    if (value is List<Dynamic>)
                        return new List((List<Dynamic>) value);
                    if (value is Dictionary<Dynamic, Dynamic>)
                        return new Map((Dictionary<Dynamic, Dynamic>) value);
                    if (value is HashSet<Dynamic>)
                        return new Set((HashSet<Dynamic>) value);
                    if (value is Queue<Dynamic>)
                        return new Queue((Queue<Dynamic>) value);
                    if (value is Stack<Dynamic>)
                        return new Stack((Stack<Dynamic>) value);
                    if (value is Tuple<Class, Dictionary<string, Dynamic>>)
                    {
                        var couple = (Tuple<Class, Dictionary<string, Dynamic>>) value;
                        return new Object(couple.Item1, couple.Item2);
                    }
                    if (value is Function)
                        return new Closure((Function) value);
                    return new Resource(value);
                default:
                    return new Resource(value);
            }
        }
    }
}
