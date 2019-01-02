using System;
using System.Collections.Generic;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Properties;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.Utilities;
using System.Text;

namespace AddyScript.Runtime.Dynamics
{
    public sealed class String : Dynamic
    {
        private readonly string value;

        public String(string value)
        {
            this.value = value;
        }

        public override Class Class
        {
            get { return Class.String; }
        }

        public override bool AsBoolean
        {
            get
            {
                if (string.Compare(value, Resources.FALSE, true) == 0)
                    return false;
                if (string.Compare(value, Resources.TRUE, true) == 0)
                    return true;
                return bool.Parse(value);
            }
        }

        public override int AsInt32
        {
            get { return int.Parse(value); }
        }

        public override BigInteger AsBigInteger
        {
            get { return BigInteger.Parse(value); }
        }

        public override double AsDouble
        {
            get { return double.Parse(value); }
        }

        public override BigDecimal AsBigDecimal
        {
            get { return new BigDecimal(value); }
        }

        public override DateTime AsDateTime
        {
            get { return DateTime.Parse(value); }
        }

        public override object AsNativeObject
        {
            get { return value; }
        }

        public override object Clone()
        {
            return new String(value);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            switch (format)
            {
                case "x":
                case "X":
                    {
                        var sb = new StringBuilder();
                        foreach (char c in value)
                            if (32 <= c && c < 127)
                                sb.Append(c);
                            else
                                sb.AppendFormat("\\x{0:x2}", (int)c);
                        return sb.ToString();
                    }
                default:
                    return value.ToString(formatProvider);
            }
        }

        protected override bool UnsafeEquals(Dynamic other)
        {
            return value == other.ToString();
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(Dynamic other)
        {
            return value.CompareTo(other.ToString());
        }

        public override object ConvertTo(Type targetType)
        {
            return targetType.IsEnum
                 ? Enum.Parse(targetType, value)
                 : targetType == typeof(char[])
                 ? value.ToCharArray()
                 : targetType == typeof(byte[])
                 ? StringUtil.String2ByteArray(value)
                 : base.ConvertTo(targetType);
        }

        public override bool ConversionNeeded(Class targetClass, BinaryOperator _operator)
        {
            return false;
        }

        public override Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                    return new String(value + operand);
                case BinaryOperator.Times:
                    return new String(StringUtil.Repeat(value, operand.AsInt32));
                case BinaryOperator.LessThan:
                    return Boolean.FromBool(string.Compare(value, operand.ToString()) < 0);
                case BinaryOperator.LessThanOrEqual:
                    return Boolean.FromBool(string.Compare(value, operand.ToString()) <= 0);
                case BinaryOperator.GreaterThan:
                    return Boolean.FromBool(string.Compare(value, operand.ToString()) > 0);
                case BinaryOperator.GreaterThanOrEqual:
                    return Boolean.FromBool(string.Compare(value, operand.ToString()) >= 0);
                case BinaryOperator.StartsWith:
                    return Boolean.FromBool(value.StartsWith(operand.ToString()));
                case BinaryOperator.EndsWith:
                    return Boolean.FromBool(value.EndsWith(operand.ToString()));
                case BinaryOperator.Contains:
                    return Boolean.FromBool(value.Contains(operand.ToString()));
                case BinaryOperator.Matches:
                    return Boolean.FromBool(StringUtil.GetRegex(operand.ToString()).IsMatch(value));
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }

        public override Dynamic GetItem(Dynamic index)
        {
            int n = index.AsInt32, l = value.Length;
            if (n >= l) return null;
            while (n < 0) n += l;
            return new String(value[n].ToString());
        }

        public override void SetItem(Dynamic index, Dynamic d)
        {
            throw new InvalidOperationException(Resources.StringsAreImmutable);
        }

        public override IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetEnumerable()
        {
            for (int i = 0; i < value.Length; ++i)
            {
                var key = new Integer(i);
                var val = new String(value[i].ToString());
                yield return new KeyValuePair<Dynamic, Dynamic>(key, val);
            }
        }
    }
}
