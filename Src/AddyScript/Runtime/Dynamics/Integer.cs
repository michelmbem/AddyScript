using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Integer : Dynamic
    {
        private readonly int value;

        public Integer(int value)
        {
            this.value = value;
        }

        public Integer Opposite
        {
            get { return new Integer(-value); }
        }

        public Integer Complement
        {
            get { return new Integer(~value); }
        }

        public override Class Class
        {
            get { return Class.Integer; }
        }

        public override bool AsBoolean
        {
            get { return value != 0; }
        }

        public override int AsInt32
        {
            get { return value; }
        }

        public override BigInteger AsBigInteger
        {
            get { return new BigInteger(value); }
        }

        public override Rational32 AsRational32
        {
            get { return new Rational32(value); }
        }

        public override double AsDouble
        {
            get { return value; }
        }

        public override BigDecimal AsBigDecimal
        {
            get { return new BigDecimal(value); }
        }

        public override Complex64 AsComplex64
        {
            get { return new Complex64(value, 0); }
        }

        public override object AsNativeObject
        {
            get { return value; }
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return value.ToString(format, formatProvider);
        }

        protected override bool UnsafeEquals(Dynamic other)
        {
            return value == other.AsInt32;
        }

        public override int GetHashCode()
        {
            return value;
        }

        protected override int UnsafeCompareTo(Dynamic other)
        {
            return value.CompareTo(other.AsInt32);
        }

        public override object ConvertTo(Type targetType)
        {
            return targetType.IsEnum
                 ? Convert.ChangeType(value, targetType)
                 : base.ConvertTo(targetType);
        }

        public override Dynamic UnaryOperation(UnaryOperator _operator)
        {
            switch (_operator)
            {
                case UnaryOperator.Plus:
                    return this;
                case UnaryOperator.Minus:
                    return Opposite;
                case UnaryOperator.BitwiseNot:
                    return Complement;
                default:
                    return base.UnaryOperation(_operator);
            }
        }

        public override Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            try
            {
                switch (_operator)
                {
                    case BinaryOperator.Plus:
                        return new Integer(checked(value + operand.AsInt32));
                    case BinaryOperator.Minus:
                        return new Integer(value - operand.AsInt32);
                    case BinaryOperator.Times:
                        return new Integer(checked(value * operand.AsInt32));
                    case BinaryOperator.Divide:
                        return Rational.Simplify(new Rational32(value, operand.AsInt32));
                    case BinaryOperator.Modulo:
                        return new Integer(value % operand.AsInt32);
                    case BinaryOperator.Power:
                        return new Integer(MathUtil.Power(value, operand.AsInt32));
                    case BinaryOperator.And:
                        return new Integer(value & operand.AsInt32);
                    case BinaryOperator.Or:
                        return new Integer(value | operand.AsInt32);
                    case BinaryOperator.ExclusiveOr:
                        return new Integer(value ^ operand.AsInt32);
                    case BinaryOperator.ShiftLeft:
                        return new Integer(value << operand.AsInt32);
                    case BinaryOperator.ShiftRight:
                        return new Integer(value >> operand.AsInt32);
                    case BinaryOperator.LessThan:
                        return Boolean.FromBool(value < operand.AsInt32);
                    case BinaryOperator.LessThanOrEqual:
                        return Boolean.FromBool(value <= operand.AsInt32);
                    case BinaryOperator.GreaterThan:
                        return Boolean.FromBool(value > operand.AsInt32);
                    case BinaryOperator.GreaterThanOrEqual:
                        return Boolean.FromBool(value >= operand.AsInt32);
                    default:
                        return base.BinaryOperation(_operator, operand);
                }
            }
            catch (OverflowException)
            {
                return new Long(value).BinaryOperation(_operator, operand);
            }
        }
    }
}
