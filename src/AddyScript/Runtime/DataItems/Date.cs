using System;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Date(DateTime value) : DataItem
{
    public override Class Class => Class.Date;

    public override DateTime AsDateTime => value;

    public override object AsNativeObject => value;

    public override object Clone() => new Date(value);

    public override string ToString(string format, IFormatProvider formatProvider)
        => value.ToString(format, formatProvider);

    protected override bool UnsafeEquals(DataItem other) => value == other.AsDateTime;

    public override int GetHashCode() => value.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other) => value.CompareTo(other.AsDateTime);

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Plus => new Date(value + TimeSpan.FromDays(operand.AsDouble)), // ToDo: Maybe introduce a 'Duration' type to handle this!
        BinaryOperator.Minus => new Float((value - operand.AsDateTime).TotalDays), // ToDo: Maybe introduce a 'Duration' type to handle this!
        BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsDateTime),
        BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsDateTime),
        BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsDateTime),
        BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsDateTime),
        _ => base.BinaryOperation(_operator, operand),
    };

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "date" => new Date(value.Date),
        "time" => new Date(new DateTime(value.TimeOfDay.Ticks)),
        "year" => new Integer(value.Year),
        "month" => new Integer(value.Month),
        "day" => new Integer(value.Day),
        "yearday" => new Integer(value.DayOfYear),
        "weekday" => new String(value.DayOfWeek.ToString()),
        "hour" => new Integer(value.Hour),
        "minute" => new Integer(value.Minute),
        "second" => new Integer(value.Second),
        "millisecond" => new Integer(value.Millisecond),
        "ticks" => new Long(value.Ticks),
        _ => base.GetProperty(propertyName),
    };
}
