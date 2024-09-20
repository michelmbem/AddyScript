using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Rational : DataItem
    {
        private readonly Rational32 value;

        public Rational(Rational32 value)
        {
            this.value = value;
        }

        public Rational(int numerator, int denominator)
        {
            value = new Rational32(numerator, denominator);
        }

        public Rational(int numerator)
        {
            value = new Rational32(numerator);
        }

        public static DataItem Simplify(Rational32 rational)
        {
            rational = rational.Simplify();
            return rational.Denominator == 1 ? new Integer(rational.Numerator) : new Rational(rational);
        }

        public Rational Opposite
        {
            get { return new Rational(-value); }
        }

        public DataItem Inverse
        {
            get { return Simplify(value.Inverse()); }
        }

        public override Class Class
        {
            get { return Class.Rational; }
        }

        public override bool AsBoolean
        {
            get { return value != Rational32.Zero; }
        }

        public override int AsInt32
        {
            get { return (int) value; }
        }

        public override BigInteger AsBigInteger
        {
            get { return new BigInteger((int) value); }
        }

        public override Rational32 AsRational32
        {
            get { return value; }
        }

        public override double AsDouble
        {
            get { return (double) value; }
        }

        public override BigDecimal AsBigDecimal
        {
            get { return new BigDecimal((decimal) value); }
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
            return value == other.AsRational32;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
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
                BinaryOperator.Plus => Simplify(value + operand.AsRational32),
                BinaryOperator.Minus => Simplify(value - operand.AsRational32),
                BinaryOperator.Times => Simplify(value * operand.AsRational32),
                BinaryOperator.Divide => Simplify(value / operand.AsRational32),
                BinaryOperator.Power => new Rational(value.Power(operand.AsInt32)),
                _ => base.BinaryOperation(_operator, operand),
            };
        }
    }
}
