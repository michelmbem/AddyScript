using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Properties;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Tuple(DataItem[] items) : DataItem
{
    public override Class Class => Class.Tuple;

    public override DataItem[] AsArray => items;

    public override List<DataItem> AsList => [..items];

    public override HashSet<DataItem> AsHashSet => [..items];

    public override Queue<DataItem> AsQueue => new (items);

    public override Stack<DataItem> AsStack => new (items);

    public override object AsNativeObject => items;

    public override object Clone()
    {
        var cloneItems = new DataItem[items.Length];
        Array.Copy(items, cloneItems, items.Length);
        return new Tuple(cloneItems);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        StringBuilder sb = new ("(");
        bool stripEnd = false;

        foreach (DataItem item in items)
        {
            sb.Append(item.ToString(format, formatProvider))
              .Append(", ");

            stripEnd = true;
        }

        if (stripEnd) sb.Length -= 2;
        sb.Append(')');

        return sb.ToString();
    }

    protected override bool UnsafeEquals(DataItem other)
    {
        var otherItems = other.AsArray;
        return items.Length == otherItems.Length &&
               !items.Where((item, index) => !item.Equals(otherItems[index]))
                     .Any();
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        foreach (DataItem item in items)
            hashCode.Add(item);

        return hashCode.ToHashCode();
    }

    protected override int UnsafeCompareTo(DataItem other)
    {
        DataItem[] otherItems = other.AsArray;
        int minLength = Math.Min(items.Length, otherItems.Length);

        for (int i = 0; i < minLength; ++i)
        {
            int cmp = items[i].CompareTo(otherItems[i]);
            if (cmp != 0) return cmp;
        }

        return Math.Sign(items.Length - otherItems.Length);
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

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "size" => new Integer(items.Length),
        "front" => items.FirstOrDefault(Void.Value),
        "back" => items.LastOrDefault(Void.Value),
        _ => base.GetProperty(propertyName),
    };

    public override DataItem GetItem(DataItem index)
    {
        int n = index.AsInt32, l = items.Length;
        if (l <= 0 || n >= l) return null;
        while (n < 0) n += l;
        return items[n];
    }

    public override void SetItem(DataItem index, DataItem value)
        => throw new InvalidOperationException(Resources.TuplesAreImmutable);

    public override DataItem GetItemRange(int lBound, int uBound)
    {
        AdjustBounds(items.Length, ref lBound, ref uBound);
        return new Tuple(items[lBound..uBound]);
    }

    public override void SetItemRange(int lBound, int uBound, DataItem value) =>
        throw new InvalidOperationException(Resources.TuplesAreImmutable);

    public override IEnumerable<(DataItem, DataItem)> GetEnumerable()
    {
        int i = 0;
        foreach (DataItem item in items)
            yield return (new Integer(i++), item);
    }
}
