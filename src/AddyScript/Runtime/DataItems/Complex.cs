using System;
using SysComplex = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.DataItems;


public sealed class Complex : DataItem
{
    private readonly SysComplex value;

    public Complex(SysComplex value) => this.value = value;

    public Complex(double realPart, double imaginaryPart = 0) =>
        value = new SysComplex(realPart, imaginaryPart);

    public Complex Opposite => new (-value);

    public Complex Conjugate => new (SysComplex.Conjugate(value));

    public override Class Class => Class.Complex;

    public override bool AsBoolean => value != SysComplex.Zero;

    public override SysComplex AsComplex => value;

    public override object AsNativeObject => value;

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        var realString = MathExt.Equal(value.Real, 0) ? null : value.Real.ToString(format);
        var imaginaryString = MathExt.Equal(value.Imaginary, 0) ? null : value.Imaginary.ToString(format) + 'i';

        if (realString == null) return imaginaryString ?? "0";
        if (imaginaryString == null) return realString;

        return value.Imaginary < 0.0
             ? $"({realString}{imaginaryString})"
             : $"({realString}+{imaginaryString})";
    }

    protected override bool UnsafeEquals(DataItem other) => value == other.AsComplex;

    public override int GetHashCode() => value.GetHashCode();

    public override DataItem UnaryOperation(UnaryOperator _operator) => _operator switch
    {
        UnaryOperator.Plus => this,
        UnaryOperator.Minus => Opposite,
        _ => base.UnaryOperation(_operator),
    };

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Plus => new Complex(value + operand.AsComplex),
        BinaryOperator.Minus => new Complex(value - operand.AsComplex),
        BinaryOperator.Times => new Complex(value * operand.AsComplex),
        BinaryOperator.Divide => new Complex(value / operand.AsComplex),
        BinaryOperator.Power => new Complex(SysComplex.Pow(value, operand.AsComplex)),
        _ => base.BinaryOperation(_operator, operand),
    };

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "real" => new Float(value.Real),
        "imag" => new Float(value.Imaginary),
        _ => base.GetProperty(propertyName),
    };
}
