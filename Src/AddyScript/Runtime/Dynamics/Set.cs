using System;
using System.Collections.Generic;
using System.Text;

using AddyScript.Ast.Expressions;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Set : Dynamic
    {
        private readonly HashSet<Dynamic> hashSet;

        public Set()
        {
            hashSet = new HashSet<Dynamic>();
        }

        public Set(IEnumerable<Dynamic> initialContent)
        {
            hashSet = new HashSet<Dynamic>(initialContent);
        }

        public override Class Class
        {
            get { return Class.Set; }
        }

        public override List<Dynamic> AsList
        {
            get { return new List<Dynamic>(hashSet); }
        }

        public override HashSet<Dynamic> AsHashSet
        {
            get { return hashSet; }
        }

        public override Queue<Dynamic> AsQueue
        {
            get { return new Queue<Dynamic>(hashSet); }
        }

        public override Stack<Dynamic> AsStack
        {
            get { return new Stack<Dynamic>(hashSet); }
        }

        public override object AsNativeObject
        {
            get { return hashSet; }
        }

        public override object Clone()
        {
            var cloneSet = new HashSet<Dynamic>();
            
            foreach (Dynamic item in hashSet)
                cloneSet.Add((Dynamic) item.Clone());

            return new Set(cloneSet);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder("{");
            bool trimEnd = false;

            foreach (Dynamic element in hashSet)
            {
                sb.Append(element).Append(", ");
                trimEnd = true;
            }

            if (trimEnd)
                sb.Remove(sb.Length - 2, 2);

            return sb.Append("}").ToString();
        }

        protected override bool UnsafeEquals(Dynamic other)
        {
            return hashSet.Equals(other.AsHashSet);
        }

        public override int GetHashCode()
        {
            return hashSet.GetHashCode();
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

        public override Dynamic BinaryOperation(BinaryOperator _operator, Dynamic operand)
        {
            switch (_operator)
            {
                case BinaryOperator.Plus:
                case BinaryOperator.Or:
                {
                    var result = new HashSet<Dynamic>(hashSet);
                    result.UnionWith(operand.AsHashSet);
                    return new Set(result);
                }
                case BinaryOperator.Minus:
                {
                    var result = new HashSet<Dynamic>(hashSet);
                    result.ExceptWith(operand.AsHashSet);
                    return new Set(result);
                }
                case BinaryOperator.And:
                {
                    var result = new HashSet<Dynamic>(hashSet);
                    result.IntersectWith(operand.AsHashSet);
                    return new Set(result);
                }
                case BinaryOperator.ExclusiveOr:
                {
                    var result = new HashSet<Dynamic>(hashSet);
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

        public override IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetEnumerable()
        {
            foreach (Dynamic element in hashSet)
                yield return new KeyValuePair<Dynamic, Dynamic>(element, element);
        }
    }
}
