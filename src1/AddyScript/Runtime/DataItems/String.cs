using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Properties;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.Utilities;
using AddyScript.Runtime.OOP;

namespace AddyScript.Runtime.DataItems
{
    public sealed class String(string value) : DataItem
    {
        public override Class Class => Class.String;

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

        protected override bool UnsafeEquals(DataItem other)
        {
            return value == other.ToString();
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(DataItem other)
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

        public override bool IsEmpty()
        {
            return value.Length <= 0;
        }

        public override bool ConversionNeeded(Class targetClass, BinaryOperator _operator)
        {
            return false;
        }

        public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
        {
            return _operator switch
            {
                BinaryOperator.Plus => new String(value + operand),
                BinaryOperator.Times => new String(StringUtil.Repeat(value, operand.AsInt32)),
                BinaryOperator.LessThan => Boolean.FromBool(string.Compare(value, operand.ToString()) < 0),
                BinaryOperator.LessThanOrEqual => Boolean.FromBool(string.Compare(value, operand.ToString()) <= 0),
                BinaryOperator.GreaterThan => Boolean.FromBool(string.Compare(value, operand.ToString()) > 0),
                BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(string.Compare(value, operand.ToString()) >= 0),
                BinaryOperator.StartsWith => Boolean.FromBool(value.StartsWith(operand.ToString())),
                BinaryOperator.EndsWith => Boolean.FromBool(value.EndsWith(operand.ToString())),
                BinaryOperator.Contains => Boolean.FromBool(value.Contains(operand.ToString())),
                BinaryOperator.Matches => Boolean.FromBool(StringUtil.GetRegex(operand.ToString()).IsMatch(value)),
                _ => base.BinaryOperation(_operator, operand),
            };
        }

        public override DataItem GetItem(DataItem index)
        {
            int n = index.AsInt32, l = value.Length;
            if (n >= l) return null;
            while (n < 0) n += l;
            return new String(value[n].ToString());
        }

        public override void SetItem(DataItem index, DataItem d)
        {
            throw new InvalidOperationException(Resources.StringsAreImmutable);
        }

        public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
        {
            for (int i = 0; i < value.Length; ++i)
            {
                var key = new Integer(i);
                var val = new String(value[i].ToString());
                yield return new KeyValuePair<DataItem, DataItem>(key, val);
            }
        }
    }
}
