using System;
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

    public Map(int capacity) => dict = new Dictionary<DataItem, DataItem>(capacity);

    public Map(Dictionary<DataItem, DataItem> initialContent) =>
        dict = new Dictionary<DataItem, DataItem>(initialContent);

    public override Class Class => Class.Map;

    public override Dictionary<DataItem, DataItem> AsDictionary => dict;

    public override Dictionary<string, DataItem> AsDynamicObject =>
        dict.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value);

    public override object AsNativeObject => dict;

    public override object Clone()
    {
        var cloneDict = dict.ToDictionary(
            pair => (DataItem)pair.Key.Clone(),
            pair => (DataItem)pair.Value.Clone());

        return new Map(cloneDict);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        StringBuilder sb = new ("{");

        if (dict.Count > 0)
        {
            bool stripEnd = false;

            foreach (var pair in dict)
            {
                sb.Append(pair.Key.ToString(format, formatProvider))
                  .Append(" => ")
                  .Append(pair.Value.ToString(format, formatProvider))
                  .Append(", ");

                stripEnd = true;
            }

            if (stripEnd) sb.Length -= 2;
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

    public override IEnumerable<(DataItem, DataItem)> GetEnumerable() =>
        dict.Select(entry => (entry.Key, entry.Value));
}
