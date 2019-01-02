using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Rational : Dynamic
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

        public static Dynamic Simplify(Rational32 rational)
        {
            rational = rational.Simplify();
            return rational.Denominator == 1
                 ? new Integer(rational.Numerator)
                 : (Dynamic) new Rational(rational);
        }

        public Rational Opposite
        {
            get { return new Rational(-value); }
        }

        public Dynamic Inverse
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

        protected override bool UnsafeEquals(Dynamic other)
        {
            return value == other.AsRational32;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
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
                    return Simplify(value + operand.AsRational32);
                case BinaryOperator.Minus:
                    return Simplify(value - operand.AsRational32);
                case BinaryOperator.Times:
                    return Simplify(value * operand.AsRational32);
                case BinaryOperator.Divide:
                    return Simplify(value / operand.AsRational32);
                case BinaryOperator.Power:
                    return new Rational(value.Power(operand.AsInt32));
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }
    }
}
