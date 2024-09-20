using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Decimal(BigDecimal value) : DataItem
    {
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

        protected override bool UnsafeEquals(DataItem other)
        {
            return value == other.AsBigDecimal;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(DataItem other)
        {
            return value.CompareTo(other.AsBigDecimal);
        }

        public override DataItem UnaryOperation(UnaryOperator _operator)
        {
            return _operator switch
            {
                UnaryOperator.Plus => this,
                UnaryOperator.Minus => Opposite,
                _ => base.UnaryOperation(_operator),
            };
        }

        public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
        {
            return _operator switch
            {
                BinaryOperator.Plus => new Decimal(value + operand.AsBigDecimal),
                BinaryOperator.Minus => new Decimal(value - operand.AsBigDecimal),
                BinaryOperator.Times => new Decimal(value * operand.AsBigDecimal),
                BinaryOperator.Divide => new Decimal(value / operand.AsBigDecimal),
                BinaryOperator.Modulo => new Decimal(value % operand.AsBigDecimal),
                BinaryOperator.Power => new Decimal(value.Power(operand.AsInt32)),
                BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsBigDecimal),
                BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsBigDecimal),
                BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsBigDecimal),
                BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsBigDecimal),
                _ => base.BinaryOperation(_operator, operand),
            };
        }
    }
}
