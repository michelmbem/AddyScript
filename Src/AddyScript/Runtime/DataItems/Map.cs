using System;
using System.Collections.Generic;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Map : DataItem
    {
        private readonly Dictionary<DataItem, DataItem> dict;

        public Map()
        {
            dict = [];
        }

        public Map(int capacity)
        {
            dict = new Dictionary<DataItem, DataItem>(capacity);
        }

        public Map(Dictionary<DataItem, DataItem> initialContent)
        {
            dict = new Dictionary<DataItem, DataItem>(initialContent);
        }

        public override Class Class
        {
            get { return Class.Map; }
        }

        public override Dictionary<DataItem, DataItem> AsDictionary
        {
            get { return dict; }
        }

        public override Dictionary<string, DataItem> AsDynamicObject
        {
            get
            {
                var obj = new Dictionary<string, DataItem>();

                foreach (KeyValuePair<DataItem, DataItem> pair in dict)
                    obj.Add(pair.Key.ToString(), pair.Value);

                return obj;
            }
        }

        public override object AsNativeObject
        {
            get { return dict; }
        }

        public override object Clone()
        {
            var cloneDict = new Dictionary<DataItem, DataItem>();

            foreach (KeyValuePair<DataItem, DataItem> pair in dict)
                cloneDict.Add((DataItem) pair.Key.Clone(), (DataItem) pair.Key.Clone());

            return new Map(cloneDict);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder("{");

            if (dict.Count > 0)
            {
                bool trimEnd = false;

                foreach (KeyValuePair<DataItem, DataItem> pair in dict)
                {
                    sb.Append(pair.Key).Append(" => ").Append(pair.Value).Append(", ");
                    trimEnd = true;
                }

                if (trimEnd)
                    sb.Remove(sb.Length - 2, 2);
            }
            else
                sb.Append("=>");

            return sb.Append("}").ToString();
        }

        protected override bool UnsafeEquals(DataItem other)
        {
            var otherDict = other.AsDictionary;
            if (dict.Count != otherDict.Count) return false;

            foreach (DataItem key in dict.Keys)
                if (!(otherDict.ContainsKey(key) &&
                    dict[key].Equals(otherDict[key])))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return dict.GetHashCode();
        }

        public override bool IsEmpty()
        {
            return dict.Count <= 0;
        }

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
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }

        public override DataItem GetItem(DataItem index)
        {
            dict.TryGetValue(index, out DataItem value);
            return value ?? Void.Value;
        }

        public override void SetItem(DataItem index, DataItem value)
        {
            dict[index] = value;
        }

        public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
        {
            foreach (var entry in dict)
                yield return entry;
        }
    }
}
