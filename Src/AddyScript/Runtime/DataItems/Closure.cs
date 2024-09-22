using System;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Closure(Function function) : DataItem
    {
        public override Class Class => Class.Closure;

        public override object AsNativeObject => function;

        public override Function AsFunction => function;

        protected override bool UnsafeEquals(DataItem other)
        {
            return function.Equals(other.AsFunction);
        }

        public override int GetHashCode()
        {
            return function.GetHashCode();
        }

        public override object ConvertTo(Type targetType)
        {
            return targetType.IsSubclassOf(typeof(Delegate))
                 ? function.ToDelegate(targetType)
                 : base.ConvertTo(targetType);
        }
    }
}
