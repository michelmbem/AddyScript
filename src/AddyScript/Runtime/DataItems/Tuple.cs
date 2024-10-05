using System;
using System.Collections.Generic;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Properties;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Tuple(DataItem[] items) : DataItem
{
    public override Class Class => Class.Tuple;

    public override DataItem[] AsArray => items;

    public override List<DataItem> AsList => new(items);

    public override HashSet<DataItem> AsHashSet => new(items);

    public override Queue<DataItem> AsQueue => new(items);

    public override Stack<DataItem> AsStack => new(items);

    public override object AsNativeObject => items;

    public override object Clone()
    {
        var cloneItems = new DataItem[items.Length];
        Array.Copy(items, cloneItems, items.Length);
        return new Tuple(cloneItems);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        var sb = new StringBuilder("(");
        bool trimEnd = false;

        foreach (DataItem item in items)
        {
            sb.Append(item.ToString(format, formatProvider))
              .Append(", ");

            trimEnd = true;
        }

        if (trimEnd) sb.Remove(sb.Length - 2, 2);

        return sb.Append(')').ToString();
    }

    protected override bool UnsafeEquals(DataItem other)
    {
        var otherItems = other.AsArray;
        if (items.Length != otherItems.Length) return false;

        for (int i = 0; i < items.Length; ++i)
            if (!items[i].Equals(otherItems[i]))
                return false;

        return true;
    }

    public override int GetHashCode() => items.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other)
    {
        var otherItems = other.AsArray;
        int l = Math.Min(items.Length, otherItems.Length);

        for (int i = 0; i < l; ++i)
        {
            int cmp = items[i].CompareTo(otherItems[i]);
            if (cmp != 0) return cmp;
        }

        if (items.Length < otherItems.Length) return -1;
        if (items.Length > otherItems.Length) return +1;
        return 0;
    }

    public override bool ConversionNeeded(Class targetClass, BinaryOperator _operator) => _operator switch
    {
        BinaryOperator.Plus => targetClass.ClassID switch
        {
            ClassID.List or ClassID.Set or ClassID.Queue or ClassID.Stack => false,
            _ => base.ConversionNeeded(targetClass, _operator),
        },
        _ => base.ConversionNeeded(targetClass, _operator),
    };

    public override object ConvertTo(Type targetType)
    {
        if (targetType.IsArray)
        {
            Type elementType = targetType.GetElementType();
            Array array = Array.CreateInstance(elementType, items.Length);

            for (int i = 0; i < items.Length; ++i)
                array.SetValue(items[i].ConvertTo(elementType), i);

            return array;
        }

        return base.ConvertTo(targetType);
    }

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
    {
        switch (_operator)
        {
            case BinaryOperator.Plus:
                {
                    var operandItems = operand.AsArray;
                    var result = new DataItem[items.Length + operandItems.Length];
                    Array.Copy(items, result, items.Length);
                    Array.Copy(operandItems, 0, result, items.Length, operandItems.Length);
                    return new Tuple(result);
                }
            case BinaryOperator.Times:
                {
                    var result = new List<DataItem>();
                    int n = operand.AsInt32;
                    for (int i = 0; i < n; ++i) result.AddRange(items);
                    return new Tuple([.. result]);
                }
            case BinaryOperator.Contains:
                return Boolean.FromBool(Array.IndexOf(items, operand) >= 0);
            default:
                return base.BinaryOperation(_operator, operand);
        }
    }

    public override DataItem GetItem(DataItem index)
    {
        int n = index.AsInt32, l = items.Length;
        if (l <= 0 || n >= l) return null;
        while (n < 0) n += l;
        return items[n];
    }

    public override void SetItem(DataItem index, DataItem value)
        => throw new InvalidOperationException(Resources.TuplesAreImmutable);

    public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
    {
        for (int i = 0; i < items.Length; ++i)
        {
            var key = new Integer(i);
            yield return new KeyValuePair<DataItem, DataItem>(key, items[i]);
        }
    }
}