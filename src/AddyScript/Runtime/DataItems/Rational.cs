using System;
using System.Numerics;
using SysComplex = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Rational : DataItem
{
    private readonly Fraction value;

    public Rational(Fraction value) => this.value = value;

    public Rational(BigInteger numerator, BigInteger denominator) =>
        value = new Fraction(numerator, denominator);

    public Rational(BigInteger numerator) => value = new Fraction(numerator);

    public Rational Opposite => new (-value);

    public DataItem Inverse => new Rational(value.Inverse());

    public override Class Class => Class.Rational;

    public override bool AsBoolean => value.ToBoolean(null);

    public override int AsInt32 => (int)value;

    public override BigInteger AsBigInteger => (BigInteger)value;

    public override Fraction AsFraction => value;

    public override double AsDouble => (double)value;

    public override BigDecimal AsBigDecimal => (BigDecimal)value;

    public override SysComplex AsComplex => new ((double)value, 0);

    public override object AsNativeObject => value;

    public override string ToString(string format, IFormatProvider formatProvider) =>
        value.ToString(format, formatProvider);

    protected override bool UnsafeEquals(DataItem other) => value == other.AsFraction;

    public override int GetHashCode() => value.GetHashCode();

    public override DataItem UnaryOperation(UnaryOperator _operator) => _operator switch
    {
        UnaryOperator.Plus => this,
        UnaryOperator.Minus => Opposite,
        _ => base.UnaryOperation(_operator),
    };

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Plus => new Rational(value + operand.AsFraction),
        BinaryOperator.Minus => new Rational(value - operand.AsFraction),
        BinaryOperator.Times => new Rational(value * operand.AsFraction),
        BinaryOperator.Divide => new Rational(value / operand.AsFraction),
        BinaryOperator.Power => new Rational(value.Power(operand.AsInt32)),
        BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsFraction),
        BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsFraction),
        BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsFraction),
        BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsFraction),
        _ => base.BinaryOperation(_operator, operand),
    };

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "num" => new Long(value.Numerator),
        "den" => new Long(value.Denominator),
        _ => base.GetProperty(propertyName),
    };
}
