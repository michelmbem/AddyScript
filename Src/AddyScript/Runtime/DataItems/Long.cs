using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Long(BigInteger value) : DataItem
    {
        public Long Opposite
        {
            get { return new Long(-value); }
        }

        public Long Complement
        {
            get { return new Long(~value); }
        }

        public override Class Class
        {
            get { return Class.Long; }
        }

        public override bool AsBoolean
        {
            get { return value != BigInteger.Zero; }
        }

        public override int AsInt32
        {
            get { return (int) value; }
        }

        public override BigInteger AsBigInteger
        {
            get { return value; }
        }

        public override Rational32 AsRational32
        {
            get { return new Rational32((int) value); }
        }

        public override double AsDouble
        {
            get { return (double) value; }
        }

        public override BigDecimal AsBigDecimal
        {
            get { return new BigDecimal(value); }
        }

        public override Complex64 AsComplex64
        {
            get { return new Complex64((double) value, 0); }
        }

        public override object AsNativeObject
        {
            get { return value; }
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return value.ToString(format, formatProvider);
        }

        protected override bool UnsafeEquals(DataItem other)
        {
            return value == other.AsBigInteger;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(DataItem other)
        {
            return value.CompareTo(other.AsBigInteger);
        }

        public override object ConvertTo(Type targetType)
        {
            return targetType.IsEnum
                 ? Convert.ChangeType((long) value, targetType)
                 : base.ConvertTo(targetType);
        }

        public override DataItem UnaryOperation(UnaryOperator _operator)
        {
            return _operator switch
            {
                UnaryOperator.Plus => this,
                UnaryOperator.Minus => Opposite,
                UnaryOperator.BitwiseNot => Complement,
                _ => base.UnaryOperation(_operator),
            };
        }

        public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
        {
            return _operator switch
            {
                BinaryOperator.Plus => new Long(value + operand.AsBigInteger),
                BinaryOperator.Minus => new Long(value - operand.AsBigInteger),
                BinaryOperator.Times => new Long(value * operand.AsBigInteger),
                BinaryOperator.Divide => new Long(value / operand.AsBigInteger),
                BinaryOperator.Modulo => new Long(value % operand.AsBigInteger),
                BinaryOperator.Power => new Long(BigInteger.Pow(value, operand.AsInt32)),
                BinaryOperator.And => new Long(value & operand.AsBigInteger),
                BinaryOperator.Or => new Long(value | operand.AsBigInteger),
                BinaryOperator.ExclusiveOr => new Long(value ^ operand.AsBigInteger),
                BinaryOperator.ShiftLeft => new Long(value << operand.AsInt32),
                BinaryOperator.ShiftRight => new Long(value >> operand.AsInt32),
                BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsBigInteger),
                BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsBigInteger),
                BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsBigInteger),
                BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsBigInteger),
                _ => base.BinaryOperation(_operator, operand),
            };
        }
    }
}
