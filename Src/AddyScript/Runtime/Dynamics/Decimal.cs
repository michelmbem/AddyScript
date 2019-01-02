using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Decimal : Dynamic
    {
        private readonly BigDecimal value;

        public Decimal(BigDecimal value)
        {
            this.value = value;
        }

        public Decimal Opposite
        {
            get { return new Decimal(-value); }
        }

        public override Class Class
        {
            get { return Class.Decimal; }
        }

        public override bool AsBoolean
        {
            get { return value != BigDecimal.Zero; }
        }

        public override int AsInt32
        {
            get { return (int) value; }
        }

        public override BigInteger AsBigInteger
        {
            get { return (BigInteger) value; }
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
            get { return value; }
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
            return value == other.AsBigDecimal;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(Dynamic other)
        {
            return value.CompareTo(other.AsBigDecimal);
        }

        public override Dynamic UnaryOperation(UnaryOperator _operator)
        {
            switch (_operator)
            {
                case UnaryOperator.Plus:
                    return this;
                case UnaryOperator.Minus:
                    return Opposite;
                default:
                    return base.UnaryOperation(_operator);
            }
        }

        public override Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                    return new Decimal(value + operand.AsBigDecimal);
                case BinaryOperator.Minus:
                    return new Decimal(value - operand.AsBigDecimal);
                case BinaryOperator.Times:
                    return new Decimal(value * operand.AsBigDecimal);
                case BinaryOperator.Divide:
                    return new Decimal(value / operand.AsBigDecimal);
                case BinaryOperator.Modulo:
                    return new Decimal(value % operand.AsBigDecimal);
                case BinaryOperator.Power:
                    return new Decimal(value.Power(operand.AsInt32));
                case BinaryOperator.LessThan:
                    return Boolean.FromBool(value < operand.AsBigDecimal);
                case BinaryOperator.LessThanOrEqual:
                    return Boolean.FromBool(value <= operand.AsBigDecimal);
                case BinaryOperator.GreaterThan:
                    return Boolean.FromBool(value > operand.AsBigDecimal);
                case BinaryOperator.GreaterThanOrEqual:
                    return Boolean.FromBool(value >= operand.AsBigDecimal);
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }
    }
}
