using System;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Complex : DataItem
{
    private readonly Complex64 value;

    public Complex(Complex64 value) => this.value = value;

    public Complex(double realPart, double imaginaryPart) => value = new (realPart, imaginaryPart);

    public Complex(double realPart) => value = new (realPart, 0);

    public Complex Opposite => new (-value);

    public Complex Conjugate => new (Complex64.Conjugate(value));

    public override Class Class => Class.Complex;

    public override bool AsBoolean => value != Complex64.Zero;

    public override Complex64 AsComplex64 => value;

    public override object AsNativeObject => value;

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        string realString = value.Real != 0.0 ? value.Real.ToString(format) : null;
        string imaginaryString = value.Imaginary switch
        {
            -1.0 => "-i",
            0.0 => "0",
            1.0 => "i",
            _ => value.Imaginary.ToString(format) + "i",
        };

        if (realString == null) return imaginaryString;
        if (imaginaryString == "0") return realString;

        return value.Imaginary < 0.0
             ? $"({realString}{imaginaryString})"
             : $"({realString}+{imaginaryString})";
    }

    protected override bool UnsafeEquals(DataItem other) => value == other.AsComplex64;

    public override int GetHashCode() => value.GetHashCode();

    public override DataItem UnaryOperation(UnaryOperator _operator) => _operator switch
    {
        UnaryOperator.Plus => this,
        UnaryOperator.Minus => Opposite,
        _ => base.UnaryOperation(_operator),
    };

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Plus => new Complex(value + operand.AsComplex64),
        BinaryOperator.Minus => new Complex(value - operand.AsComplex64),
        BinaryOperator.Times => new Complex(value * operand.AsComplex64),
        BinaryOperator.Divide => new Complex(value / operand.AsComplex64),
        BinaryOperator.Power => new Complex(Complex64.Pow(value, operand.AsComplex64)),
        _ => base.BinaryOperation(_operator, operand),
    };

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "real" => new Float(value.Real),
        "imag" => new Float(value.Imaginary),
        _ => base.GetProperty(propertyName),
    };
}
