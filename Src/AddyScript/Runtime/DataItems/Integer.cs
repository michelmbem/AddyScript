using System;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.Utilities;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Integer(int value) : DataItem
    {
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

        protected override bool UnsafeEquals(DataItem other)
        {
            return value == other.AsInt32;
        }

        public override int GetHashCode()
        {
            return value;
        }

        protected override int UnsafeCompareTo(DataItem other)
        {
            return value.CompareTo(other.AsInt32);
        }

        public override object ConvertTo(Type targetType)
        {
            return targetType.IsEnum
                 ? Convert.ChangeType(value, targetType)
                 : base.ConvertTo(targetType);
        }

        public override DataItem UnaryOperation(UnaryOperator _operator)
        {
            return _operator switch
            {
                UnaryOperator.Plus => this,
                UnaryOperator.Minus => Opposite,
                UnaryOperator.BitwiseNot => Complement,
                _ => base.UnaryOperation(_operator),
            };
        }

        public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
        {
            try
            {
                return _operator switch
                {
                    BinaryOperator.Plus => new Integer(checked(value + operand.AsInt32)),
                    BinaryOperator.Minus => new Integer(value - operand.AsInt32),
                    BinaryOperator.Times => new Integer(checked(value * operand.AsInt32)),
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
}
