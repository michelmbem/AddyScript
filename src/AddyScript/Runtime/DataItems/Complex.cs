using System;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Complex : DataItem
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

        public Complex Opposite => new(-value);

        public Complex Conjugate
        {
            get { return new Complex(Complex64.Conjugate(value)); }
        }

        public override Class Class => Class.Complex;

        public override bool AsBoolean
        {
            get { return value != Complex64.Zero; }
        }

        public override Complex64 AsComplex64 => value;

        public override object AsNativeObject => value;

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

        protected override bool UnsafeEquals(DataItem other)
        {
            return value == other.AsComplex64;
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
                BinaryOperator.Plus => new Complex(value + operand.AsComplex64),
                BinaryOperator.Minus => new Complex(value - operand.AsComplex64),
                BinaryOperator.Times => new Complex(value * operand.AsComplex64),
                BinaryOperator.Divide => new Complex(value / operand.AsComplex64),
                BinaryOperator.Power => new Complex(Complex64.Pow(value, operand.AsComplex64)),
                _ => base.BinaryOperation(_operator, operand),
            };
        }
    }
}
