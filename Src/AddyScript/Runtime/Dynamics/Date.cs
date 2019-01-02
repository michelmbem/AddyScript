using System;

using AddyScript.Ast.Expressions;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Date : Dynamic
    {
        private DateTime value;

        public Date(DateTime value)
        {
            this.value = value;
        }

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

        protected override bool UnsafeEquals(Dynamic other)
        {
            return value == other.AsDateTime;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(Dynamic other)
        {
            return value.CompareTo(other.AsDateTime);
        }

        public override Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                    // ToDo: Maybe introduce a 'Duration' type to handle this!
                    return new Date(value + TimeSpan.FromDays(operand.AsDouble));
                case BinaryOperator.Minus:
                    // ToDo: Maybe introduce a 'Duration' type to handle this!
                    return new Float((value - operand.AsDateTime).TotalDays);
                case BinaryOperator.LessThan:
                    return Boolean.FromBool(value < operand.AsDateTime);
                case BinaryOperator.LessThanOrEqual:
                    return Boolean.FromBool(value <= operand.AsDateTime);
                case BinaryOperator.GreaterThan:
                    return Boolean.FromBool(value > operand.AsDateTime);
                case BinaryOperator.GreaterThanOrEqual:
                    return Boolean.FromBool(value >= operand.AsDateTime);
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }
    }
}
