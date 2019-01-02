using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Float : Dynamic
    {
        private readonly double value;

        public Float(double value)
        {
            this.value = value;
        }

        public Float Opposite
        {
            get { return new Float(-value); }
        }

        public override Class Class
        {
            get { return Class.Float; }
        }

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

        public override double AsDouble
        {
            get { return value; }
        }

        public override BigDecimal AsBigDecimal
        {
            get { return new BigDecimal(value); }
        }

        public override Complex64 AsComplex64
        {
            get { return new Complex64(value, 0); }
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

        protected override int UnsafeCompareTo(Dynamic other)
        {
            return value.CompareTo(other.AsDouble);
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
                    return new Float(value + operand.AsDouble);
                case BinaryOperator.Minus:
                    return new Float(value - operand.AsDouble);
                case BinaryOperator.Times:
                    return new Float(value * operand.AsDouble);
                case BinaryOperator.Divide:
                    return new Float(value / operand.AsDouble);
                case BinaryOperator.Modulo:
                    return new Float(MathUtil.Modulo(value, operand.AsDouble));
                case BinaryOperator.Power:
                    return new Float(Math.Pow(value, operand.AsDouble));
                case BinaryOperator.LessThan:
                    return Boolean.FromBool(value < operand.AsDouble);
                case BinaryOperator.LessThanOrEqual:
                    return Boolean.FromBool(value <= operand.AsDouble);
                case BinaryOperator.GreaterThan:
                    return Boolean.FromBool(value > operand.AsDouble);
                case BinaryOperator.GreaterThanOrEqual:
                    return Boolean.FromBool(value >= operand.AsDouble);
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }
    }
}
