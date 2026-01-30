using System;
using System.Numerics;
using SysComplex = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Decimal(BigDecimal value) : DataItem
{
    public Decimal Opposite => new (-value);

    public override Class Class => Class.Decimal;

    public override bool AsBoolean => value.ToBoolean(null);

    public override int AsInt32 => (int)value;

    public override BigInteger AsBigInteger => (BigInteger)value;

    public override Fraction AsFraction => value;

    public override double AsDouble => (double)value;

    public override BigDecimal AsBigDecimal => value;

    public override SysComplex AsComplex => new ((double)value, 0);

    public override object AsNativeObject => value;

    public override string ToString(string format, IFormatProvider formatProvider) =>
        value.ToString(format, formatProvider);

    protected override bool UnsafeEquals(DataItem other) => value == other.AsBigDecimal;

    public override int GetHashCode() => value.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other) => value.CompareTo(other.AsBigDecimal);

    public override DataItem UnaryOperation(UnaryOperator _operator) => _operator switch
    {
        UnaryOperator.Plus => this,
        UnaryOperator.Minus => Opposite,
        _ => base.UnaryOperation(_operator),
    };

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Plus => new Decimal(value + operand.AsBigDecimal),
        BinaryOperator.Minus => new Decimal(value - operand.AsBigDecimal),
        BinaryOperator.Times => new Decimal(value * operand.AsBigDecimal),
        BinaryOperator.Divide => new Decimal(value / operand.AsBigDecimal),
        BinaryOperator.Modulo => new Decimal(value % operand.AsBigDecimal),
        BinaryOperator.Power => new Decimal(value.Pow(operand.AsInt32)),
        BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsBigDecimal),
        BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsBigDecimal),
        BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsBigDecimal),
        BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsBigDecimal),
        _ => base.BinaryOperation(_operator, operand),
    };
}
