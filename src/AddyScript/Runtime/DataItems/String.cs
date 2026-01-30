using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using AddyScript.Ast.Expressions;
using AddyScript.Properties;
using AddyScript.Runtime.NativeTypes;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;
using AddyScript.Translators;


namespace AddyScript.Runtime.DataItems;


public sealed class String(string value) : DataItem
{
    public override Class Class => Class.String;

    public override bool AsBoolean => value switch
    {
        Boolean.FALSE_STRING => false,
        Boolean.TRUE_STRING => true,
        _ => throw new FormatException()
    };

    public override int AsInt32
    {
        get
        {

            if (!(int.TryParse(value, CultureInfo.InvariantCulture, out int result) ||
                int.TryParse(value, CultureInfo.CurrentUICulture, out result)))
                throw new FormatException();

            return result;
        }
    }

    public override BigInteger AsBigInteger
    {
        get
        {

            if (!(BigInteger.TryParse(value, CultureInfo.InvariantCulture, out BigInteger result) ||
                BigInteger.TryParse(value, CultureInfo.CurrentUICulture, out result)))
                throw new FormatException();

            return result;
        }
    }

    public override double AsDouble
    {
        get
        {

            if (!(double.TryParse(value, CultureInfo.InvariantCulture, out double result) ||
                double.TryParse(value, CultureInfo.CurrentUICulture, out result)))
                throw new FormatException();

            return result;
        }
    }

    public override BigDecimal AsBigDecimal => BigDecimal.Parse(value);

    public override DateTime AsDateTime
    {
        get
        {

            if (!(DateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime result) ||
                DateTime.TryParse(value, CultureInfo.CurrentUICulture, out result)))
                throw new FormatException();

            return result;
        }
    }

    public override TimeSpan AsTimeSpan
    {
        get
        {

            if (!(TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan result) ||
                  TimeSpan.TryParse(value, CultureInfo.CurrentUICulture, out result)))
                throw new FormatException();

            return result;
        }
    }

    public override byte[] AsByteArray => StringUtil.String2ByteArray(value);

    private IEnumerable<DataItem> Chars =>
        value.ToCharArray().Select(c => new String(c.ToString()));

    public override DataItem[] AsArray => [.. Chars];

    public override List<DataItem> AsList => [.. Chars];

    public override HashSet<DataItem> AsHashSet => [.. Chars];

    public override object AsNativeObject => value;

    public override object Clone() => new String(value);

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        return format switch
        {
            "x" or "X" => CodeGenerator.EscapedString(value, false),
            _ => value,
        };
    }

    protected override bool UnsafeEquals(DataItem other) => value == other.ToString();

    public override int GetHashCode() => value.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other) =>
        string.CompareOrdinal(value, other.ToString());

    public override object ConvertTo(Type targetType)
    {
        return targetType switch
        {
            { IsEnum: true } => Enum.Parse(targetType, value),
            not null when targetType == typeof(char[]) => value.ToCharArray(),
            _ => base.ConvertTo(targetType)
        };
    }

    public override bool IsEmpty() => value.Length == 0;

    public override bool ConversionNeeded(Class targetClass, BinaryOperator _operator) => false;

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand) => _operator switch
    {
        BinaryOperator.Plus => new String(value + operand),
        BinaryOperator.Times => new String(StringUtil.Repeat(value, operand.AsInt32)),
        BinaryOperator.LessThan => Boolean.FromBool(string.CompareOrdinal(value, operand.ToString()) < 0),
        BinaryOperator.LessThanOrEqual => Boolean.FromBool(string.CompareOrdinal(value, operand.ToString()) <= 0),
        BinaryOperator.GreaterThan => Boolean.FromBool(string.CompareOrdinal(value, operand.ToString()) > 0),
        BinaryOperator.GreaterThanOrEqual => Boolean.FromBool(string.CompareOrdinal(value, operand.ToString()) >= 0),
        BinaryOperator.StartsWith => Boolean.FromBool(value.StartsWith(operand.ToString())),
        BinaryOperator.EndsWith => Boolean.FromBool(value.EndsWith(operand.ToString())),
        BinaryOperator.Contains => Boolean.FromBool(value.Contains(operand.ToString())),
        BinaryOperator.Matches => Boolean.FromBool(StringUtil.ToRegex(operand.ToString()).IsMatch(value)),
        _ => base.BinaryOperation(_operator, operand),
    };

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "empty" => Boolean.FromBool(IsEmpty()),
        "length" => new Integer(value.Length),
        _ => base.GetProperty(propertyName),
    };

    public override DataItem GetItem(DataItem index)
    {
        int n = index.AsInt32, l = value.Length;
        if (l <= 0 || n >= l) return null;
        while (n < 0) n += l;
        return new String(value[n].ToString());
    }

    public override void SetItem(DataItem index, DataItem value) =>
        throw new InvalidOperationException(Resources.StringsAreImmutable);

    public override DataItem GetItemRange(int lBound, int uBound)
    {
        AdjustBounds(value.Length, ref lBound, ref uBound);
        return new String(value[lBound..uBound]);
    }

    public override void SetItemRange(int lBound, int uBound, DataItem value) =>
        throw new InvalidOperationException(Resources.StringsAreImmutable);

    public override IEnumerable<(DataItem, DataItem)> GetEnumerable()
    {
        for (int i = 0; i < value.Length; ++i)
            yield return (new Integer(i), new String(value[i].ToString()));
    }
}
