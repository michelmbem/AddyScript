using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Compilers;
using AddyScript.Properties;
using AddyScript.Runtime.Frames;
using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Dynamics
{
    /// <summary>
    /// The base class of all AddyScript's builtin data types.
    /// </summary>
    public abstract class Dynamic
        : IFrameItem, ICloneable, IFormattable, IEquatable<Dynamic>,
          IComparable<Dynamic>, IDisposable
    {
        private static int serialIDSeqSeed;

        private readonly int serialID;

        protected Dynamic()
        {
            serialID = NextSerialID();
        }

        protected int SerialID
        {
            get { return serialID; }
        }

        public FrameItemKind Kind
        {
            get { return FrameItemKind.Variable; }
        }

        public abstract Class Class { get; }

        public virtual bool AsBoolean
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Boolean.Name)); }
        }

        public virtual int AsInt32
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Integer.Name)); }
        }

        public virtual BigInteger AsBigInteger
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Long.Name)); }
        }

        public virtual Rational32 AsRational32
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Rational.Name)); }
        }

        public virtual double AsDouble
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Float.Name)); }
        }

        public virtual BigDecimal AsBigDecimal
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Decimal.Name)); }
        }

        public virtual Complex64 AsComplex64
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Complex.Name)); }
        }

        public virtual DateTime AsDateTime
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Date.Name)); }
        }

        public virtual List<Dynamic> AsList
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.List.Name)); }
        }

        public virtual Dictionary<Dynamic, Dynamic> AsDictionary
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Map.Name)); }
        }

        public virtual HashSet<Dynamic> AsHashSet
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Set.Name)); }
        }

        public virtual Queue<Dynamic> AsQueue
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Queue.Name)); }
        }

        public virtual Stack<Dynamic> AsStack
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Stack.Name)); }
        }

        public virtual Dictionary<string, Dynamic> AsDynamicObject
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Object.Name)); }
        }

        public virtual object AsNativeObject
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Resource.Name)); }
        }

        public virtual Function AsFunction
        {
            get { throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Closure.Name)); }
        }

        protected static int NextSerialID()
        {
            return ++serialIDSeqSeed;
        }

        public bool InstanceOf(Class klass)
        {
            return (Class == klass) || (Class.Inherits(klass));
        }

        public virtual object Clone()
        {
            return this;
        }

        public virtual string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("<{0}:0x{1:x8}>", Class.Name, SerialID);
        }

        public override string ToString()
        {
            return ToString(string.Empty, CultureInfo.CurrentUICulture);
        }

        protected virtual bool UnsafeEquals(Dynamic other)
        {
            return SerialID == other.SerialID;
        }

        public bool Equals(Dynamic other)
        {
            try
            {
                return UnsafeEquals(other);
            }
            catch
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return (obj is Dynamic) && Equals((Dynamic) obj);
        }

        public override int GetHashCode()
        {
            return SerialID;
        }

        protected virtual int UnsafeCompareTo(Dynamic other)
        {
            return 0;
        }

        public int CompareTo(Dynamic other)
        {
            try
            {
                return UnsafeCompareTo(other);
            }
            catch
            {
                return ToString().CompareTo(other.ToString());
            }
        }

        public virtual void Dispose()
        {
        }

        public virtual bool ConversionNeeded(Class targetClass, BinaryOperator _operator)
        {
            switch (_operator)
            {
                case BinaryOperator.Identical:
                case BinaryOperator.NotIdentical:
                    return false;
                default:
                    return (targetClass == Class.String) || (targetClass.ClassID > Class.ClassID);
            }
        }

        public virtual Dynamic ConvertTo(Class targetClass)
        {
            if (targetClass == Class) return this;

            switch (targetClass.ClassID)
            {
                case ClassID.Boolean:
                    return Boolean.FromBool(AsBoolean);
                case ClassID.Integer:
                    return new Integer(AsInt32);
                case ClassID.Long:
                    return new Long(AsBigInteger);
                case ClassID.Rational:
                    return new Rational(AsRational32);
                case ClassID.Float:
                    return new Float(AsDouble);
                case ClassID.Decimal:
                    return new Decimal(AsBigDecimal);
                case ClassID.Complex:
                    return new Complex(AsComplex64);
                case ClassID.Date:
                    return new Date(AsDateTime);
                case ClassID.String:
                    return new String(ToString());
                case ClassID.List:
                    return new List(AsList);
                case ClassID.Map:
                    return new Map(AsDictionary);
                case ClassID.Set:
                    return new Set(AsHashSet);
                case ClassID.Queue:
                    return new Queue(AsQueue);
                case ClassID.Stack:
                    return new Stack(AsStack);
                case ClassID.Object:
                    return new Object(AsDynamicObject);
                case ClassID.Resource:
                    return new Resource(AsNativeObject);
                case ClassID.Closure:
                    return new Closure(AsFunction);
                default:
                    throw new InvalidCastException();
            }
        }

        public virtual object ConvertTo(Type targetType)
        {
            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Boolean:
                    return AsBoolean;
                case TypeCode.SByte:
                    return (sbyte) AsInt32;
                case TypeCode.Byte:
                    return (byte) AsInt32;
                case TypeCode.Int16:
                    return (short) AsInt32;
                case TypeCode.UInt16:
                    return (ushort) AsInt32;
                case TypeCode.Int32:
                    return AsInt32;
                case TypeCode.UInt32:
                    return (uint) AsBigInteger;
                case TypeCode.Int64:
                    return (long) AsBigInteger;
                case TypeCode.UInt64:
                    return (ulong) AsBigInteger;
                case TypeCode.Single:
                    return (float) AsDouble;
                case TypeCode.Double:
                    return AsDouble;
                case TypeCode.Decimal:
                    return (decimal) AsBigDecimal;
                case TypeCode.DateTime:
                    return AsDateTime;
                case TypeCode.Char:
                    string s = ToString();
                    return s.Length > 0 ? s[0] : char.MinValue;
                case TypeCode.String:
                    return ToString();
                case TypeCode.Object:
                    return targetType == typeof(Dynamic) ? this : AsNativeObject;
                default:
                    return AsNativeObject;
            }
        }

        public virtual Dynamic UnaryOperation(UnaryOperator _operator)
        {
            throw new InvalidOperationException(
                string.Format(Resources.OperatorCantBeApplied,
                              CodeGenerator.UnaryOperatorToString(_operator),
                              Class.Name));
        }

        public virtual Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            switch (_operator)
            {
                case BinaryOperator.Equal:
                    return Boolean.FromBool(Equals(operand));
                case BinaryOperator.NotEqual:
                    return Boolean.FromBool(!Equals(operand));
                case BinaryOperator.Identical:
                    return Boolean.FromBool((Class == operand.Class) && Equals(operand));
                case BinaryOperator.NotIdentical:
                    return Boolean.FromBool(!((Class == operand.Class) && Equals(operand)));
                default:
                    throw new InvalidOperationException(
                        string.Format(Resources.OperatorCantBeApplied,
                                      CodeGenerator.BinaryOperatorToString(_operator),
                                      Class.Name));
            }
        }

        public virtual Dynamic GetProperty(string propertyName)
        {
            throw new InvalidOperationException(string.Format(Resources.ClassHasNoProperty, Class.Name));
        }

        public virtual void SetProperty(string propertyName, Dynamic value)
        {
            throw new InvalidOperationException(string.Format(Resources.ClassHasNoProperty, Class.Name));
        }

        public virtual Dynamic GetItem(Dynamic index)
        {
            throw new InvalidOperationException(string.Format(Resources.ClassHasNoIndexer, Class.Name));
        }

        public virtual void SetItem(Dynamic index, Dynamic value)
        {
            throw new InvalidOperationException(string.Format(Resources.ClassHasNoIndexer, Class.Name));
        }

        public virtual IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetEnumerable()
        {
            throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, Class.Name));
        }
    }
}
