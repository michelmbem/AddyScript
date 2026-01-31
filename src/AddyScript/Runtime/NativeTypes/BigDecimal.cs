using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;


namespace AddyScript.Runtime.NativeTypes;


[Serializable]
public partial struct BigDecimal :
    IFormattable, IConvertible, IComparable, IComparable<BigDecimal>, IEquatable<BigDecimal>
{
    #region Fields

    // We are fixing an arbitrary limit of 50 decimal digits for big decimals.
    private const int MAX_SCALE = 50;

    private static readonly Regex DecimalRegex = GetDecimalRegex();
    private static readonly BigInteger BigIntegerTen = new (10);

    public static readonly BigDecimal MinusOne = new (BigInteger.MinusOne, 0);
    public static readonly BigDecimal Zero = new (BigInteger.Zero, 0);
    public static readonly BigDecimal One = new (BigInteger.One, 0);
    public static readonly BigDecimal Ten = new (BigIntegerTen, 0);
    public static readonly BigDecimal PointOne = new (BigInteger.One, 1);

    private readonly BigInteger unscaled;
    private readonly int scale;

    #endregion

    #region Constructors

    private BigDecimal(BigInteger unscaled, int scale)
    {
        this.unscaled = unscaled;
        this.scale = scale;
    }

    public BigDecimal(int n) : this(new BigInteger(n)) { }

    public BigDecimal(uint n) : this(new BigInteger(n)) { }

    public BigDecimal(long n) : this(new BigInteger(n)) { }

    public BigDecimal(ulong n) : this(new BigInteger(n)) { }

    public BigDecimal(BigInteger n) : this(n, 0) { }

    public BigDecimal(float x) : this((double)x) { }

    public BigDecimal(double x)
    {
        if (double.IsNaN(x) || double.IsInfinity(x))
            throw new ArithmeticException("Invalid floating-point value");

        // "R" guarantees round-trip exactness
        var tmp = Parse(x.ToString("R", CultureInfo.InvariantCulture));
        unscaled = tmp.unscaled;
        scale = tmp.scale;
    }
    
    public BigDecimal(decimal d)
    {
        int[] bits = decimal.GetBits(d);

        var (lo, mid, hi, ext) = (bits[0], bits[1], bits[2], bits[3]);
        bool negate = (ext & unchecked((int)0x80000000)) != 0;
        int decs = (ext >> 16) & 0x7F;

        var value = ((BigInteger)(uint)hi << 64) | ((BigInteger)(uint)mid << 32) | (uint)lo;
        if (negate) value = -value;

        var tmp = new BigDecimal(value, decs).Deflate();
        unscaled = tmp.unscaled;
        scale = tmp.scale;
    }

    public BigDecimal(byte[] bytes)
    {
        var unscaledBytes = new byte[bytes.Length - 4];
        Array.Copy(bytes, unscaledBytes, unscaledBytes.Length);
        unscaled = new BigInteger(unscaledBytes);
        
        uint dword = BitConverter.ToUInt32(bytes, unscaledBytes.Length);
        if ((dword & 0x80000000) != 0)
        {
            unscaled = -unscaled;
            dword &= 0x7FFFFFFF;
        }

        scale = (int)dword;
    }

    #endregion

    #region Properties
    
    public readonly BigInteger Unscaled => unscaled;
    
    public readonly int Scale => scale;

    public readonly int Sign => unscaled.Sign;

    public readonly bool IsZero => unscaled.IsZero;

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
    public readonly string ToString(string format, IFormatProvider formatProvider)
    {
        StringBuilder sb = new (BigInteger.Abs(unscaled).ToString());
        
        if (scale > 0)
        {
            if (scale >= sb.Length) sb.Insert(0, "0", scale - sb.Length + 1);
            var fmt = (NumberFormatInfo)formatProvider?.GetFormat(typeof(NumberFormatInfo));
            sb.Insert(sb.Length - scale, fmt?.CurrencyDecimalSeparator ?? ".");
        }

        if (Sign < 0) sb.Insert(0, '-');

        return sb.ToString();
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
    public readonly TypeCode GetTypeCode() => TypeCode.Object;

    /// <summary>
    /// Converts the value of this instance to an equivalent Boolean value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A Boolean value equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly bool ToBoolean(IFormatProvider provider) => !IsZero;

    /// <summary>
    /// Converts the value of this instance to an equivalent Unicode character using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A Unicode character equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly char ToChar(IFormatProvider provider) => (char)ToUInt16(provider);

    /// <summary>
    /// Converts the value of this instance to an equivalent 8-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 8-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly sbyte ToSByte(IFormatProvider provider) => (sbyte)Round().unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 8-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 8-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly byte ToByte(IFormatProvider provider) => (byte)Round().unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 16-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 16-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly short ToInt16(IFormatProvider provider) => (short)Round().unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 16-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 16-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly ushort ToUInt16(IFormatProvider provider) => (ushort)Round().unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 32-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public int ToInt32(IFormatProvider provider) => (int)Round().unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 32-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly uint ToUInt32(IFormatProvider provider) => (uint)Round().unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 64-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 64-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly long ToInt64(IFormatProvider provider) => (long)Round().unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 64-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 64-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly ulong ToUInt64(IFormatProvider provider) => (ulong)Round().unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent single-precision floating-point number using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A single-precision floating-point number equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly float ToSingle(IFormatProvider provider)
    {
        if (unscaled.IsZero)
            return 0f * (unscaled.Sign < 0 ? -1f : 1f);

        if (scale is < -45 or > 38)
            return unscaled.Sign < 0 ? float.NegativeInfinity : float.PositiveInfinity;

        return float.Parse(ToScientificString(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent double-precision floating-point number using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A double-precision floating-point number equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly double ToDouble(IFormatProvider provider)
    {
        if (unscaled.IsZero)
            return 0.0 * (unscaled.Sign < 0 ? -1.0 : 1.0);

        if (scale is < -324 or > 308)
            return unscaled.Sign < 0 ? double.NegativeInfinity : double.PositiveInfinity;

        return double.Parse(ToScientificString(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent <see cref="T:System.Decimal" /> number using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Decimal" /> number equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly decimal ToDecimal(IFormatProvider provider)
    {
        if (scale is < 0 or > 28)
            throw new OverflowException("Scale out of range for decimal");

        var abs = BigInteger.Abs(unscaled);
        if (abs.GetBitLength() > 96)
            throw new OverflowException("Magnitude too large for decimal");

        var lo = (uint)(abs & uint.MaxValue);
        var mid = (uint)((abs >> 32) & uint.MaxValue);
        var hi = (uint)((abs >> 64) & uint.MaxValue);
        var negate = unscaled.Sign < 0;
        
        return new decimal((int)lo, (int)mid, (int)hi, negate, (byte)scale);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent <see cref="DateTime" /> using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A <see cref="DateTime" /> instance equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly DateTime ToDateTime(IFormatProvider provider) =>
        throw new InvalidCastException("Cannot convert BigDecimal to DateTime");

    /// <summary>
    /// Converts the value of this instance to an equivalent <see cref="string" /> using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> instance equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly string ToString(IFormatProvider provider) => ToString();

    /// <summary>
    /// Converts the value of this instance to an <see cref="object" /> of the specified <see cref="Type" /> that has an equivalent value, using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An <see cref="object" /> instance of type <paramref name="conversionType" /> whose value is equivalent to the value of this instance.
    /// </returns>
    /// <param name="conversionType">The <see cref="Type" /> to which the value of this instance is converted.</param>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly object ToType(Type conversionType, IFormatProvider provider)
    {
        if (conversionType == typeof(BigDecimal)) return this;
        if (conversionType == typeof(Fraction)) return (Fraction)this;
        if (conversionType == typeof(BigInteger)) return (BigInteger)this;
        throw new InvalidCastException($"Cannot convert {nameof(BigDecimal)} to {conversionType.Name}");
    }

    #endregion

    #region Implementation of IComparable

    /// <summary>
    /// Compares the current instance with another object of the same type.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that indicates the relative order of the objects being compared.
    /// The return value has these meanings:
    /// Less than zero: this instance is less than <paramref name="obj" />.
    /// Zero: this instance is equal to <paramref name="obj" />.
    /// Greater than zero: this instance is greater than <paramref name="obj" />.
    /// </returns>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <filterpriority>2</filterpriority>
    public readonly int CompareTo(object obj)
    {
        return obj is BigDecimal dec
             ? CompareTo(dec)
             : throw new ArgumentException($"{nameof(obj)} must be of type {nameof(BigDecimal)}");
    }

    #endregion

    #region Implementation of IComparable<BigDecimal>

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
    public readonly int CompareTo(BigDecimal other)
    {
        return scale >= other.scale
             ? Compare(this, other.Inflate(scale))
             : Compare(Inflate(other.scale), other);
    }

    #endregion

    #region Implementation of IEquatable<BigDecimal>

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public readonly bool Equals(BigDecimal other)
    {
        return scale >= other.scale
             ? Compare(this, other.Inflate(scale)) == 0
             : Compare(Inflate(other.scale), other) == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Parses the string representation of a decimal number and returns its equivalent BigDecimal value.
    /// </summary>
    /// <remarks>
    /// The input string must represent a valid decimal number, optionally including a sign ('+' or '-'), a decimal point,
    /// and an exponent part (e.g., "1.23e-4"). The method does not allow whitespace or invalid characters.
    /// To avoid exceptions, validate the input format before calling this method.
    /// </remarks>
    /// <param name="s">
    /// The string containing the decimal number to parse. The string may include an optional sign, decimal point, and
    /// exponent. Leading and trailing whitespace are not permitted.
    /// </param>
    /// <returns>A BigDecimal value equivalent to the number contained in the specified string.</returns>
    /// <exception cref="FormatException">Thrown if the input string is null, empty, or not in a valid decimal format.</exception>
    public static BigDecimal Parse(string s)
    {
        if (string.IsNullOrEmpty(s)) throw new FormatException();

        var input = s[0] switch
        {
            '.' => "0" + s,
            '+' or '-' when s.Length > 1 && s[1] == '.' => s.Insert(1, "0"),
            _ => s
        };

        Match match = DecimalRegex.Match(input);
        if (!match.Success) throw new FormatException();

        StringBuilder sb = new ();

        Group signGroup = match.Groups["SIGN"];
        if (signGroup.Success) sb.Append(signGroup.Value);

        Group integerGroup = match.Groups["INTEGER"];
        if (integerGroup.Success) sb.Append(integerGroup.Value);

        int decs = 0;

        Group decimalsGroup = match.Groups["DECIMALS"];
        if (decimalsGroup.Success)
        {
            decs = decimalsGroup.Length;
            sb.Append(decimalsGroup.Value);
        }

        Group exponentGroup = match.Groups["EXPONENT"];
        if (exponentGroup.Success) decs -= int.Parse(exponentGroup.Value[1..]);

        if (decs < 0)
        {
            sb.Append('0', -decs);
            decs = 0;
        }

        return new BigDecimal(BigInteger.Parse(sb.ToString()), decs).Deflate();
    }

    /// <summary>
    /// Attempts to convert the string representation of a number to its BigDecimal equivalent, and returns a value that
    /// indicates whether the conversion succeeded.
    /// </summary>
    /// <remarks>Unlike the Parse method, TryParse does not throw an exception if the conversion fails.</remarks>
    /// <param name="s">The string containing the number to convert.</param>
    /// <param name="value">
    /// When this method returns, contains the BigDecimal value equivalent to the number contained in <paramref name="s"/>,
    /// if the conversion succeeded, or the default value if the conversion failed. This parameter is passed uninitialized.
    /// </param>
    /// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
    public static bool TryParse(string s, out BigDecimal value)
    {
        try
        {
            value = Parse(s);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Determines whether the specified object is equal to the current BigDecimal instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current BigDecimal instance.</param>
    /// <returns>true if the specified object is a BigDecimal and has the same value as the current instance; otherwise, false.</returns>
    public readonly override bool Equals(object obj) => obj is BigDecimal other && Equals(other);

    /// <summary>
    /// Serves as the default hash function for the current object.
    /// </summary>
    /// <remarks>
    /// Use the returned hash code when storing instances in hash-based collections such as HashSet
    /// or Dictionary. The hash code is based on the values of the object's unscaled and scale fields.
    /// </remarks>
    /// <returns>A 32-bit signed integer hash code for the current object.</returns>
    public readonly override int GetHashCode() => HashCode.Combine(unscaled, scale);

    /// <summary>
    /// Returns a string that represents the current object using the current UI culture.
    /// </summary>
    /// <remarks>
    /// This method provides a culture-sensitive string representation of the object by using the current UI culture.
    /// To specify a different format or culture, use the overloads that accept format or culture parameters.
    /// </remarks>
    /// <returns>A string representation of the current object, formatted using the current UI culture.</returns>
    public readonly override string ToString() => ToString(null, CultureInfo.CurrentUICulture);

    #endregion

    /// <summary>
    /// Returns a new BigDecimal that is this value raised to the specified integer power.
    /// </summary>
    /// <remarks>
    /// Raising to a negative exponent returns the reciprocal of this value raised to the absolute value of the exponent.
    /// If exp is zero, the result is 1 regardless of the value of this instance, except when this instance is zero,
    /// which may result in an undefined value.
    /// </remarks>
    /// <param name="exp">The exponent to which to raise this value. Can be negative, zero, or positive.</param>
    /// <returns>A BigDecimal representing this value raised to the power of exp.</returns>
    public readonly BigDecimal Pow(int exp) => new (BigInteger.Pow(unscaled, exp), scale * exp);

    /// <summary>
    /// Returns a new BigDecimal whose value is the absolute value of the current instance.
    /// </summary>
    /// <returns>
    /// A BigDecimal representing the absolute value of this instance.
    /// If the value is already non-negative, the original value is returned.
    /// </returns>
    public readonly BigDecimal Abs() => new (BigInteger.Abs(unscaled), scale);

    /// <summary>
    /// Returns a new BigDecimal instance with any fractional digits removed, effectively rounding toward zero.
    /// </summary>
    /// <remarks>
    /// This method does not perform rounding; it simply removes any fractional component.
    /// If the value is already integral, the original instance is returned.
    /// </remarks>
    /// <returns>A BigDecimal representing the integral part of the current value, with all digits after the decimal point truncated.</returns>
    public readonly BigDecimal Truncate()
    {
        if (scale <= 0) return this;
        var powers = BigInteger.Pow(BigIntegerTen, scale);
        return new BigDecimal(unscaled / powers);
    }

    /// <summary>
    /// Returns the largest integral value less than or equal to the current BigDecimal value.
    /// </summary>
    /// <remarks>
    /// This method is equivalent to mathematical floor operation. For positive values, it returns the integer part;
    /// for negative values with a fractional component, it returns the next lower integer.
    /// </remarks>
    /// <returns>A BigDecimal representing the largest integer less than or equal to this value.</returns>
    public readonly BigDecimal Floor()
    {
        BigDecimal trunc = Truncate();
        return Sign < 0 ? trunc - One : trunc;
    }

    /// <summary>
    /// Returns the smallest integral value greater than or equal to the current BigDecimal value.
    /// </summary>
    /// <returns>
    /// A BigDecimal representing the smallest integer greater than or equal to this value.
    /// If the value is already an integer, the same value is returned.
    /// </returns>
    public readonly BigDecimal Ceiling()
    {
        BigDecimal trunc = Truncate();
        return Sign > 0 ? trunc + One : trunc;
    }

    /// <summary>
    /// Returns a new BigDecimal rounded to the specified number of decimal places using rounding to nearest,
    /// with ties rounded away from zero.
    /// </summary>
    /// <remarks>
    /// This method uses rounding to nearest, with ties rounded away from zero (also known as "round half up").
    /// The original BigDecimal instance is not modified.
    /// </remarks>
    /// <param name="precision">The number of decimal places to round to. Must be zero or greater. A value of 0 rounds to the nearest integer.</param>
    /// <returns>
    /// A new BigDecimal instance rounded to the specified precision. If the current value already has equal or fewer
    /// decimal places than the specified precision, the original value is returned.
    /// </returns>
    public readonly BigDecimal Round(int precision = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(precision);

        if (scale <= precision) return this;

        var p = BigInteger.Pow(BigIntegerTen, scale - precision);
        var q = BigInteger.DivRem(unscaled, p, out var r);
        if (r >= p / 2L) q += BigInteger.One;

        return new BigDecimal(q, precision);
    }
    
    /// <summary>
    /// Converts the current value to a byte array representation that encodes both the unscaled value and scale.
    /// </summary>
    /// <remarks>
    /// The returned byte array includes the absolute value of the unscaled integer followed by a
    /// 4-byte segment encoding the scale and sign. This format is suitable for serialization and interoperability
    /// scenarios where both the numeric value and its scale must be preserved.
    /// </remarks>
    /// <returns>
    /// A byte array containing the unscaled value and scale of the current instance.
    /// The array can be used to reconstruct the original value.
    /// </returns>
    public readonly byte[] ToByteArray()
    {
        byte[] unscaledBytes = BigInteger.Abs(unscaled).ToByteArray();
        var bytes = new byte[unscaledBytes.Length + 4];
        Array.Copy(unscaledBytes, bytes, unscaledBytes.Length);

        var dword = (uint) scale;
        if (Sign < 0) dword |= 0x80000000;
        Array.Copy(BitConverter.GetBytes(dword), 0, bytes, unscaledBytes.Length, 4);

        return bytes;
    }

    #endregion

    #region Operators

    #region Conversion from other types

    public static implicit operator BigDecimal(sbyte n) => new (n);

    public static implicit operator BigDecimal(byte n) => new (n);

    public static implicit operator BigDecimal(short n) => new (n);

    public static implicit operator BigDecimal(ushort n) => new (n);

    public static implicit operator BigDecimal(int n) => new (n);

    public static implicit operator BigDecimal(uint n) => new (n);

    public static implicit operator BigDecimal(long n) => new (n);

    public static implicit operator BigDecimal(ulong n) => new (n);

    public static implicit operator BigDecimal(BigInteger n) => new (n);

    public static explicit operator BigDecimal(float x) => new (x);

    public static explicit operator BigDecimal(double x) => new (x);

    public static implicit operator BigDecimal(decimal d) => new (d);

    public static explicit operator BigDecimal(Fraction d) =>
        FromRational(d.Numerator, d.Denominator, MAX_SCALE, MidpointRounding.ToEven);

    #endregion

    #region Conversion to other types

    public static explicit operator sbyte(BigDecimal self) => self.ToSByte(null);

    public static explicit operator byte(BigDecimal self) => self.ToByte(null);

    public static explicit operator short(BigDecimal self) => self.ToInt16(null);

    public static explicit operator ushort(BigDecimal self) => self.ToUInt16(null);

    public static explicit operator int(BigDecimal self) => self.ToInt32(null);

    public static explicit operator uint(BigDecimal self) => self.ToUInt32(null);

    public static explicit operator long(BigDecimal self) => self.ToInt64(null);

    public static explicit operator ulong(BigDecimal self) => self.ToUInt64(null);

    public static explicit operator float(BigDecimal self) => self.ToSingle(null);

    public static explicit operator double(BigDecimal self) => self.ToDouble(null);

    public static explicit operator decimal(BigDecimal self) => self.ToDecimal(null);

    public static explicit operator BigInteger(BigDecimal self) => self.Round().unscaled;

    public static implicit operator Fraction(BigDecimal self)
    {
        var (num, den) = self.ToRational();
        return new Fraction(num, den);
    }

    #endregion

    #region Relational

    public static bool operator ==(BigDecimal a, BigDecimal b) => a.Equals(b);

    public static bool operator !=(BigDecimal a, BigDecimal b) => !a.Equals(b);

    public static bool operator <(BigDecimal a, BigDecimal b) => a.CompareTo(b) < 0;

    public static bool operator >(BigDecimal a, BigDecimal b) => a.CompareTo(b) > 0;

    public static bool operator <=(BigDecimal a, BigDecimal b) => a.CompareTo(b) <= 0;

    public static bool operator >=(BigDecimal a, BigDecimal b) => a.CompareTo(b) >= 0;

    #endregion

    #region Arithmetic

    public static BigDecimal operator +(BigDecimal a) => a;

    public static BigDecimal operator -(BigDecimal a) => a.Opposite();

    public static BigDecimal operator +(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Add(a, b.Inflate(a.scale))
             : Add(a.Inflate(b.scale), b);
    }

    public static BigDecimal operator -(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Subtract(a, b.Inflate(a.scale))
             : Subtract(a.Inflate(b.scale), b);
    }

    public static BigDecimal operator *(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Multiply(a, b.Inflate(a.scale))
             : Multiply(a.Inflate(b.scale), b);
    }

    public static BigDecimal operator /(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Divide(a, b.Inflate(a.scale))
             : Divide(a.Inflate(b.scale), b);
    }

    public static BigDecimal operator %(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Modulo(a, b.Inflate(a.scale))
             : Modulo(a.Inflate(b.scale), b);
    }

    #endregion

    #endregion

    #region Private Methods

    [GeneratedRegex(
        @"^(?<SIGN>\+|\-)?(?<INTEGER>\d+)(\.(?<DECIMALS>\d+))?(?<EXPONENT>(e|E)(\+|\-)?\d+)?$",
        RegexOptions.Compiled)]
    private static partial Regex GetDecimalRegex();

    private readonly BigDecimal Inflate(int decs)
    {
        if (decs <= scale) return this;

        var pow10 = BigInteger.Pow(BigIntegerTen, decs - scale);
        return new BigDecimal(unscaled * pow10, decs);
    }

    private readonly BigDecimal Deflate()
    {
        if (scale <= 0) return this;

        BigInteger n = unscaled;
        int decs = scale;
        
        while (decs > 0)
        {
            var q = BigInteger.DivRem(n, BigIntegerTen, out var r);
            if (!r.IsZero) break;

            n = q;
            --decs;
        }

        return new BigDecimal(n, decs);
    }

    private readonly BigDecimal Opposite() => new (-unscaled, scale);

    private static int Compare(BigDecimal a, BigDecimal b)
    {
        // Assuming a.scale == b.scale
        return a.unscaled.CompareTo(b.unscaled);
    }

    private static BigDecimal Add(BigDecimal a, BigDecimal b)
    {
        // Assuming a.scale == b.scale
        return new BigDecimal(a.unscaled + b.unscaled, a.scale).Deflate();
    }

    private static BigDecimal Subtract(BigDecimal a, BigDecimal b)
    {
        // Assuming a.scale == b.scale
        return new BigDecimal(a.unscaled - b.unscaled, a.scale).Deflate();
    }

    private static BigDecimal Multiply(BigDecimal a, BigDecimal b)
    {
        // Assuming a.scale == b.scale
        return new BigDecimal(a.unscaled * b.unscaled, a.scale + b.scale).Deflate();
    }

    private static BigDecimal Divide(BigDecimal a, BigDecimal b)
    {
        // Assuming a.scale == b.scale
        var q = BigInteger.DivRem(a.unscaled, b.unscaled, out var r);
        var decs = 0;
        
        while (decs < MAX_SCALE && !r.IsZero)
        {
            var q1 = BigInteger.DivRem(r * BigIntegerTen, b.unscaled, out var r1);
            q = q * BigIntegerTen + q1;
            r = r1;
            ++decs;
        }

        return new BigDecimal(q, decs).Deflate();
    }

    private static BigDecimal Modulo(BigDecimal a, BigDecimal b)
    {
        // Assuming a.scale == b.scale
        _ = BigInteger.DivRem(a.unscaled, b.unscaled, out var r);
        return new BigDecimal(r, a.scale).Deflate();
    }
    
    private static BigDecimal FromRational(BigInteger numerator, BigInteger denominator,
                                           int scale, MidpointRounding rounding)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scale);

        var factor = BigInteger.Pow(BigIntegerTen, scale);
        var scaled = numerator * factor;
        var q = BigInteger.DivRem(scaled, denominator, out var r);

        if (!r.IsZero)
        {
            var roundUp = rounding switch
            {
                MidpointRounding.AwayFromZero =>
                    BigInteger.Abs(r) * 2 >= BigInteger.Abs(denominator),
                MidpointRounding.ToEven =>
                    BigInteger.Abs(r) * 2 > BigInteger.Abs(denominator) ||
                    (BigInteger.Abs(r) * 2 == BigInteger.Abs(denominator) && !q.IsEven),
                _ => throw new NotSupportedException()
            };

            if (roundUp) q += numerator.Sign;
        }

        return new BigDecimal(q, scale).Deflate();
    }

    private readonly (BigInteger, BigInteger) ToRational() => (unscaled, BigInteger.Pow(BigIntegerTen, scale));
    
    private readonly string ToScientificString()
    {
        var abs = BigInteger.Abs(unscaled);
        var digits = abs.ToString();
        var exponent = digits.Length - scale - 1;

        if (digits.Length == 1)
            return (unscaled.Sign < 0 ? "-" : "") + digits + "e" +
                   exponent.ToString(CultureInfo.InvariantCulture);

        return (unscaled.Sign < 0 ? "-" : "") + digits[0] + "." + digits[1..] + "e" +
               exponent.ToString(CultureInfo.InvariantCulture);
    }

    #endregion
}
