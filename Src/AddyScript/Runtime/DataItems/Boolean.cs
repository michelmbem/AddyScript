using System;
using System.Numerics;

using AddyScript.Ast.Expressions;
using AddyScript.Properties;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Boolean : DataItem
    {
        public static readonly Boolean False = new (false);
        public static readonly Boolean True = new (true);

        private readonly bool value;

        public static Boolean FromBool(bool value)
        {
            return value ? True : False;
        }

        private Boolean(bool value)
        {
            this.value = value;
        }

        public Boolean Negation => FromBool(!value);

        public override Class Class => Class.Boolean;

        public override bool AsBoolean => value;

        public override int AsInt32
        {
            get { return value ? 1 : 0; }
        }

        public override BigInteger AsBigInteger
        {
            get { return value ? BigInteger.One : BigInteger.Zero; }
        }

        public override Rational32 AsRational32
        {
            get { return value ? Rational32.One : Rational32.Zero; }
        }

        public override double AsDouble
        {
            get { return value ? 1.0 : 0.0; }
        }

        public override BigDecimal AsBigDecimal
        {
            get { return value ? BigDecimal.One : BigDecimal.Zero; }
        }

        public override object AsNativeObject => value;

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return value ? Resources.TRUE : Resources.FALSE;
        }

        protected override bool UnsafeEquals(DataItem other)
        {
            return value == other.AsBoolean;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(DataItem other)
        {
            return value.CompareTo(other.AsBoolean);
        }

        public override DataItem UnaryOperation(UnaryOperator _operator)
        {
            return _operator switch
            {
                UnaryOperator.Not => Negation,
                _ => base.UnaryOperation(_operator),
            };
        }

        public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
        {
            return _operator switch
            {
                BinaryOperator.And => FromBool(value & operand.AsBoolean),
                BinaryOperator.Or => FromBool(value | operand.AsBoolean),
                BinaryOperator.ExclusiveOr => FromBool(value ^ operand.AsBoolean),
                _ => base.BinaryOperation(_operator, operand),
            };
        }
    }
}
