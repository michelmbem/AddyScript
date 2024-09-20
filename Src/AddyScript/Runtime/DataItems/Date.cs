using System;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Date(DateTime value) : DataItem
    {
        public override Class Class
        {
            get { return Class.Date; }
        }

        public override DateTime AsDateTime
        {
            get { return value; }
        }

        public override object AsNativeObject
        {
            get { return value; }
        }

        public override object Clone()
        {
            return new Date(value);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return value.ToString(format, formatProvider);
        }

        protected override bool UnsafeEquals(DataItem other)
        {
            return value == other.AsDateTime;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(DataItem other)
        {
            return value.CompareTo(other.AsDateTime);
        }

        public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
        {
            return _operator switch
            {
                BinaryOperator.Plus => new Date(value + TimeSpan.FromDays(operand.AsDouble)), // ToDo: Maybe introduce a 'Duration' type to handle this!
                BinaryOperator.Minus => new Float((value - operand.AsDateTime).TotalDays), // ToDo: Maybe introduce a 'Duration' type to handle this!
                BinaryOperator.LessThan => Boolean.FromBool(value < operand.AsDateTime),
                BinaryOperator.LessThanOrEqual => Boolean.FromBool(value <= operand.AsDateTime),
                BinaryOperator.GreaterThan => Boolean.FromBool(value > operand.AsDateTime),
                BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(value >= operand.AsDateTime),
                _ => base.BinaryOperation(_operator, operand),
            };
        }
    }
}
