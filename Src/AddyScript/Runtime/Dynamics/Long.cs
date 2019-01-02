using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Long : Dynamic
    {
        private readonly BigInteger value;

        public Long(BigInteger value)
        {
            this.value = value;
        }

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

        protected override bool UnsafeEquals(Dynamic other)
        {
            return value == other.AsBigInteger;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(Dynamic other)
        {
            return value.CompareTo(other.AsBigInteger);
        }

        public override object ConvertTo(Type targetType)
        {
            return targetType.IsEnum
                 ? Convert.ChangeType((long) value, targetType)
                 : base.ConvertTo(targetType);
        }

        public override Dynamic UnaryOperation(UnaryOperator _operator)
        {
            switch (_operator)
            {
                case UnaryOperator.Plus:
                    return this;
                case UnaryOperator.Minus:
                    return Opposite;
                case UnaryOperator.BitwiseNot:
                    return Complement;
                default:
                    return base.UnaryOperation(_operator);
            }
        }

        public override Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                    return new Long(value + operand.AsBigInteger);
                case BinaryOperator.Minus:
                    return new Long(value - operand.AsBigInteger);
                case BinaryOperator.Times:
                    return new Long(value * operand.AsBigInteger);
                case BinaryOperator.Divide:
                    return new Long(value / operand.AsBigInteger);
                case BinaryOperator.Modulo:
                    return new Long(value % operand.AsBigInteger);
                case BinaryOperator.Power:
                    return new Long(BigInteger.Pow(value, operand.AsInt32));
                case BinaryOperator.And:
                    return new Long(value & operand.AsBigInteger);
                case BinaryOperator.Or:
                    return new Long(value | operand.AsBigInteger);
                case BinaryOperator.ExclusiveOr:
                    return new Long(value ^ operand.AsBigInteger);
                case BinaryOperator.ShiftLeft:
                    return new Long(value << operand.AsInt32);
                case BinaryOperator.ShiftRight:
                    return new Long(value >> operand.AsInt32);
                case BinaryOperator.LessThan:
                    return Boolean.FromBool(value < operand.AsBigInteger);
                case BinaryOperator.LessThanOrEqual:
                    return Boolean.FromBool(value <= operand.AsBigInteger);
                case BinaryOperator.GreaterThan:
                    return Boolean.FromBool(value > operand.AsBigInteger);
                case BinaryOperator.GreaterThanOrEqual:
                    return Boolean.FromBool(value >= operand.AsBigInteger);
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }
    }
}
