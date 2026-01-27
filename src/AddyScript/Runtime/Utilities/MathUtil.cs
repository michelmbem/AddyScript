using System;

using AddyScript.Properties;


namespace AddyScript.Runtime.Utilities;


public static class MathUtil
{
    public static bool Equal(double a, double b, double tolerance = double.Epsilon) =>
        Math.Abs(a - b) <= tolerance;

    public static int Pow(int n, int exp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(exp);

        if (exp == 0) return 1;

        int factor = n;
        int result = 1;

        while (exp != 0)
        {
            if ((exp & 1) != 0) result = checked(result * factor);
            if (exp == 1) break;
            factor = checked(factor * factor);
            exp >>= 1;
        }

        return result;
    }

    public static int Gcd(int a, int b)
    {
        if (a < 0) throw new ArgumentOutOfRangeException(nameof(a), Resources.CannotComputeGcdForNegative);
        if (b < 0) throw new ArgumentOutOfRangeException(nameof(b), Resources.CannotComputeGcdForNegative);

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

        long h1 = 1, h2 = 0;
        long k1 = 0, k2 = 1;

        double b = value;

        do
        {
            long a = (long)Math.Floor(b);
            long h = a * h1 + h2;
            long k = a * k1 + k2;

            double approx = (double)h / k;
            if (Equal(approx, value)) return (h, k);

            h2 = h1; h1 = h;
            k2 = k1; k1 = k;
            
            b = 1.0 / (b - a);
        } while (true);
    }
}
