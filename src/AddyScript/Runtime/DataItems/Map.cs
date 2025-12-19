using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Map : DataItem
{
    private readonly Dictionary<DataItem, DataItem> dict;

    public Map() => dict = [];

    public Map(int capacity) => dict = new (capacity);

    public Map(Dictionary<DataItem, DataItem> initialContent) => dict = new (initialContent);

    public override Class Class => Class.Map;

    public override Dictionary<DataItem, DataItem> AsDictionary => dict;

    public override Dictionary<string, DataItem> AsDynamicObject
    {
        get
        {
            var obj = new Dictionary<string, DataItem>();

            foreach (var pair in dict)
                obj.Add(pair.Key.ToString(), pair.Value);

            return obj;
        }
    }

    public override object AsNativeObject => dict;

    public override object Clone()
    {
        var cloneDict = new Dictionary<DataItem, DataItem>();

        foreach (var pair in dict)
            cloneDict.Add((DataItem)pair.Key.Clone(), (DataItem)pair.Value.Clone());

        return new Map(cloneDict);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        var sb = new StringBuilder("{");

        if (dict.Count > 0)
        {
            bool trimEnd = false;

            foreach (var pair in dict)
            {
                sb.Append(pair.Key.ToString(format, formatProvider))
                  .Append(" => ")
                  .Append(pair.Value.ToString(format, formatProvider))
                  .Append(", ");

                trimEnd = true;
            }

            if (trimEnd) sb.Remove(sb.Length - 2, 2);
        }
        else
            sb.Append("=>");

        return sb.Append('}').ToString();
    }

    protected override bool UnsafeEquals(DataItem other)
    {
        var otherDict = other.AsDictionary;
        if (dict.Count != otherDict.Count) return false;

        foreach (var pair in dict)
            if (!(otherDict.TryGetValue(pair.Key, out DataItem otherValue) &&
                pair.Value.Equals(otherValue))) return false;

        return true;
    }

    public override int GetHashCode() => dict.GetHashCode();

    public override bool IsEmpty() => dict.Count == 0;

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
    {
        switch (_operator)
        {
            case BinaryOperator.Plus:
                {
                    var result = new Dictionary<DataItem, DataItem>(dict);

                    foreach (var pair in operand.AsDictionary)
                        result.Add(pair.Key, pair.Value);

                    return new Map(result);
                }
            case BinaryOperator.Contains:
                return Boolean.FromBool(dict.ContainsKey(operand));
            default:
                return base.BinaryOperation(_operator, operand);
        }
    }

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "empty" => Boolean.FromBool(IsEmpty()),
        "size" => new Integer(dict.Count),
        "keys" => new Set(dict.Keys),
        "values" => new Set(dict.Values),
        "entries" => new Set(dict.Select(pair => new Tuple([pair.Key, pair.Value]))),
        _ => base.GetProperty(propertyName),
    };

    public override DataItem GetItem(DataItem index)
    {
        dict.TryGetValue(index, out DataItem value);
        return value ?? Void.Value;
    }

    public override void SetItem(DataItem index, DataItem value) => dict[index] = value;

    public override IEnumerable<(DataItem, DataItem)> GetEnumerable()
    {
        foreach (var entry in dict)
            yield return (entry.Key, entry.Value);
    }
}
