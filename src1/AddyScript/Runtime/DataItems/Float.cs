using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.Utilities;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Float(double value) : DataItem
    {
        public Float Opposite => new(-value);

        public override Class Class => Class.Float;

        public override bool AsBoolean
        {
            get { return value != 0.0; }
        }

        public override int AsInt32
        {
            get { return (int) value; }
        }

        public override BigInteger AsBigInteger
        {
            get { return new BigInteger(value); }
        }

        public override Rational32 AsRational32
        {
            get { return new Rational32((int) value); }
        }

        public override double AsDouble => value;

        public override BigDecimal AsBigDecimal
        {
            get { return new BigDecimal(value); }
        }

        public override Complex64 AsComplex64
        {
            get { return new Complex64(value, 0); }
        }

        public override object AsNativeObject => value;

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return value.ToString(format, formatProvider);
        }

        protected override bool UnsafeEquals(DataItem other)
        {
            double x = other.AsDouble;

            if (double.IsNaN(x))
                return double.IsNaN(value);
            if (double.IsPositiveInfinity(x))
                return double.IsPositiveInfinity(value);
            if (double.IsNegativeInfinity(x))
                return double.IsNegativeInfinity(value);

            return value == x;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(DataItem other)
        {
            return value.CompareTo(other.AsDouble);
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
                BinaryOperator.Plus => new Float(value + operand.AsDouble),
                BinaryOperator.Minus => new Float(value - operand.AsDouble),
                BinaryOperator.Times => new Float(value * operand.AsDouble),
                BinaryOperator.Divide => new Float(value / operand.AsDouble),
                BinaryOperator.Modulo => new Float(MathUtil.Modulo(value, operand.AsDouble)),
                BinaryOperator.Power => new Float(Math.Pow(value, operand.AsDouble)),
                BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsDouble),
                BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsDouble),
                BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsDouble),
                BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsDouble),
                _ => base.BinaryOperation(_operator, operand),
            };
        }
    }
}
