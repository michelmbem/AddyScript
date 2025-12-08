using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Rational : DataItem
{
    private readonly Rational32 value;

    public Rational(Rational32 value) => this.value = value;

    public Rational(int numerator, int denominator) => value = new Rational32(numerator, denominator);

    public Rational(int numerator) => value = new Rational32(numerator);

    public static DataItem Simplify(Rational32 rational)
    {
        rational = rational.Simplify();
        return rational.Denominator == 1 ? new Integer(rational.Numerator) : new Rational(rational);
    }

    public Rational Opposite => new (-value);

    public DataItem Inverse => Simplify(value.Inverse());

    public override Class Class => Class.Rational;

    public override bool AsBoolean => value != Rational32.Zero;

    public override int AsInt32 => (int)value;

    public override BigInteger AsBigInteger => new ((int)value);

    public override Rational32 AsRational32 => value;

    public override double AsDouble => (double)value;

    public override BigDecimal AsBigDecimal => new ((decimal)value);

    public override Complex64 AsComplex64 => new ((double)value, 0);

    public override object AsNativeObject => value;

    public override string ToString(string format, IFormatProvider formatProvider)
        => value.ToString(format, formatProvider);

    protected override bool UnsafeEquals(DataItem other) => value == other.AsRational32;

    public override int GetHashCode() => value.GetHashCode();

    public override DataItem UnaryOperation(UnaryOperator _operator) => _operator switch
    {
        UnaryOperator.Plus => this,
        UnaryOperator.Minus => Opposite,
        _ => base.UnaryOperation(_operator),
    };

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Plus => Simplify(value + operand.AsRational32),
        BinaryOperator.Minus => Simplify(value - operand.AsRational32),
        BinaryOperator.Times => Simplify(value * operand.AsRational32),
        BinaryOperator.Divide => Simplify(value / operand.AsRational32),
        BinaryOperator.Power => new Rational(value.Power(operand.AsInt32)),
        BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsRational32),
        BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsRational32),
        BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsRational32),
        BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsRational32),
        _ => base.BinaryOperation(_operator, operand),
    };

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "num" => new Integer(value.Numerator),
        "den" => new Integer(value.Denominator),
        _ => base.GetProperty(propertyName),
    };
}
