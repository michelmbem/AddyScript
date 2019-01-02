using System;
using System.Numerics;

using AddyScript.Ast.Expressions;
using AddyScript.Properties;
using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Boolean : Dynamic
    {
        public static readonly Boolean False = new Boolean(false);
        public static readonly Boolean True = new Boolean(true);

        private readonly bool value;

        public static Boolean FromBool(bool value)
        {
            return value ? True : False;
        }

        private Boolean(bool value)
        {
            this.value = value;
        }

        public Boolean Negation
        {
            get { return FromBool(!value); }
        }

        public override Class Class
        {
            get { return Class.Boolean; }
        }

        public override bool AsBoolean
        {
            get { return value; }
        }

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

        public override object AsNativeObject
        {
            get { return value; }
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return value ? Resources.TRUE : Resources.FALSE;
        }

        protected override bool UnsafeEquals(Dynamic other)
        {
            return value == other.AsBoolean;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        protected override int UnsafeCompareTo(Dynamic other)
        {
            return value.CompareTo(other.AsBoolean);
        }

        public override Dynamic UnaryOperation(UnaryOperator _operator)
        {
            switch (_operator)
            {
                case UnaryOperator.Not:
                    return Negation;
                default:
                    return base.UnaryOperation(_operator);
            }
        }

        public override Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            switch (_operator)
            {
                case BinaryOperator.And:
                    return FromBool(value & operand.AsBoolean);
                case BinaryOperator.Or:
                    return FromBool(value | operand.AsBoolean);
                case BinaryOperator.ExclusiveOr:
                    return FromBool(value ^ operand.AsBoolean);
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }
    }
}
