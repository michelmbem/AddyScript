using System;
using System.Collections.Generic;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class List : DataItem
{
    private readonly List<DataItem> list;

    public List() => list = [];

    public List(int capacity) => list = new(capacity);

    public List(IEnumerable<DataItem> initialContent) => list = new(initialContent);

    public override Class Class => Class.List;

    public override List<DataItem> AsList => list;

    public override HashSet<DataItem> AsHashSet => new(list);

    public override Queue<DataItem> AsQueue => new(list);

    public override Stack<DataItem> AsStack => new(list);

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
        if (list.Count != otherList.Count) return false;

        for (int i = 0; i < list.Count; ++i)
            if (!list[i].Equals(otherList[i]))
                return false;

        return true;
    }

    public override int GetHashCode() => list.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other)
    {
        var otherList = other.AsList;
        int l = Math.Min(list.Count, otherList.Count);

        for (int i = 0; i < l; ++i)
        {
            int cmp = list[i].CompareTo(otherList[i]);
            if (cmp != 0) return cmp;
        }

        if (list.Count < otherList.Count) return -1;
        if (list.Count > otherList.Count) return +1;
        return 0;
    }

    public override bool IsEmpty() => list.Count <= 0;

    public override bool ConversionNeeded(Class targetClass, BinaryOperator _operator) => _operator switch
    {
        BinaryOperator.Plus => targetClass.ClassID switch
        {
            ClassID.Set or ClassID.Queue or ClassID.Stack => false,
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

    public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
    {
        for (int i = 0; i < list.Count; ++i)
        {
            var key = new Integer(i);
            yield return new KeyValuePair<DataItem, DataItem>(key, list[i]);
        }
    }
}
