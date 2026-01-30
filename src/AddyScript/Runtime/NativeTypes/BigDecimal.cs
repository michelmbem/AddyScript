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

    public BigDecimal(BigInteger i) : this(i, 0) { }

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
    public readonly bool ToBoolean(IFormatProvider provider) => unscaled != BigInteger.Zero;

    /// <summary>
    /// Converts the value of this instance to an equivalent Unicode character using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A Unicode character equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public char ToChar(IFormatProvider provider) => (char)Round(0).unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 8-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 8-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public sbyte ToSByte(IFormatProvider provider) => (sbyte)Round(0).unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 8-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 8-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public byte ToByte(IFormatProvider provider) => (byte)Round(0).unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 16-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 16-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public short ToInt16(IFormatProvider provider) => (short)Round(0).unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 16-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 16-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public ushort ToUInt16(IFormatProvider provider) => (ushort)Round(0).unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 32-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public int ToInt32(IFormatProvider provider) => (int)Round(0).unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 32-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public uint ToUInt32(IFormatProvider provider) => (uint)Round(0).unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 64-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 64-bit signed integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public long ToInt64(IFormatProvider provider) => (long)Round(0).unscaled;

    /// <summary>
    /// Converts the value of this instance to an equivalent 64-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// An 64-bit unsigned integer equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public ulong ToUInt64(IFormatProvider provider) => (ulong)Round(0).unscaled;

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
            return 0.0f * (unscaled.Sign < 0 ? -1.0f : 1.0f);

        // float range:
        // max ≈ 3.4028235e+38
        // min normal ≈ 1.17549435e−38
        // min subnormal ≈ 1.401298e−45
        if (scale is < -45 or > 38)
            return unscaled.Sign < 0 ? float.NegativeInfinity : float.PositiveInfinity;

        // Convert via canonical scientific decimal string
        // Let the CLR perform IEEE-754 rounding
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

        // Fast overflow check (decimal exponent too large for double)
        // double max ≈ 1.7976931348623157E308
        if (scale is < -324 or > 308)
            return unscaled.Sign < 0 ? double.NegativeInfinity : double.PositiveInfinity;

        // Convert via canonical decimal string
        // This guarantees IEEE-754 round-to-nearest-even
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
        // decimal supports only scale 0–28
        if (scale is < 0 or > 28)
            throw new OverflowException("Scale out of range for decimal");

        // decimal mantissa is 96 bits
        var abs = BigInteger.Abs(unscaled);
        if (abs.GetBitLength() > 96)
            throw new OverflowException("Magnitude too large for decimal");

        // Extract 96-bit integer
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
    public DateTime ToDateTime(IFormatProvider provider) => throw new InvalidCastException();

    /// <summary>
    /// Converts the value of this instance to an equivalent <see cref="string" /> using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> instance equivalent to the value of this instance.
    /// </returns>
    /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <filterpriority>2</filterpriority>
    public string ToString(IFormatProvider provider) => ToString();

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
        return obj is BigDecimal dec
             ? CompareTo(dec)
             : throw new ArgumentException("obj should be a BigDecimal");
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
    public readonly bool Equals(BigDecimal other) => this == other;

    #endregion

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

    public readonly override bool Equals(object obj) => obj is BigDecimal bd && Equals(bd);

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
        ArgumentOutOfRangeException.ThrowIfNegative(decs);

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

    #endregion

    #region Operators

    #region Conversion from other types

    public static implicit operator BigDecimal(sbyte n) => new (n);

    public static implicit operator BigDecimal(byte n) => new ((uint)n);

    public static implicit operator BigDecimal(short n) => new (n);

    public static implicit operator BigDecimal(ushort n) => new ((uint)n);

    public static implicit operator BigDecimal(int n) => new (n);

    public static implicit operator BigDecimal(uint n) => new (n);

    public static implicit operator BigDecimal(long n) => new (n);

    public static implicit operator BigDecimal(ulong n) => new (n);

    public static implicit operator BigDecimal(BigInteger i) => new (i);

    public static explicit operator BigDecimal(float x) => new (x);

    public static explicit operator BigDecimal(double x) => new (x);

    public static implicit operator BigDecimal(decimal d) => new (d);

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

    public static explicit operator BigInteger(BigDecimal self) => self.Round(0).unscaled;

    public static explicit operator Fraction(BigDecimal self)
    {
        var (num, den) = self.ToRational();
        return new Fraction(num, den);
    }

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
        BigInteger q = BigInteger.DivRem(a.unscaled, b.unscaled, out BigInteger r);
        int decs = 0;
        
        while (decs < MAX_SCALE && !r.IsZero)
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

    private readonly (long, long) ToRational() => ((long)unscaled, MathExt.Pow(10L, scale));
    
    private readonly string ToScientificString()
    {
        BigInteger abs = BigInteger.Abs(unscaled);
        string digits = abs.ToString();
        int exponent = digits.Length - scale - 1;

        if (digits.Length == 1)
            return (unscaled.Sign < 0 ? "-" : "") +
                   digits + "e" + exponent.ToString(CultureInfo.InvariantCulture);

        return (unscaled.Sign < 0 ? "-" : "") +
               digits[0] + "." + digits.Substring(1) +
               "e" + exponent.ToString(CultureInfo.InvariantCulture);
    }

    #endregion
}
