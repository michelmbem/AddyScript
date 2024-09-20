using System;
using System.Globalization;

using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.NativeTypes
{
    [Serializable]
    public readonly struct Rational32
        : IFormattable, IConvertible, IComparable, IComparable<Rational32>, IEquatable<Rational32>
    {
        #region Fields

        public static readonly Rational32 MinusOne = new (-1);
        public static readonly Rational32 Zero = new (0);
        public static readonly Rational32 One = new (1);
        public static readonly Rational32 Half = new (1, 2);
        public static readonly Rational32 Quarter = new (1, 4);

        private readonly int numerator;
        private readonly int denominator;

        #endregion

        #region Constructors

        public Rational32(int num, int den)
        {
            if (den == 0) throw new DivideByZeroException();

            numerator = num * Math.Sign(den);
            denominator = Math.Abs(den);
        }

        public Rational32(int num)
        {
            numerator = num;
            denominator = 1;
        }

        #endregion

        #region Properties

        public int Numerator => numerator;
        public int Denominator => denominator;
        public int Sign => Math.Sign(numerator);

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
            if (denominator == 1) return numerator.ToString(format, formatProvider);
            return string.Format(formatProvider, "({0:" + format + "}/{1:" + format + "})", numerator, denominator);
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
        /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
        /// <filterpriority>2</filterpriority>
        public bool ToBoolean(IFormatProvider provider)
        {
            return (numerator != 0);
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
            return (char)(numerator / denominator);
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
            return (sbyte)(numerator / denominator);
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
            return (byte)(numerator / denominator);
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
            return (short)(numerator / denominator);
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
            return (ushort)(numerator / denominator);
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
            return numerator / denominator;
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
            return (uint)(numerator / denominator);
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
            return numerator / denominator;
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
            return (ulong)(numerator / denominator);
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent single-precision floating-point number using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A single-precision floating-point number equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
        /// <filterpriority>2</filterpriority>
        public float ToSingle(IFormatProvider provider)
        {
            return (float)numerator / denominator;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent double-precision floating-point number using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A double-precision floating-point number equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
        /// <filterpriority>2</filterpriority>
        public double ToDouble(IFormatProvider provider)
        {
            return (double)numerator / denominator;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent <see cref="T:System.Decimal" /> number using the specified culture-specific formatting information.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Decimal" /> number equivalent to the value of this instance.
        /// </returns>
        /// <param name="provider">An <see cref="IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
        /// <filterpriority>2</filterpriority>
        public decimal ToDecimal(IFormatProvider provider)
        {
            return (decimal)numerator / denominator;
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
            return ToString("g", provider);
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
            if (conversionType == typeof(Rational32))
                return this;

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
        /// <param name="obj">An object to compare with this instance.</param>
        /// <exception cref="ArgumentException"><paramref name="obj" /> is not the same type as this instance.</exception>
        /// <filterpriority>2</filterpriority>
        public int CompareTo(object obj)
        {
            if (obj is Rational32 rational)
                return CompareTo(rational);

            throw new ArgumentException("obj should be a Rational32");
        }

        #endregion

        #region Implementation of IComparable<Rational>

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
        public int CompareTo(Rational32 other)
        {
            if (this == other) return 0;
            return this < other ? -1 : 1;
        }

        #endregion

        #region Implementation of IEquatable<Rational>

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Rational32 other)
        {
            return this == other;
        }

        #endregion

        public override bool Equals(object obj)
        {
            return obj is Rational32 rational && this == rational;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(numerator, denominator);
        }

        public override string ToString()
        {
            return ToString("g", CultureInfo.CurrentUICulture);
        }

        public Rational32 Abs()
        {
            return new Rational32(Math.Abs(numerator), denominator);
        }

        public Rational32 Inverse()
        {
            return new Rational32(denominator, numerator);
        }

        public Rational32 Simplify()
        {
            if (numerator == 0 || denominator == 1) return this;
            int gcd = MathUtil.Gcd(Math.Abs(numerator), denominator);
            return gcd == 1 ? this : new Rational32(numerator / gcd, denominator / gcd);
        }

        public Rational32 Power(int exp)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(exp);

            if (exp == 0) return One;

            Rational32 factor = this;
            Rational32 result = One;

            while (exp != 0)
            {
                if ((exp & 1) != 0) result *= factor;
                if (exp == 1) break;
                factor *= factor;
                exp >>= 1;
            }

            return result;
        }

        public static void ToCommonDenominator(Rational32 a, Rational32 b, out Rational32 c, out Rational32 d)
        {
            if (a.denominator == b.denominator)
            {
                c = a;
                d = b;
            }
            else
            {
                int commonDeno = a.denominator * b.denominator;
                c = new Rational32(a.numerator * b.denominator, commonDeno);
                d = new Rational32(b.numerator * a.denominator, commonDeno);
            }
        }

        #endregion

        #region Operators

        #region Conversion

        public static implicit operator Rational32(sbyte n)
        {
            return new Rational32(n);
        }

        public static implicit operator Rational32(byte n)
        {
            return new Rational32(n);
        }

        public static implicit operator Rational32(short n)
        {
            return new Rational32(n);
        }

        public static implicit operator Rational32(ushort n)
        {
            return new Rational32(n);
        }

        public static implicit operator Rational32(int n)
        {
            return new Rational32(n);
        }

        public static explicit operator Rational32(uint n)
        {
            return new Rational32((int)n);
        }

        public static explicit operator Rational32(long n)
        {
            return new Rational32((int)n);
        }

        public static explicit operator Rational32(ulong n)
        {
            return new Rational32((int)n);
        }

        public static explicit operator Rational32(float x)
        {
            return new Rational32((int)x);
        }

        public static explicit operator Rational32(double x)
        {
            return new Rational32((int)x);
        }

        public static explicit operator Rational32(decimal d)
        {
            return new Rational32((int)d);
        }

        public static explicit operator sbyte(Rational32 self)
        {
            return self.ToSByte(null);
        }

        public static explicit operator byte(Rational32 self)
        {
            return self.ToByte(null);
        }

        public static explicit operator short(Rational32 self)
        {
            return self.ToInt16(null);
        }

        public static explicit operator ushort(Rational32 self)
        {
            return self.ToUInt16(null);
        }

        public static explicit operator int(Rational32 self)
        {
            return self.ToInt32(null);
        }

        public static explicit operator uint(Rational32 self)
        {
            return self.ToUInt32(null);
        }

        public static explicit operator long(Rational32 self)
        {
            return self.ToInt64(null);
        }

        public static explicit operator ulong(Rational32 self)
        {
            return self.ToUInt64(null);
        }

        public static explicit operator float(Rational32 self)
        {
            return self.ToSingle(null);
        }

        public static explicit operator double(Rational32 self)
        {
            return self.ToDouble(null);
        }

        public static explicit operator decimal(Rational32 self)
        {
            return self.ToDecimal(null);
        }

        #endregion

        #region Relational

        public static bool operator ==(Rational32 a, Rational32 b)
        {
            ToCommonDenominator(a, b, out Rational32 c, out Rational32 d);
            return c.numerator == d.numerator;
        }

        public static bool operator !=(Rational32 a, Rational32 b)
        {
            ToCommonDenominator(a, b, out Rational32 c, out Rational32 d);
            return c.numerator != d.numerator;
        }

        public static bool operator <(Rational32 a, Rational32 b)
        {
            ToCommonDenominator(a, b, out Rational32 c, out Rational32 d);
            return c.numerator < d.numerator;
        }

        public static bool operator >(Rational32 a, Rational32 b)
        {
            ToCommonDenominator(a, b, out Rational32 c, out Rational32 d);
            return c.numerator > d.numerator;
        }

        public static bool operator <=(Rational32 a, Rational32 b)
        {
            ToCommonDenominator(a, b, out Rational32 c, out Rational32 d);
            return c.numerator <= d.numerator;
        }

        public static bool operator >=(Rational32 a, Rational32 b)
        {
            ToCommonDenominator(a, b, out Rational32 c, out Rational32 d);
            return c.numerator >= d.numerator;
        }

        #endregion

        #region Arithmetic

        public static Rational32 operator -(Rational32 a)
        {
            return new Rational32(-a.numerator, a.denominator);
        }

        public static Rational32 operator +(Rational32 a, Rational32 b)
        {
            ToCommonDenominator(a, b, out Rational32 c, out Rational32 d);
            return new Rational32(c.numerator + d.numerator, c.denominator);
        }

        public static Rational32 operator -(Rational32 a, Rational32 b)
        {
            ToCommonDenominator(a, b, out Rational32 c, out Rational32 d);
            return new Rational32(c.numerator - d.numerator, c.denominator);
        }

        public static Rational32 operator *(Rational32 a, Rational32 b)
        {
            return new Rational32(a.numerator * b.numerator, a.denominator * b.denominator);
        }

        public static Rational32 operator /(Rational32 a, Rational32 b)
        {
            return new Rational32(a.numerator * b.denominator, a.denominator * b.numerator);
        }

        #endregion

        #endregion
    }
}
