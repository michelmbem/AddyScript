using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.DataItems;


public sealed class Integer(int value) : DataItem
{
    public Integer Opposite => new(-value);

    public Integer Complement => new(~value);

    public override Class Class => Class.Integer;

    public override bool AsBoolean => value != 0;

    public override int AsInt32 => value;

    public override BigInteger AsBigInteger => new(value);

    public override Rational32 AsRational32 => new(value);

    public override double AsDouble => value;

    public override BigDecimal AsBigDecimal => new(value);

    public override Complex64 AsComplex64 => new(value, 0);

    public override object AsNativeObject => value;

    public override string ToString(string format, IFormatProvider formatProvider)
        => value.ToString(format, formatProvider);

    protected override bool UnsafeEquals(DataItem other) => value == other.AsInt32;

    public override int GetHashCode() => value;

    protected override int UnsafeCompareTo(DataItem other) => value.CompareTo(other.AsInt32);

    public override object ConvertTo(Type targetType) => targetType.IsEnum
         ? Convert.ChangeType(value, targetType)
         : base.ConvertTo(targetType);

    public override DataItem UnaryOperation(UnaryOperator _operator) => _operator switch
    {
        UnaryOperator.Plus => this,
        UnaryOperator.Minus => Opposite,
        UnaryOperator.BitwiseNot => Complement,
        _ => base.UnaryOperation(_operator),
    };

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
    {
        try
        {
            return _operator switch
            {
                BinaryOperator.Plus => new Integer(checked(value + operand.AsInt32)),
                BinaryOperator.Minus => new Integer(value - operand.AsInt32),
                BinaryOperator.Times => operand.Class.ClassID switch
                {
                    ClassID.String or ClassID.Blob or ClassID.Tuple or ClassID.List => operand.BinaryOperation(_operator, this),
                    _ => new Integer(checked(value * operand.AsInt32))
                },
                BinaryOperator.Divide => Rational.Simplify(new Rational32(value, operand.AsInt32)),
                BinaryOperator.Modulo => new Integer(value % operand.AsInt32),
                BinaryOperator.Power => new Integer(MathUtil.Power(value, operand.AsInt32)),
                BinaryOperator.And => new Integer(value & operand.AsInt32),
                BinaryOperator.Or => new Integer(value | operand.AsInt32),
                BinaryOperator.ExclusiveOr => new Integer(value ^ operand.AsInt32),
                BinaryOperator.ShiftLeft => new Integer(value << operand.AsInt32),
                BinaryOperator.ShiftRight => new Integer(value >> operand.AsInt32),
                BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsInt32),
                BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsInt32),
                BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsInt32),
                BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsInt32),
                _ => base.BinaryOperation(_operator, operand),
            };
        }
        catch (OverflowException)
        {
            return new Long(value).BinaryOperation(_operator, operand);
        }
    }
}
