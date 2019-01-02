using System;
using System.Collections.Generic;
using System.Text;

using AddyScript.Ast.Expressions;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Map : Dynamic
    {
        private readonly Dictionary<Dynamic, Dynamic> dict;

        public Map()
        {
            dict = new Dictionary<Dynamic, Dynamic>();
        }

        public Map(int capacity)
        {
            dict = new Dictionary<Dynamic, Dynamic>(capacity);
        }

        public Map(Dictionary<Dynamic, Dynamic> initialContent)
        {
            dict = new Dictionary<Dynamic, Dynamic>(initialContent);
        }

        public override Class Class
        {
            get { return Class.Map; }
        }

        public override Dictionary<Dynamic, Dynamic> AsDictionary
        {
            get { return dict; }
        }

        public override Dictionary<string, Dynamic> AsDynamicObject
        {
            get
            {
                var obj = new Dictionary<string, Dynamic>();

                foreach (KeyValuePair<Dynamic, Dynamic> pair in dict)
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
            var cloneDict = new Dictionary<Dynamic, Dynamic>();

            foreach (KeyValuePair<Dynamic, Dynamic> pair in dict)
                cloneDict.Add((Dynamic) pair.Key.Clone(), (Dynamic) pair.Key.Clone());

            return new Map(cloneDict);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder("{");

            if (dict.Count > 0)
            {
                bool trimEnd = false;

                foreach (KeyValuePair<Dynamic, Dynamic> pair in dict)
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

        protected override bool UnsafeEquals(Dynamic other)
        {
            var otherDict = other.AsDictionary;
            if (dict.Count != otherDict.Count) return false;

            foreach (Dynamic key in dict.Keys)
                if (!(otherDict.ContainsKey(key) &&
                    dict[key].Equals(otherDict[key])))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return dict.GetHashCode();
        }

        public override Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                    {
                        var result = new Dictionary<Dynamic, Dynamic>(dict);
                        foreach (var pair in operand.AsDictionary)
                            result.Add(pair.Key, pair.Value);
                        return new Map(result);
                    }
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }

        public override Dynamic GetItem(Dynamic index)
        {
            Dynamic value;
            dict.TryGetValue(index, out value);
            return value ?? Void.Value;
        }

        public override void SetItem(Dynamic index, Dynamic value)
        {
            dict[index] = value;
        }

        public override IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetEnumerable()
        {
            foreach (var entry in dict)
                yield return entry;
        }
    }
}
