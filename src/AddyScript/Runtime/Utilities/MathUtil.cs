using System;


namespace AddyScript.Runtime.Utilities;


public static class MathUtil
{
    public static int Power(int n, int exp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(exp);

        if (exp == 0) return 1;

        int factor = n;
        int result = 1;

        while (exp != 0)
        {
            if ((exp & 1) != 0)
                result = checked(result * factor);
            if (exp == 1) break;
            factor = checked(factor * factor);
            exp >>= 1;
        }

        return result;
    }

    public static int Gcd(int a, int b)
    {
        if (a < 0) throw new ArgumentOutOfRangeException(nameof(a), "Cannot compute the Gcd of negative numbers");
        if (b < 0) throw new ArgumentOutOfRangeException(nameof(b), "Cannot compute the Gcd of negative numbers");

        while (b > 0)
        {
            int r = a % b;
            a = b;
            b = r;
        }

        return a;
    }

    public static double Modulo(double a, double b)
    {
        double c = a / b;
        double d = c - Math.Truncate(c);
        return d * b;
    }

    public static void Decompose(double value, out ulong mantissa, out ushort exponent, out bool negative)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        uint lo = (bytes[0] | ((uint) bytes[1] << 8) | ((uint) bytes[2] << 16) | ((uint) bytes[3] << 24));
        uint hi = (bytes[4] | ((uint) bytes[5] << 8) | ((uint) (bytes[6] & 0x0F) << 16));
        mantissa = lo | ((ulong) hi << 32);
        exponent = (ushort) ((((ushort) (bytes[7] & 0x7F)) << 4) | (((ushort) (bytes[6] & 0xF0)) >> 4));
        negative = ((bytes[7] & 0x80) != 0);
    }
}
