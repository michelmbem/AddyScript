using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;


namespace AddyScript.Runtime.NativeTypes
{
    [Serializable]
    public sealed class BigDecimal
        : IFormattable, IConvertible, IComparable,
          IComparable<BigDecimal>, IEquatable<BigDecimal>
    {
        #region Fields

        private const int MAX_SCALE = 50; // Note: Well, this is an arbitrary value!

        public static readonly BigDecimal MinusOne = new BigDecimal(BigInteger.MinusOne, 0);
        public static readonly BigDecimal Zero = new BigDecimal(BigInteger.Zero, 0);
        public static readonly BigDecimal One = new BigDecimal(BigInteger.One, 0);
        public static readonly BigDecimal Ten = new BigDecimal(new BigInteger(10), 0);
        public static readonly BigDecimal PointOne = new BigDecimal(BigInteger.One, 1);
        
        private static readonly Regex DecimalRegex = new Regex(@"^(?<SIGN>\+|\-)?(?<INTEGER>\d+)(\.(?<DECIMALS>\d+))?(?<EXPONENT>(e|E)(\+|\-)?\d+)?$", RegexOptions.Compiled);

        private readonly BigInteger unscaled;
        private readonly int scale;

        #endregion

        #region Constructors

        private BigDecimal(BigInteger unscaled, int scale)
        {
            this.unscaled = unscaled;
            this.scale = scale;
        }

        public BigDecimal(int n)
        {
            unscaled = new BigInteger(n);
            scale = 0;
        }

        public BigDecimal(uint n)
        {
            unscaled = new BigInteger(n);
            scale = 0;
        }

        public BigDecimal(long n)
        {
            unscaled = new BigInteger(n);
            scale = 0;
        }

        public BigDecimal(ulong n)
        {
            unscaled = new BigInteger(n);
            scale = 0;
        }

        public BigDecimal(BigInteger i)
        {
            unscaled = i;
            scale = 0;
        }

        public BigDecimal(float x)
            : this(x.ToString(CultureInfo.InvariantCulture))
        {
        }

        public BigDecimal(double x)
            : this(x.ToString(CultureInfo.InvariantCulture))
        {
        }

        public BigDecimal(decimal d)
            : this(d.ToString(CultureInfo.InvariantCulture))
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
            Group signGroup = match.Groups[DecimalRegex.GroupNumberFromName("SIGN")];
            if (signGroup.Success) sb.Append(signGroup.Value);

            Group integerGroup = match.Groups[DecimalRegex.GroupNumberFromName("INTEGER")];
            if (integerGroup.Success) sb.Append(integerGroup.Value);

            int decs = 0;
            Group decimalsGroup = match.Groups[DecimalRegex.GroupNumberFromName("DECIMALS")];
            if (decimalsGroup.Success)
            {
                decs = decimalsGroup.Length;
                sb.Append(decimalsGroup.Value);
            }

            Group exponentGroup = match.Groups[DecimalRegex.GroupNumberFromName("EXPONENT")];
            if (exponentGroup.Success) decs -= int.Parse(exponentGroup.Value.Substring(1));

            if (decs < 0)
            {
                sb.Append('0', -decs);
                decs = 0;
            }

            var tmp = new BigDecimal(BigInteger.Parse(sb.ToString()), decs);
            tmp = tmp.Deflate();
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
                dword &= 0x7fffffff;
            }

            scale = (int) dword;
        }

        #endregion

        #region Properties

        public sbyte Sign
        {
            get { return (sbyte) unscaled.Sign; }
        }

        public int Scale
        {
            get { return scale; }
        }

        #endregion

        #region Public Methods

        #region Implementation of IFormattable

        /// <summary>
        /// Formats the value of the current instance using the specified format.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String" /> containing the value of the current instance in the specified format.
        /// </returns>
        /// <param name="format">The <see cref="T:System.String" /> specifying the format to use.-or- null to use the default format defined for the type of the <see cref="T:System.IFormattable" /> implementation. </param>
        /// <param name="formatProvider">The <see cref="T:System.IFormatProvider" /> to use to format the value.-or- null to obtain the numeric format information from the current locale setting of the operating system. </param><filterpriority>2</filterpriority>
        public string ToString(string format, IFormatProvider formatProvider)
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
        /// Returns the <see cref="T:System.TypeCode" /> for this instance.
        /// </summary>
        /// <returns>
        /// The enumerated constant that is the <see cref="T:System.TypeCode" /> of the class or value type that implements this interface.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent Boolean value using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A Boolean value equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public bool ToBoolean(IFormatProvider provider)
        {
            return unscaled != BigInteger.Zero;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent Unicode character using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A Unicode character equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public char ToChar(IFormatProvider provider)
        {
            return (char) Round(0).unscaled;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent 8-bit signed integer using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// An 8-bit signed integer equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public sbyte ToSByte(IFormatProvider provider)
        {
            return (sbyte) Round(0).unscaled;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent 8-bit unsigned integer using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// An 8-bit unsigned integer equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public byte ToByte(IFormatProvider provider)
        {
            return (byte) Round(0).unscaled;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent 16-bit signed integer using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// An 16-bit signed integer equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public short ToInt16(IFormatProvider provider)
        {
            return (short) Round(0).unscaled;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent 16-bit unsigned integer using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// An 16-bit unsigned integer equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public ushort ToUInt16(IFormatProvider provider)
        {
            return (ushort) Round(0).unscaled;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// An 32-bit signed integer equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public int ToInt32(IFormatProvider provider)
        {
            return (int) Round(0).unscaled;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// An 32-bit unsigned integer equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public uint ToUInt32(IFormatProvider provider)
        {
            return (uint) Round(0).unscaled;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent 64-bit signed integer using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// An 64-bit signed integer equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public long ToInt64(IFormatProvider provider)
        {
            return (long) Round(0).unscaled;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent 64-bit unsigned integer using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// An 64-bit unsigned integer equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public ulong ToUInt64(IFormatProvider provider)
        {
            return (ulong) Round(0).unscaled;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent single-precision floating-point number using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A single-precision floating-point number equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public float ToSingle(IFormatProvider provider)
        {
            return (float) unscaled / (float) Math.Pow(10.0, scale);
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent double-precision floating-point number using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A double-precision floating-point number equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public double ToDouble(IFormatProvider provider)
        {
            return (double) unscaled / Math.Pow(10.0, scale);
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent <see cref="T:System.Decimal" /> number using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Decimal" /> number equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public decimal ToDecimal(IFormatProvider provider)
        {
            return (decimal) unscaled / (decimal) Math.Pow(10.0, scale);
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent <see cref="T:System.DateTime" /> using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.DateTime" /> instance equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent <see cref="T:System.String" /> using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String" /> instance equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        /// <summary>
        /// Converts the value of this instance to an <see cref="T:System.Object" /> of the specified <see cref="T:System.Type" /> that has an equivalent value, using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Object" /> instance of type <paramref name="conversionType" /> whose value is equivalent to the value of this instance.
        /// </returns>
        /// <param name="conversionType">The <see cref="T:System.Type" /> to which the value of this instance is converted. </param>
        /// <param name="provider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information. </param><filterpriority>2</filterpriority>
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
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj" />. Zero This instance is equal to <paramref name="obj" />. Greater than zero This instance is greater than <paramref name="obj" />. 
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param>
        /// <exception cref="T:System.ArgumentException"><paramref name="obj" /> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
        public int CompareTo(object obj)
        {
            if (obj is BigDecimal)
                return CompareTo((BigDecimal) obj);

            throw new ArgumentException("obj");
        }

        #endregion

        #region Implementation of IComparable<BigDecimal>

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />. 
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(BigDecimal other)
        {
            if (ReferenceEquals(other, null))
                throw new ArgumentNullException("other");

            if (this < other) return -1;
            if (this > other) return +1;
            return 0;
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
        public bool Equals(BigDecimal other)
        {
            if (ReferenceEquals(other, null))
                throw new ArgumentNullException("other");

            return this == other;
        }

        #endregion

        public override bool Equals(object obj)
        {
            return obj is BigDecimal && this == (BigDecimal) obj;
        }

        public override int GetHashCode()
        {
            return unscaled.GetHashCode() + scale;
        }

        public override string ToString()
        {
            return ToString(null, CultureInfo.CurrentUICulture);
        }

        public BigDecimal Power(int exp)
        {
            return new BigDecimal(BigInteger.Pow(unscaled, exp), scale * exp);
        }

        public BigDecimal Abs()
        {
            return new BigDecimal(BigInteger.Abs(unscaled), scale);
        }

        public BigDecimal Truncate()
        {
            if (scale <= 0) return this;
            BigInteger powers = BigInteger.Pow(new BigInteger(10), scale);
            return new BigDecimal(unscaled / powers);
        }

        public BigDecimal Floor()
        {
            BigDecimal trunc = Truncate();
            return Sign < 0 ? trunc - One : trunc;
        }

        public BigDecimal Ceiling()
        {
            BigDecimal trunc = Truncate();
            return Sign > 0 ? trunc + One : trunc;
        }

        public BigDecimal Round(int decs)
        {
            if (decs < 0) throw new ArgumentOutOfRangeException();
            if (scale <= decs) return this;

            BigInteger q, r, p = BigInteger.Pow(new BigInteger(10), scale - decs);
            q = BigInteger.DivRem(unscaled, p, out r);
            if (r >= p / 2) q += BigInteger.One;

            return new BigDecimal(q, decs);
        }
        
        public byte[] ToByteArray()
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

        #region Conversion

        public static implicit operator BigDecimal(sbyte n)
        {
            return new BigDecimal(n);
        }

        public static implicit operator BigDecimal(byte n)
        {
            return new BigDecimal((uint) n);
        }

        public static implicit operator BigDecimal(short n)
        {
            return new BigDecimal(n);
        }

        public static implicit operator BigDecimal(ushort n)
        {
            return new BigDecimal((uint) n);
        }

        public static implicit operator BigDecimal(int n)
        {
            return new BigDecimal(n);
        }

        public static implicit operator BigDecimal(uint n)
        {
            return new BigDecimal(n);
        }

        public static implicit operator BigDecimal(long n)
        {
            return new BigDecimal(n);
        }

        public static implicit operator BigDecimal(ulong n)
        {
            return new BigDecimal(n);
        }

        public static implicit operator BigDecimal(BigInteger i)
        {
            return new BigDecimal(i);
        }

        public static explicit operator BigDecimal(float x)
        {
            return new BigDecimal(x);
        }

        public static explicit operator BigDecimal(double x)
        {
            return new BigDecimal(x);
        }

        public static implicit operator BigDecimal(decimal d)
        {
            return new BigDecimal(d);
        }

        public static explicit operator sbyte(BigDecimal self)
        {
            return self.ToSByte(null);
        }

        public static explicit operator byte(BigDecimal self)
        {
            return self.ToByte(null);
        }

        public static explicit operator short(BigDecimal self)
        {
            return self.ToInt16(null);
        }

        public static explicit operator ushort(BigDecimal self)
        {
            return self.ToUInt16(null);
        }

        public static explicit operator int(BigDecimal self)
        {
            return self.ToInt32(null);
        }

        public static explicit operator uint(BigDecimal self)
        {
            return self.ToUInt32(null);
        }

        public static explicit operator long(BigDecimal self)
        {
            return self.ToInt64(null);
        }

        public static explicit operator ulong(BigDecimal self)
        {
            return self.ToUInt64(null);
        }

        public static explicit operator float(BigDecimal self)
        {
            return self.ToSingle(null);
        }

        public static explicit operator double(BigDecimal self)
        {
            return self.ToDouble(null);
        }

        public static explicit operator decimal(BigDecimal self)
        {
            return self.ToDecimal(null);
        }

        public static explicit operator BigInteger(BigDecimal self)
        {
            return self.Round(0).unscaled;
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

        public static BigDecimal operator -(BigDecimal a)
        {
            return a.Opposite();
        }

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

        private BigDecimal Inflate(int decs)
        {
            if (decs <= scale) return this;
            BigInteger powers = BigInteger.Pow(new BigInteger(10), decs - scale);
            return new BigDecimal(unscaled * powers, decs);
        }

        private BigDecimal Deflate()
        {
            if (scale <= 0) return this;

            BigInteger bigTen = new BigInteger(10);
            BigInteger i = unscaled;
            int decs = scale;
            
            while (decs > 0)
            {
                BigInteger q, r;
                q = BigInteger.DivRem(i, bigTen, out r);
                if (r != BigInteger.Zero) break;
                i = q;
                --decs;
            }

            return new BigDecimal(i, decs);
        }

        private BigDecimal Opposite()
        {
            return new BigDecimal(-unscaled, scale);
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
            BigInteger q, r;
            q = BigInteger.DivRem(a.unscaled, b.unscaled, out r);

            BigInteger bigTen = new BigInteger(10);
            int decs = 0;
            
            while (decs < MAX_SCALE && r != BigInteger.Zero)
            {
                BigInteger q1, r1;
                q1 = BigInteger.DivRem(r * bigTen, b.unscaled, out r1);
                q = q * bigTen + q1;
                r = r1;
                ++decs;
            }

            return new BigDecimal(q, decs).Deflate();
        }

        private static BigDecimal Modulo(BigDecimal a, BigDecimal b)
        {
            // Assuming a.scale == b.scale
            BigInteger q, r;
            q = BigInteger.DivRem(a.unscaled, b.unscaled, out r);
            return new BigDecimal(r, a.scale).Deflate();
        }

        #endregion
    }
}
