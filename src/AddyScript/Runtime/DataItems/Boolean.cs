using System;
using System.Numerics;
using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Boolean : DataItem
{
    public const string FALSE_STRING = "false";
    public const string TRUE_STRING = "true";
    
    public static readonly Boolean False = new (false);
    public static readonly Boolean True = new (true);

    private readonly bool value;

    public static Boolean FromBool(bool value) => value ? True : False;

    private Boolean(bool value) => this.value = value;

    public Boolean Negation => FromBool(!value);

    public override Class Class => Class.Boolean;

    public override bool AsBoolean => value;

    public override int AsInt32 => value ? 1 : 0;

    public override BigInteger AsBigInteger => value ? BigInteger.One : BigInteger.Zero;

    public override Rational32 AsRational32 => value ? Rational32.One : Rational32.Zero;

    public override double AsDouble => value ? 1.0 : 0.0;

    public override BigDecimal AsBigDecimal => value ? BigDecimal.One : BigDecimal.Zero;

    public override object AsNativeObject => value;

    public override string ToString(string format, IFormatProvider formatProvider) =>
        value ? TRUE_STRING : FALSE_STRING;

    protected override bool UnsafeEquals(DataItem other) => value == other.AsBoolean;

    public override int GetHashCode() => value.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other) => value.CompareTo(other.AsBoolean);

    public override DataItem UnaryOperation(UnaryOperator _operator) => _operator switch
    {
        UnaryOperator.Not => Negation,
        _ => base.UnaryOperation(_operator),
    };

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.And => FromBool(value && operand.AsBoolean),
        BinaryOperator.Or => FromBool(value || operand.AsBoolean),
        BinaryOperator.ExclusiveOr => FromBool(value ^ operand.AsBoolean),
        _ => base.BinaryOperation(_operator, operand),
    };
}
