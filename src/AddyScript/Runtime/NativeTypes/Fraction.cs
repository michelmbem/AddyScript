using System;
using System.Globalization;
using System.Numerics;

using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.NativeTypes;


[Serializable]
public readonly struct Fraction :
    IFormattable, IConvertible, IComparable, IComparable<Fraction>, IEquatable<Fraction>
{
    #region Fields

    public static readonly Fraction MinusOne = new (BigInteger.MinusOne);
    public static readonly Fraction Zero = new (BigInteger.Zero);
    public static readonly Fraction One = new (BigInteger.One);
    public static readonly Fraction Half = new (BigInteger.One, 2);
    public static readonly Fraction Third = new (BigInteger.One, 3);
    
    private readonly BigInteger numerator;
    private readonly BigInteger denominator;
    
    #endregion

    #region Constructors

    public Fraction(BigInteger num, BigInteger den)
    {
        if (den.IsZero) throw new DivideByZeroException("Denominator cannot be zero");
        
        var gcd = BigInteger.GreatestCommonDivisor(num, den);
        numerator = num / gcd * den.Sign;
        denominator = BigInteger.Abs(den / gcd);
    }

    public Fraction(BigInteger num) : this(num, BigInteger.One) { }

    #endregion

    #region Properties

    public BigInteger Numerator => numerator;

    public BigInteger Denominator => denominator;

    public int Sign => numerator.Sign;

    public bool IsZero => numerator.IsZero;

    #endregion

    #region Public Methods

    #region Implementation of IFormattable

    /// <summary>
    /// Formats the value of the current instance using the specified format.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> containing the value of the current instance in the specified format.
    /// </returns>
    /// <param name="format">The <see cref="string" /> specifying the format to use.-or- null to use the default format defined for the type of the <see cref="IFormattable" /> implementation.</param>
    /// <param name="formatProvider">The <see cref="IFormatProvider" /> to use to format the value.-or- null to obtain the numeric format information from the current locale setting of the operating system.</param>
    /// <filterpriority>2</filterpriority>
    public string ToString(string format, IFormatProvider formatProvider)
    {
        string s = denominator.IsOne
            ? numerator.ToString(format, formatProvider)
            : string.Format(formatProvider, $"({{0:{format}}}/{{1:{format}}})", numerator, denominator);
        return s ?? string.Empty;
    }

    #endregion

    #region Implementation of IConvertible

    /// <summary>
    /// Returns the <see cref="TypeCode" /> for this instance.
    /// </summary>
    /// <returns>
    /// The enumerated constant that is the <see cref="TypeCode" /> of the class or value type that implements this interface.
    /// </returns>
    /// <filterpriority>2</filterpriority>
    public TypeCode GetTypeCode() => TypeCode.Object;

    /// <summary>
    /// Converts the value of this instance to an equivalent Boolean value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A Boolean value equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public bool ToBoolean(IFormatProvider provider) => !IsZero;

    /// <summary>
    /// Converts the value of this instance to an equivalent Unicode character using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A Unicode character equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public char ToChar(IFormatProvider provider) => (char)ToUInt16(provider);

    /// <summary>
    /// Converts the value of this instance to an equivalent 8-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 8-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public sbyte ToSByte(IFormatProvider provider) => decimal.ToSByte(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent 8-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 8-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public byte ToByte(IFormatProvider provider) => decimal.ToByte(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent 16-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 16-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public short ToInt16(IFormatProvider provider) => decimal.ToInt16(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent 16-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 16-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public ushort ToUInt16(IFormatProvider provider) => decimal.ToUInt16(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 32-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public int ToInt32(IFormatProvider provider) => decimal.ToInt32(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 32-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public uint ToUInt32(IFormatProvider provider) => decimal.ToUInt32(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent 64-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 64-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public long ToInt64(IFormatProvider provider) => decimal.ToInt64(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent 64-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 64-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public ulong ToUInt64(IFormatProvider provider) => decimal.ToUInt64(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent single-precision floating-point number using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A single-precision floating-point number equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public float ToSingle(IFormatProvider provider) => decimal.ToSingle(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent double-precision floating-point number using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A double-precision floating-point number equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public double ToDouble(IFormatProvider provider) => decimal.ToDouble(ToDecimal(provider));

    /// <summary>
    /// Converts the value of this instance to an equivalent <see cref="T:System.Decimal" /> number using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Decimal" /> number equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public decimal ToDecimal(IFormatProvider provider) => (decimal)numerator / (decimal)denominator;

    /// <summary>
    /// Converts the value of this instance to an equivalent <see cref="DateTime" /> using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A <see cref="DateTime" /> instance equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public DateTime ToDateTime(IFormatProvider provider) => throw new InvalidCastException();

    /// <summary>
    /// Converts the value of this instance to an equivalent <see cref="string" /> using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> instance equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public string ToString(IFormatProvider provider) => ToString("g", provider);

    /// <summary>
    /// Converts the value of this instance to an <see cref="object" /> of the specified <see cref="Type" /> that has an equivalent value, using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An <see cref="object" /> instance of type <paramref name="conversionType" /> whose value is equivalent to the value of this instance.
    /// </returns>
    /// <param name="conversionType">The <see cref="Type" /> to which the value of this instance is converted.</param>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public object ToType(Type conversionType, IFormatProvider provider)
    {
        if (conversionType == typeof(Fraction)) return this;
        if (conversionType == typeof(BigDecimal)) return (BigDecimal)this;
        if (conversionType == typeof(BigInteger)) return (BigInteger)this;
        throw new InvalidCastException($"Cannot convert {nameof(Fraction)} to {conversionType.Name}");
    }

    #endregion

    #region Implementation of IComparable

    /// <summary>
    /// Compares the current instance with another object of the same type.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that indicates the relative order of the objects being compared.
    /// The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj" />.
    /// Zero This instance is equal to <paramref name="obj" />. Greater than zero This instance is greater than <paramref name="obj" />. 
    /// </returns>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <exception cref="ArgumentException"><paramref name="obj" /> is not the same type as this instance.</exception>
    /// <filterpriority>2</filterpriority>
    public int CompareTo(object obj) => obj is Fraction other
             ? CompareTo(other)
             : throw new ArgumentException($"{nameof(obj)} should be a {nameof(Fraction)}");

    #endregion

    #region Implementation of IComparable<Fraction>

    /// <summary>
    /// Compares the current object with another object of the same type.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that indicates the relative order of the objects being compared.
    /// The return value has these meanings:
    /// Less than zero: this instance is less than <paramref name="other" />.
    /// Zero: this instance is equal to <paramref name="other" />.
    /// Greater than zero: this instance is greater than <paramref name="other" />.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public int CompareTo(Fraction other)
    {
        BigInteger left  = numerator * other.denominator;
        BigInteger right = other.numerator * denominator;
        return left.CompareTo(right);
    }

    #endregion

    #region Implementation of IEquatable<Fraction>

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public bool Equals(Fraction other) =>
        numerator == other.numerator && denominator == other.denominator;

    #endregion

    #region Overrides

    /// <summary>
    /// Determines whether the specified object is equal to the current Fraction instance.
    /// </summary>
    /// <remarks>This method overrides Object.Equals and supports value equality comparison for Fraction
    /// instances. Two Fraction objects are considered equal if they represent the same rational value.</remarks>
    /// <param name="obj">The object to compare with the current Fraction instance.</param>
    /// <returns>true if the specified object is a Fraction and is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object obj) => obj is Fraction frac && Equals(frac);

    /// <summary>
    /// Serves as the default hash function for the current object.
    /// </summary>
    /// <remarks>Use this method when inserting instances of this type into hash-based collections such as
    /// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> or <see
    /// cref="System.Collections.Generic.HashSet{T}"/>. The hash code is based on the values of the numerator and
    /// denominator fields.</remarks>
    /// <returns>A 32-bit signed integer hash code that represents the current object.</returns>
    public override int GetHashCode() => HashCode.Combine(numerator, denominator);

    /// <summary>
    /// Returns a string that represents the current object using the general format and the current UI culture.
    /// </summary>
    /// <returns>A string representation of the current object, formatted using the general format specifier and the current UI
    /// culture.</returns>
    public override string ToString() => ToString("g", CultureInfo.CurrentUICulture);

    #endregion

    /// <summary>
    /// Returns a new Fraction that represents the absolute value of the current instance.
    /// </summary>
    /// <returns>A Fraction whose value is the absolute value of this instance.</returns>
    public Fraction Abs() => new (BigInteger.Abs(numerator), denominator);

    /// <summary>
    /// Returns a new Fraction that is the multiplicative inverse of the current fraction.
    /// </summary>
    /// <returns>A Fraction representing the reciprocal of the current fraction.</returns>
    /// <exception cref="DivideByZeroException">Thrown if the current fraction is zero.</exception>
    public Fraction Inverse() => numerator.IsZero
        ? throw new DivideByZeroException("Cannot invert zero fraction")
        : new Fraction(denominator, numerator);

    /// <summary>
    /// Returns a new Fraction that is this instance raised to the specified non-negative integer power.
    /// </summary>
    /// <param name="exp">The non-negative integer exponent to which to raise this fraction.</param>
    /// <returns>A Fraction representing this value raised to the power of exp. If exp is 0, returns a Fraction equal to one.</returns>
    public Fraction Power(int exp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(exp);

        if (exp == 0) return One;

        var (factor, result) = (this, One);
        
        while (exp != 0)
        {
            if ((exp & 1) != 0) result *= factor;
            if (exp == 1) break;
            factor *= factor;
            exp >>= 1;
        }

        return result;
    }

    #endregion

    #region Operators

    #region Conversion from other types

    public static implicit operator Fraction(sbyte n) => new (n);

    public static implicit operator Fraction(byte n) => new (n);

    public static implicit operator Fraction(short n) => new (n);

    public static implicit operator Fraction(ushort n) => new (n);

    public static implicit operator Fraction(int n) => new (n);

    public static implicit operator Fraction(uint n) => new (n);

    public static implicit operator Fraction(long n) => new (n);

    public static implicit operator Fraction(ulong n) => new (n);

    public static implicit operator Fraction(BigInteger n) => new (n);

    public static explicit operator Fraction(float x)
    {
        var (num, den) = MathExt.ToRational(x);
        return new Fraction(num, den);
    }

    public static explicit operator Fraction(double x)
    {
        var (num, den) = MathExt.ToRational(x);
        return new Fraction(num, den);
    }

    public static implicit operator Fraction(decimal d) => (Fraction)new BigDecimal(d);

    #endregion

    #region Conversion to other types

    public static explicit operator sbyte(Fraction self) => self.ToSByte(null);

    public static explicit operator byte(Fraction self) => self.ToByte(null);

    public static explicit operator short(Fraction self) => self.ToInt16(null);

    public static explicit operator ushort(Fraction self) => self.ToUInt16(null);

    public static explicit operator int(Fraction self) => self.ToInt32(null);

    public static explicit operator uint(Fraction self) => self.ToUInt32(null);

    public static explicit operator long(Fraction self) => self.ToInt64(null);

    public static explicit operator ulong(Fraction self) => self.ToUInt64(null);

    public static explicit operator BigInteger(Fraction self) => self.numerator / self.denominator;

    public static explicit operator float(Fraction self) => self.ToSingle(null);

    public static explicit operator double(Fraction self) => self.ToDouble(null);

    public static explicit operator decimal(Fraction self) => self.ToDecimal(null);

    #endregion

    #region Relational

    public static bool operator ==(Fraction a, Fraction b) => a.Equals(b);

    public static bool operator !=(Fraction a, Fraction b) => !a.Equals(b);

    public static bool operator <(Fraction a, Fraction b) => a.CompareTo(b) < 0;

    public static bool operator >(Fraction a, Fraction b) => a.CompareTo(b) > 0;

    public static bool operator <=(Fraction a, Fraction b) => a.CompareTo(b) <= 0;

    public static bool operator >=(Fraction a, Fraction b) => a.CompareTo(b) >= 0;

    #endregion

    #region Arithmetic

    public static Fraction operator +(Fraction a) => a;

    public static Fraction operator -(Fraction a) => new (-a.numerator, a.denominator);

    public static Fraction operator +(Fraction a, Fraction b) =>
        new (a.numerator * b.denominator + b.numerator * a.denominator, a.denominator * b.denominator);

    public static Fraction operator -(Fraction a, Fraction b) =>
        new (a.numerator * b.denominator - b.numerator * a.denominator, a.denominator * b.denominator);

    public static Fraction operator *(Fraction a, Fraction b) =>
        new (a.numerator * b.numerator, a.denominator * b.denominator);

    public static Fraction operator /(Fraction a, Fraction b)
    {
        return b.IsZero
             ? throw new DivideByZeroException("Cannot divide by zero")
             : new Fraction(a.numerator * b.denominator, a.denominator * b.numerator);
    }

    #endregion

    #endregion
}
