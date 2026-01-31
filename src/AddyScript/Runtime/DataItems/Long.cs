using System;
using System.Numerics;
using SysComplex = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Long(BigInteger value) : DataItem
{
    public Long Opposite => new (-value);

    public Long Complement => new (~value);

    public override Class Class => Class.Long;

    public override bool AsBoolean => !value.IsZero;

    public override int AsInt32 => (int)value;

    public override BigInteger AsBigInteger => value;

    public override Fraction AsFraction => value;

    public override double AsDouble => (double)value;

    public override BigDecimal AsBigDecimal => value;

    public override SysComplex AsComplex => new ((double)value, 0);

    public override object AsNativeObject => value;

    public override string ToString(string format, IFormatProvider formatProvider) =>
        value.ToString(format, formatProvider) ?? string.Empty;

    protected override bool UnsafeEquals(DataItem other) => value == other.AsBigInteger;

    public override int GetHashCode() => value.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other) => value.CompareTo(other.AsBigInteger);

    public override object ConvertTo(Type targetType) => targetType.IsEnum
         ? Convert.ChangeType((long)value, targetType)
         : base.ConvertTo(targetType);

    public override DataItem UnaryOperation(UnaryOperator _operator) => _operator switch
    {
        UnaryOperator.Plus => this,
        UnaryOperator.Minus => Opposite,
        UnaryOperator.BitwiseNot => Complement,
        _ => base.UnaryOperation(_operator),
    };

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Plus => new Long(value + operand.AsBigInteger),
        BinaryOperator.Minus => new Long(value - operand.AsBigInteger),
        BinaryOperator.Times => new Long(value * operand.AsBigInteger),
        BinaryOperator.Divide => new Rational(new Fraction(value, operand.AsBigInteger)),
        BinaryOperator.Modulo => new Long(value % operand.AsBigInteger),
        BinaryOperator.Power => new Long(BigInteger.Pow(value, operand.AsInt32)),
        BinaryOperator.And => new Long(value & operand.AsBigInteger),
        BinaryOperator.Or => new Long(value | operand.AsBigInteger),
        BinaryOperator.ExclusiveOr => new Long(value ^ operand.AsBigInteger),
        BinaryOperator.ShiftLeft => new Long(value << operand.AsInt32),
        BinaryOperator.ShiftRight => new Long(value >> operand.AsInt32),
        BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsBigInteger),
        BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsBigInteger),
        BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsBigInteger),
        BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsBigInteger),
        _ => base.BinaryOperation(_operator, operand),
    };
}
