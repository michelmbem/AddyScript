using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class List : DataItem
{
    private readonly List<DataItem> list;

    public List() => list = [];

    public List(int capacity) => list = new (capacity);

    public List(IEnumerable<DataItem> initialContent) => list = new (initialContent);

    public override Class Class => Class.List;

    public override DataItem[] AsArray => [..list];

    public override List<DataItem> AsList => list;

    public override HashSet<DataItem> AsHashSet => [..list];

    public override Queue<DataItem> AsQueue => new (list);

    public override Stack<DataItem> AsStack => new (list);

    public override object AsNativeObject => list;

    public override object Clone()
    {
        var cloneList = list.ConvertAll(x => (DataItem)x.Clone());
        return new List(cloneList);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        var sb = new StringBuilder("[");
        bool trimEnd = false;

        foreach (DataItem item in list)
        {
            sb.Append(item.ToString(format, formatProvider))
              .Append(", ");

            trimEnd = true;
        }

        if (trimEnd) sb.Remove(sb.Length - 2, 2);

        return sb.Append(']').ToString();
    }

    protected override bool UnsafeEquals(DataItem other)
    {
        var otherList = other.AsList;
        return list.Count == otherList.Count &&
               !list.Where((item, index) => !item.Equals(otherList[index]))
                    .Any();
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        foreach (DataItem item in list)
            hashCode.Add(item);

        return hashCode.ToHashCode();
    }

    protected override int UnsafeCompareTo(DataItem other)
    {
        var otherList = other.AsList;
        int minCount = Math.Min(list.Count, otherList.Count);

        for (int i = 0; i < minCount; ++i)
        {
            int cmp = list[i].CompareTo(otherList[i]);
            if (cmp != 0) return cmp;
        }

        return Math.Sign(list.Count - otherList.Count);
    }

    public override bool IsEmpty() => list.Count == 0;

    public override bool ConversionNeeded(Class targetClass, BinaryOperator _operator) => _operator switch
    {
        BinaryOperator.Plus => targetClass.ClassID switch
        {
            ClassID.Tuple or ClassID.Set or ClassID.Queue or ClassID.Stack => false,
            _ => base.ConversionNeeded(targetClass, _operator),
        },
        _ => base.ConversionNeeded(targetClass, _operator),
    };

    public override object ConvertTo(Type targetType)
    {
        if (targetType.IsArray)
        {
            Type elementType = targetType.GetElementType();
            Array array = Array.CreateInstance(elementType, list.Count);

            for (int i = 0; i < list.Count; ++i)
                array.SetValue(list[i].ConvertTo(elementType), i);

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
                var result = new List<DataItem>(list);
                result.AddRange(operand.AsList);
                return new List(result);
            }
            case BinaryOperator.Times:
            {
                var result = new List<DataItem>();
                int n = operand.AsInt32;
                for (int i = 0; i < n; ++i) result.AddRange(list);
                return new List(result);
            }
            case BinaryOperator.Contains:
                return Boolean.FromBool(list.Contains(operand));
            default:
                return base.BinaryOperation(_operator, operand);
        }
    }

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "empty" => Boolean.FromBool(IsEmpty()),
        "size" => new Integer(list.Count),
        "front" => list.FirstOrDefault(Void.Value),
        "back" => list.LastOrDefault(Void.Value),
        _ => base.GetProperty(propertyName),
    };

    public override DataItem GetItem(DataItem index)
    {
        int n = index.AsInt32, l = list.Count;
        if (l <= 0 || n >= l) return null;
        while (n < 0) n += l;
        return list[n];
    }

    public override void SetItem(DataItem index, DataItem value)
    {
        int n = index.AsInt32, l = list.Count;
        if (l <= 0 || n >= l) throw new ArgumentOutOfRangeException();
        while (n < 0) n += l;
        list[n] = value;
    }

    public override DataItem GetItemRange(int lBound, int uBound)
    {
        AdjustBounds(list.Count, ref lBound, ref uBound);
        return new List(list[lBound..uBound]);
    }

    public override void SetItemRange(int lBound, int uBound, DataItem value)
    {
        AdjustBounds(list.Count, ref lBound, ref uBound);
        list.RemoveRange(lBound, uBound - lBound);
        list.InsertRange(lBound, value.AsList);
    }

    public override IEnumerable<(DataItem, DataItem)> GetEnumerable()
    {
        int i = 0;
        foreach (DataItem item in list)
            yield return (new Integer(i++), item);
    }
}
