using System;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Duration(TimeSpan value) : DataItem
{
    public override Class Class => Class.Duration;

    public override TimeSpan AsTimeSpan => value;

    public override object AsNativeObject => value;

    public override object Clone() => new Duration(value);

    public override string ToString(string format, IFormatProvider formatProvider) =>
        value.ToString(format, formatProvider);

    protected override bool UnsafeEquals(DataItem other) => value == other.AsTimeSpan;

    public override int GetHashCode() => value.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other) => value.CompareTo(other.AsDateTime);

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Plus => operand is Date
                             ? new Date(operand.AsDateTime + value)
                             : new Duration(value + operand.AsTimeSpan),
        BinaryOperator.Minus => new Duration(value - operand.AsTimeSpan),
        BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsTimeSpan),
        BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsTimeSpan),
        BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsTimeSpan),
        BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsTimeSpan),
        _ => base.BinaryOperation(_operator, operand),
    };

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "days" => new Integer(value.Days),
        "hours" => new Integer(value.Hours),
        "minutes" => new Integer(value.Minutes),
        "seconds" => new Integer(value.Seconds),
        "milliseconds" => new Integer(value.Milliseconds),
        "totalDays" => new Float(value.TotalDays),
        "totalHours" => new Float(value.TotalHours),
        "totalMinutes" => new Float(value.TotalMinutes),
        "totalSeconds" => new Float(value.TotalSeconds),
        "totalMilliseconds" => new Float(value.TotalMilliseconds),
        "ticks" => new Long(value.Ticks),
        _ => base.GetProperty(propertyName),
    };
}
