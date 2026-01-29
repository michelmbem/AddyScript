using System;

using AddyScript.Properties;


namespace AddyScript.Runtime.Utilities;


public static class MathExt
{
    public static bool Equal(double a, double b, double tolerance = double.Epsilon) =>
        Math.Abs(a - b) <= tolerance;

    public static long Pow(long n, int exp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(exp);

        if (exp == 0) return 1L;

        var (factor, result) = (n, 1L);

        while (exp != 0)
        {
            if ((exp & 1) != 0) result = checked(result * factor);
            if (exp == 1) break;
            factor = checked(factor * factor);
            exp >>= 1;
        }

        return result;
    }

    public static long Gcd(long a, long b)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(a);
        ArgumentOutOfRangeException.ThrowIfNegative(b);

        while (b > 0)
        {
            var r = a % b;
            a = b;
            b = r;
        }

        return a;
    }

    public static double Modulo(double a, double b)
    {
        var c = a / b;
        var d = c - Math.Truncate(c);
        return d * b;
    }

    public static (bool, ushort, ulong) Decompose(double value)
    {
        // binary format: sign[1]exponent[11]mantissa[52]
        var bits = BitConverter.DoubleToUInt64Bits(value);
        var negative = bits >> 63 != 0UL;
        var exponent = (ushort)((bits >> 52) & 0x7FFUL);
        var mantissa = bits & 0xFFFFFFFFFFFFFUL;
        
        return (negative, exponent, mantissa);
    }

    public static (long, long) ToRational(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("Value must be finite.");

        // Depends on the number of significant decimal digits we want in the input (16, for instance)
        const double tolerance = 1e-16;
        
        var (h1, h2, k1, k2) = (1L, 0L, 0L, 1L);
        var b = value;

        while (true)
        {
            var a = (long)Math.Floor(b);
            var h = a * h1 + h2;
            var k = a * k1 + k2;

            var approx = (double)h / k;
            if (Equal(approx, value, tolerance)) return (h, k);

            (h2, h1, k2, k1) = (h1, h, k1, k);
            b = 1.0 / (b - a);
        }
    }
}
