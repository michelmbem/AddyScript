using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.NativeTypes;


[Serializable]
public partial struct BigDecimal :
    IFormattable, IConvertible, IComparable, IComparable<BigDecimal>, IEquatable<BigDecimal>
{
    #region Fields

    private const int MAX_SCALE = 50; // Note: Well, this is an arbitrary value!

    public static readonly BigDecimal MinusOne = new (BigInteger.MinusOne, 0);
    public static readonly BigDecimal Zero = new (BigInteger.Zero, 0);
    public static readonly BigDecimal One = new (BigInteger.One, 0);
    public static readonly BigDecimal Ten = new (BigIntegerTen, 0);
    public static readonly BigDecimal PointOne = new (BigInteger.One, 1);

    private static readonly BigInteger BigIntegerTen = new (10);
    private static readonly Regex DecimalRegex = GetDecimalRegex();

    private readonly BigInteger unscaled;
    private readonly int scale;

    #endregion

    #region Constructors

    private BigDecimal(BigInteger unscaled, int scale)
    {
        this.unscaled = unscaled;
        this.scale = scale;
    }

    public BigDecimal(int n) : this(new BigInteger(n))
    {
    }

    public BigDecimal(uint n) : this(new BigInteger(n))
    {
    }

    public BigDecimal(long n) : this(new BigInteger(n))
    {
    }

    public BigDecimal(ulong n) : this(new BigInteger(n))
    {
    }

    public BigDecimal(BigInteger i) : this(i, 0)
    {
    }

    public BigDecimal(float x) : this(x.ToString(CultureInfo.InvariantCulture))
    {
    }

    public BigDecimal(double x) : this(x.ToString(CultureInfo.InvariantCulture))
    {
    }

    public BigDecimal(decimal d) : this(d.ToString(CultureInfo.InvariantCulture))
    {
    }

    public BigDecimal(string s)
    {
        if (string.IsNullOrEmpty(s)) throw new FormatException();

        switch (s[0])
        {
            case '.':
                s = "0" + s;
                break;
            case '+':
            case '-':
                if (s.Length > 1 && s[1] == '.')
                    s = s.Insert(1, "0");
                break;
        }

        Match match = DecimalRegex.Match(s);
        if (!match.Success) throw new FormatException();

        var sb = new StringBuilder();

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

        var tmp = new BigDecimal(BigInteger.Parse(sb.ToString()), decs).Deflate();
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

    public readonly int Sign => unscaled.Sign;
    
    public readonly BigInteger Unscaled => unscaled;
    
    public readonly int Scale => scale;

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
        var sb = new StringBuilder(BigInteger.Abs(unscaled).ToString());
        
        if (scale > 0)
        {
            if (scale >= sb.Length) sb.Insert(0, "0", scale - sb.Length + 1);
            var fmt = (NumberFormatInfo) formatProvider.GetFormat(typeof(NumberFormatInfo));
            sb.Insert(sb.Length - scale, fmt.CurrencyDecimalSeparator);
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
    public readonly TypeCode GetTypeCode()
    {
        return TypeCode.Object;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent Boolean value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A Boolean value equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public readonly bool ToBoolean(IFormatProvider provider)
    {
        return unscaled != BigInteger.Zero;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent Unicode character using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A Unicode character equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public char ToChar(IFormatProvider provider)
    {
        return (char)Round(0).unscaled;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 8-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 8-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public sbyte ToSByte(IFormatProvider provider)
    {
        return (sbyte)Round(0).unscaled;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 8-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 8-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public byte ToByte(IFormatProvider provider)
    {
        return (byte)Round(0).unscaled;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 16-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 16-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public short ToInt16(IFormatProvider provider)
    {
        return (short)Round(0).unscaled;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 16-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 16-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public ushort ToUInt16(IFormatProvider provider)
    {
        return (ushort)Round(0).unscaled;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 32-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public int ToInt32(IFormatProvider provider)
    {
        return (int)Round(0).unscaled;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 32-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public uint ToUInt32(IFormatProvider provider)
    {
        return (uint)Round(0).unscaled;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 64-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 64-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public long ToInt64(IFormatProvider provider)
    {
        return (long)Round(0).unscaled;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 64-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 64-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public ulong ToUInt64(IFormatProvider provider)
    {
        return (ulong)Round(0).unscaled;
    }

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
        return (float)unscaled / (float)Math.Pow(10.0, scale);
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
        return (double)unscaled / Math.Pow(10.0, scale);
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
        return (decimal)unscaled / (decimal)Math.Pow(10.0, scale);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent <see cref="DateTime" /> using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A <see cref="DateTime" /> instance equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public DateTime ToDateTime(IFormatProvider provider)
    {
        throw new InvalidCastException();
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent <see cref="string" /> using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> instance equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public string ToString(IFormatProvider provider)
    {
        return ToString();
    }

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
        if (conversionType == typeof(BigDecimal))
            return this;
        if (conversionType == typeof(BigInteger))
            return Round(0).unscaled;
        throw new InvalidCastException();
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
    public int CompareTo(object obj)
    {
        if (obj is BigDecimal dec)
            return CompareTo(dec);

        throw new ArgumentException("obj should be a BigDecimal");
    }

    #endregion

    #region Implementation of IComparable<BigDecimal>

    /// <summary>
    /// Compares the current object with another object of the same type.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that indicates the relative order of the objects being compared.
    /// The return value has these meanings:
    /// Less than zero: this instance is less than <paramref name="obj" />.
    /// Zero: this instance is equal to <paramref name="obj" />.
    /// Greater than zero: this instance is greater than <paramref name="obj" />.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public readonly int CompareTo(BigDecimal other)
    {
        if (this == other) return 0;
        return this < other ? -1 : 1;
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
        return this == other;
    }

    #endregion

    public readonly override bool Equals(object obj) => obj is BigDecimal dec && this == dec;

    public readonly override int GetHashCode() => HashCode.Combine(unscaled, scale);

    public readonly override string ToString() => ToString(null, CultureInfo.CurrentUICulture);

    public readonly BigDecimal Pow(int exp) => new (BigInteger.Pow(unscaled, exp), scale * exp);

    public readonly BigDecimal Abs() => new (BigInteger.Abs(unscaled), scale);

    public readonly BigDecimal Truncate()
    {
        if (scale <= 0) return this;
        BigInteger powers = BigInteger.Pow(BigIntegerTen, scale);
        return new (unscaled / powers);
    }

    public readonly BigDecimal Floor()
    {
        BigDecimal trunc = Truncate();
        return Sign < 0 ? trunc - One : trunc;
    }

    public readonly BigDecimal Ceiling()
    {
        BigDecimal trunc = Truncate();
        return Sign > 0 ? trunc + One : trunc;
    }

    public readonly BigDecimal Round(int decs)
    {
        if (decs < 0) throw new ArgumentOutOfRangeException();
        if (scale <= decs) return this;

        BigInteger p = BigInteger.Pow(BigIntegerTen, scale - decs);
        BigInteger q = BigInteger.DivRem(unscaled, p, out BigInteger r);
        if (r >= p / 2) q += BigInteger.One;

        return new (q, decs);
    }
    
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

    public readonly (long, long) ToRational() => ((long)unscaled, MathUtil.Pow(10, scale));

    #endregion

    #region Operators

    #region Conversion

    public static implicit operator BigDecimal(sbyte n) => new (n);

    public static implicit operator BigDecimal(byte n) => new ((uint) n);

    public static implicit operator BigDecimal(short n) => new (n);

    public static implicit operator BigDecimal(ushort n) => new ((uint) n);

    public static implicit operator BigDecimal(int n) => new (n);

    public static implicit operator BigDecimal(uint n) => new (n);

    public static implicit operator BigDecimal(long n) => new (n);

    public static implicit operator BigDecimal(ulong n) => new (n);

    public static implicit operator BigDecimal(BigInteger i) => new (i);

    public static explicit operator BigDecimal(float x) => new (x);

    public static explicit operator BigDecimal(double x) => new (x);

    public static implicit operator BigDecimal(decimal d) => new (d);

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

    public static explicit operator BigInteger(BigDecimal self) => self.Round(0).unscaled;

    #endregion

    #region Relational

    public static bool operator ==(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Compare(a, b.Inflate(a.scale)) == 0
             : Compare(a.Inflate(b.scale), b) == 0;
    }

    public static bool operator !=(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Compare(a, b.Inflate(a.scale)) != 0
             : Compare(a.Inflate(b.scale), b) != 0;
    }

    public static bool operator <(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Compare(a, b.Inflate(a.scale)) < 0
             : Compare(a.Inflate(b.scale), b) < 0;
    }

    public static bool operator >(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Compare(a, b.Inflate(a.scale)) > 0
             : Compare(a.Inflate(b.scale), b) > 0;
    }

    public static bool operator <=(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Compare(a, b.Inflate(a.scale)) <= 0
             : Compare(a.Inflate(b.scale), b) <= 0;
    }

    public static bool operator >=(BigDecimal a, BigDecimal b)
    {
        return a.scale >= b.scale
             ? Compare(a, b.Inflate(a.scale)) >= 0
             : Compare(a.Inflate(b.scale), b) >= 0;
    }

    #endregion

    #region Arithmetic

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

        BigInteger powers = BigInteger.Pow(BigIntegerTen, decs - scale);
        return new (unscaled * powers, decs);
    }

    private readonly BigDecimal Deflate()
    {
        if (scale <= 0) return this;

        BigInteger i = unscaled;
        int decs = scale;
        
        while (decs > 0)
        {
            BigInteger q = BigInteger.DivRem(i, BigIntegerTen, out BigInteger r);
            if (r != BigInteger.Zero) break;

            i = q;
            --decs;
        }

        return new (i, decs);
    }

    private readonly BigDecimal Opposite()
    {
        return new (-unscaled, scale);
    }

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
        BigInteger q = BigInteger.DivRem(a.unscaled, b.unscaled, out BigInteger r);
        int decs = 0;
        
        while (decs < MAX_SCALE && r != BigInteger.Zero)
        {
            BigInteger q1 = BigInteger.DivRem(r * BigIntegerTen, b.unscaled, out BigInteger r1);
            q = q * BigIntegerTen + q1;
            r = r1;
            ++decs;
        }

        return new BigDecimal(q, decs).Deflate();
    }

    private static BigDecimal Modulo(BigDecimal a, BigDecimal b)
    {
        // Assuming a.scale == b.scale
        _ = BigInteger.DivRem(a.unscaled, b.unscaled, out BigInteger r);
        return new BigDecimal(r, a.scale).Deflate();
    }

    #endregion
}
