using System;
using System.Collections.Generic;
using System.Text;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Set : DataItem
    {
        private readonly HashSet<DataItem> hashSet;

        public Set()
        {
            hashSet = [];
        }

        public Set(IEnumerable<DataItem> initialContent)
        {
            hashSet = new HashSet<DataItem>(initialContent);
        }

        public override Class Class
        {
            get { return Class.Set; }
        }

        public override List<DataItem> AsList
        {
            get { return new List<DataItem>(hashSet); }
        }

        public override HashSet<DataItem> AsHashSet
        {
            get { return hashSet; }
        }

        public override Queue<DataItem> AsQueue
        {
            get { return new Queue<DataItem>(hashSet); }
        }

        public override Stack<DataItem> AsStack
        {
            get { return new Stack<DataItem>(hashSet); }
        }

        public override object AsNativeObject
        {
            get { return hashSet; }
        }

        public override object Clone()
        {
            var cloneSet = new HashSet<DataItem>();
            
            foreach (DataItem item in hashSet)
                cloneSet.Add((DataItem) item.Clone());

            return new Set(cloneSet);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder("{");
            bool trimEnd = false;

            foreach (DataItem element in hashSet)
            {
                sb.Append(element).Append(", ");
                trimEnd = true;
            }

            if (trimEnd)
                sb.Remove(sb.Length - 2, 2);

            return sb.Append("}").ToString();
        }

        protected override bool UnsafeEquals(DataItem other)
        {
            return hashSet.Equals(other.AsHashSet);
        }

        public override int GetHashCode()
        {
            return hashSet.GetHashCode();
        }

        public override bool IsEmpty()
        {
            return hashSet.Count <= 0;
        }

        public override bool ConversionNeeded(Class targetClass, BinaryOperator _operator)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                    switch (targetClass.ClassID)
                    {
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

        public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                case BinaryOperator.Or:
                {
                    var result = new HashSet<DataItem>(hashSet);
                    result.UnionWith(operand.AsHashSet);
                    return new Set(result);
                }
                case BinaryOperator.Minus:
                {
                    var result = new HashSet<DataItem>(hashSet);
                    result.ExceptWith(operand.AsHashSet);
                    return new Set(result);
                }
                case BinaryOperator.And:
                {
                    var result = new HashSet<DataItem>(hashSet);
                    result.IntersectWith(operand.AsHashSet);
                    return new Set(result);
                }
                case BinaryOperator.ExclusiveOr:
                {
                    var result = new HashSet<DataItem>(hashSet);
                    result.SymmetricExceptWith(operand.AsHashSet);
                    return new Set(result);
                }
                case BinaryOperator.LessThan:
                    return Boolean.FromBool(hashSet.IsProperSubsetOf(operand.AsHashSet));
                case BinaryOperator.LessThanOrEqual:
                    return Boolean.FromBool(hashSet.IsSubsetOf(operand.AsHashSet));
                case BinaryOperator.GreaterThan:
                    return Boolean.FromBool(hashSet.IsProperSupersetOf(operand.AsHashSet));
                case BinaryOperator.GreaterThanOrEqual:
                    return Boolean.FromBool(hashSet.IsSupersetOf(operand.AsHashSet));
                case BinaryOperator.Contains:
                    return Boolean.FromBool(hashSet.Contains(operand));
                default:
                    return base.BinaryOperation(_operator, operand);
            }
        }

        public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
        {
            foreach (DataItem element in hashSet)
                yield return new KeyValuePair<DataItem, DataItem>(element, element);
        }
    }
}
