using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Complex : Dynamic
    {
        private readonly Complex64 value;

        public Complex(Complex64 value)
        {
            this.value = value;
        }

        public Complex(double realPart, double imagineryPart)
        {
            value = new Complex64(realPart, imagineryPart);
        }

        public Complex(double realPart)
        {
            value = new Complex64(realPart, 0);
        }

        public Complex Opposite
        {
            get { return new Complex(-value); }
        }

        public Complex Conjugate
        {
            get { return new Complex(Complex64.Conjugate(value)); }
        }

        public override Class Class
        {
            get { return Class.Complex; }
        }

        public override bool AsBoolean
        {
            get { return value != Complex64.Zero; }
        }

        public override Complex64 AsComplex64
        {
            get { return value; }
        }

        public override object AsNativeObject
        {
            get { return value; }
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            string realString = null, imaginaryString;

            if (value.Real != 0.0)
                realString = value.Real.ToString(format);

            if (value.Imaginary == -1.0)
                imaginaryString = "-i";
            else if (value.Imaginary == 0.0)
                imaginaryString = "0";
            else if (value.Imaginary == 1.0)
                imaginaryString = "i";
            else
                imaginaryString = value.Imaginary.ToString(format) + "i";

            if (realString == null) return imaginaryString;
            if (imaginaryString == "0") return realString;

            return value.Imaginary < 0.0
                ? string.Format("({0}{1})", realString, imaginaryString)
                : string.Format("({0}+{1})", realString, imaginaryString); 
        }

        protected override bool UnsafeEquals(Dynamic other)
        {
            return value == other.AsComplex64;
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
                    return new Complex(value + operand.AsComplex64);
                case BinaryOperator.Minus:
                    return new Complex(value - operand.AsComplex64);
                case BinaryOperator.Times:
                    return new Complex(value * operand.AsComplex64);
                case BinaryOperator.Divide:
                    return new Complex(value / operand.AsComplex64);
                case BinaryOperator.Power:
                    return new Complex(Complex64.Pow(value, operand.AsComplex64));
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }
    }
}
