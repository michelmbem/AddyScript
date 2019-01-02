using System;
using System.Collections.Generic;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class List : Dynamic
    {
        private readonly List<Dynamic> list;

        public List()
        {
            list = new List<Dynamic>();
        }

        public List(int capacity)
        {
            list = new List<Dynamic>(capacity);
        }

        public List(IEnumerable<Dynamic> initialContent)
        {
            list = new List<Dynamic>(initialContent);
        }

        public override Class Class
        {
            get { return Class.List; }
        }

        public override List<Dynamic> AsList
        {
            get { return list; }
        }

        public override HashSet<Dynamic> AsHashSet
        {
            get { return new HashSet<Dynamic>(list); }
        }

        public override Queue<Dynamic> AsQueue
        {
            get { return new Queue<Dynamic>(list); }
        }

        public override Stack<Dynamic> AsStack
        {
            get { return new Stack<Dynamic>(list); }
        }

        public override object AsNativeObject
        {
            get { return list; }
        }

        public override object Clone()
        {
            var cloneList = list.ConvertAll(x => (Dynamic) x.Clone());
            return new List(cloneList);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder("[");
            bool trimEnd = false;

            foreach (Dynamic item in list)
            {
                sb.Append(item).Append(", ");
                trimEnd = true;
            }

            if (trimEnd)
                sb.Remove(sb.Length - 2, 2);

            return sb.Append("]").ToString();
        }

        protected override bool UnsafeEquals(Dynamic other)
        {
            var otherList = other.AsList;
            if (list.Count != otherList.Count) return false;

            for (int i = 0; i < list.Count; ++i)
                if (!list[i].Equals(otherList[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return list.GetHashCode();
        }

        protected override int UnsafeCompareTo(Dynamic other)
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

        public override bool ConversionNeeded(Class targetClass, BinaryOperator _operator)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                    switch (targetClass.ClassID)
                    {
                        case ClassID.Set:
                        case ClassID.Queue:
                        case ClassID.Stack:
                            return false;
                        default:
                            return base.ConversionNeeded(targetClass, _operator);
                    }
                default:
                    return base.ConversionNeeded(targetClass, _operator);
            }
        }

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

        public override Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                    {
                        var result = new List<Dynamic>(list);
                        result.AddRange(operand.AsList);
                        return new List(result);
                    }
                case BinaryOperator.Times:
                    {
                        var result = new List<Dynamic>();
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

        public override Dynamic GetItem(Dynamic index)
        {
            int n = index.AsInt32, l = list.Count;
            if (n >= l) return null;
            while (n < 0) n += l;
            return list[n];
        }

        public override void SetItem(Dynamic index, Dynamic value)
        {
            int n = index.AsInt32, l = list.Count;
            if (n >= l) throw new ArgumentOutOfRangeException();
            while (n < 0) n += l;
            list[n] = value;
        }

        public override IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetEnumerable()
        {
            for (int i = 0; i < list.Count; ++i)
            {
                var key = new Integer(i);
                yield return new KeyValuePair<Dynamic, Dynamic>(key, list[i]);
            }
        }
    }
}
