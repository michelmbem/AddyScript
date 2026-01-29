using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using SysComplex = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Translators;
using AddyScript.Properties;
using AddyScript.Runtime.Frames;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.DataItems;


/// <summary>
/// The base class of all AddyScript builtin data types.
/// </summary>
public abstract class DataItem :
    IFrameItem, ICloneable, IFormattable, IEquatable<DataItem>, IComparable<DataItem>, IDisposable
{
    public FrameItemKind Kind => FrameItemKind.Variable;

    public abstract Class Class { get; }

    public virtual bool AsBoolean =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Boolean.Name));

    public virtual int AsInt32 =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Integer.Name));

    public virtual BigInteger AsBigInteger =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Long.Name));

    public virtual Fraction AsFraction =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Rational.Name));

    public virtual double AsDouble =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Float.Name));

    public virtual BigDecimal AsBigDecimal =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Decimal.Name));

    public virtual SysComplex AsComplex =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Complex.Name));

    public virtual DateTime AsDateTime =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Date.Name));

    public virtual TimeSpan AsTimeSpan =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Duration.Name));

    public virtual byte[] AsByteArray =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Blob.Name));

    public virtual DataItem[] AsArray =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Tuple.Name));

    public virtual List<DataItem> AsList =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.List.Name));

    public virtual HashSet<DataItem> AsHashSet =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Set.Name));

    public virtual Queue<DataItem> AsQueue =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Queue.Name));

    public virtual Stack<DataItem> AsStack =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Stack.Name));

    public virtual Dictionary<DataItem, DataItem> AsDictionary =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Map.Name));

    public virtual Dictionary<string, DataItem> AsDynamicObject =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Object.Name));

    public virtual object AsNativeObject =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Resource.Name));

    public virtual Function AsFunction =>
        throw new InvalidCastException(string.Format(Resources.CannotConvert, Class.Name, Class.Closure.Name));

    public bool InstanceOf(Class klass) => Class == klass || Class.Inherits(klass);

    public virtual object Clone() => this;

    public virtual string ToString(string format, IFormatProvider formatProvider) => $"<{Class.Name}>";

    public override string ToString() => ToString(string.Empty, CultureInfo.CurrentUICulture);

    protected virtual bool UnsafeEquals(DataItem other) => ReferenceEquals(this, other);

    public bool Equals(DataItem other)
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

    public override bool Equals(object obj) => obj is DataItem other && Equals(other);

    protected virtual int UnsafeCompareTo(DataItem other) => 0;

    public int CompareTo(DataItem other)
    {
        try
        {
            return UnsafeCompareTo(other);
        }
        catch
        {
            return string.CompareOrdinal(ToString(), other.ToString());
        }
    }

    public virtual void Dispose() => GC.SuppressFinalize(this);

    public virtual bool IsEmpty() => false;

    public virtual bool ConversionNeeded(Class targetClass, BinaryOperator _operator) => _operator switch
    {
        BinaryOperator.Identical or BinaryOperator.NotIdentical or BinaryOperator.Contains or
        BinaryOperator.StartsWith or BinaryOperator.EndsWith or BinaryOperator.Matches => false,
        BinaryOperator.Plus => Class.IsLosslesslyConvertibleTo(targetClass) || targetClass == Class.String,
        _ => Class.IsLosslesslyConvertibleTo(targetClass),
    };

    public virtual DataItem ConvertTo(Class targetClass)
    {
        if (targetClass == Class) return this;

        return targetClass.ClassID switch
        {
            ClassID.Boolean => Boolean.FromBool(AsBoolean),
            ClassID.Integer => new Integer(AsInt32),
            ClassID.Long => new Long(AsBigInteger),
            ClassID.Rational => new Rational(AsFraction),
            ClassID.Float => new Float(AsDouble),
            ClassID.Decimal => new Decimal(AsBigDecimal),
            ClassID.Complex => new Complex(AsComplex),
            ClassID.Date => new Date(AsDateTime),
            ClassID.String => new String(ToString()),
            ClassID.Blob => new Blob(AsByteArray),
            ClassID.Tuple => new Tuple(AsArray),
            ClassID.List => new List(AsList),
            ClassID.Set => new Set(AsHashSet),
            ClassID.Queue => new Queue(AsQueue),
            ClassID.Stack => new Stack(AsStack),
            ClassID.Map => new Map(AsDictionary),
            ClassID.Object => new Object(AsDynamicObject),
            ClassID.Resource => new Resource(AsNativeObject),
            ClassID.Closure => new Closure(AsFunction),
            _ => throw new InvalidCastException(),
        };
    }

    public virtual object ConvertTo(Type targetType)
    {
        if (targetType == GetType() || targetType == typeof(DataItem))
            return this;

        return Type.GetTypeCode(targetType) switch
        {
            TypeCode.Boolean => AsBoolean,
            TypeCode.SByte => (sbyte)AsInt32,
            TypeCode.Byte => (byte)AsInt32,
            TypeCode.Int16 => (short)AsInt32,
            TypeCode.UInt16 => (ushort)AsInt32,
            TypeCode.Int32 => AsInt32,
            TypeCode.UInt32 => (uint)AsBigInteger,
            TypeCode.Int64 => (long)AsBigInteger,
            TypeCode.UInt64 => (ulong)AsBigInteger,
            TypeCode.Single => (float)AsDouble,
            TypeCode.Double => AsDouble,
            TypeCode.Decimal => (decimal)AsBigDecimal,
            TypeCode.DateTime => AsDateTime,
            TypeCode.Char => (ToString() + char.MinValue)[0],
            TypeCode.String => ToString(),
            _ => targetType switch
            {
                not null when targetType == typeof(TimeSpan) => AsTimeSpan,
                not null when targetType == typeof(byte[]) => AsByteArray,
                not null when targetType == typeof(DateOnly) => DateOnly.FromDateTime(AsDateTime),
                not null when targetType == typeof(TimeOnly) => TimeOnly.FromTimeSpan(AsTimeSpan),
                not null when targetType.IsAssignableTo(typeof(ITuple)) =>
                    Reflector.CreateInstance(targetType, AsArray),
                _ => AsNativeObject,
            },
        };
    }

    public virtual DataItem UnaryOperation(UnaryOperator _operator) =>
        throw new InvalidOperationException(string.Format(Resources.OperatorCantBeApplied,
                                                          CodeGenerator.UnaryOperatorToString(_operator),
                                                          Class.Name));

    public virtual DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Equal => Boolean.FromBool(Equals(operand)),
        BinaryOperator.NotEqual => Boolean.FromBool(!Equals(operand)),
        BinaryOperator.Identical => Boolean.FromBool(Class == operand.Class && Equals(operand)),
        BinaryOperator.NotIdentical => Boolean.FromBool(!(Class == operand.Class && Equals(operand))),
        _ => throw new InvalidOperationException(string.Format(Resources.OperatorCantBeApplied,
                                                               CodeGenerator.BinaryOperatorToString(_operator),
                                                               Class.Name)),
    };

    public virtual DataItem GetProperty(string propertyName) =>
        throw new InvalidOperationException(string.Format(Resources.ClassHasNoProperty, Class.Name, propertyName));

    public virtual void SetProperty(string propertyName, DataItem value) =>
        throw new InvalidOperationException(string.Format(Resources.ClassHasNoProperty, Class.Name, propertyName));

    public virtual DataItem GetItem(DataItem index) =>
        throw new InvalidOperationException(string.Format(Resources.ClassHasNoIndexReader, Class.Name));

    public virtual void SetItem(DataItem index, DataItem value) =>
        throw new InvalidOperationException(string.Format(Resources.ClassHasNoIndexWriter, Class.Name));

    public virtual DataItem GetItemRange(int lBound, int uBound) =>
        throw new InvalidOperationException(string.Format(Resources.ClassHasNoRangeReader, Class.Name));

    public virtual void SetItemRange(int lBound, int uBound, DataItem value) =>
        throw new InvalidOperationException(string.Format(Resources.ClassHasNoRangeWriter, Class.Name));

    public virtual IEnumerable<(DataItem, DataItem)> GetEnumerable() =>
        throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, Class.Name));

    protected static void AdjustBounds(int size, ref int lower, ref int upper)
    {
        while (lower < 0) lower += size;
        while (upper < 0) upper += size;
        upper = Math.Min(upper, size);
    }
}
