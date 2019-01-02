using System;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Closure : Dynamic
    {
        private readonly Function function;
        
        public Closure(Function function)
        {
            this.function = function;
        }

        public override Class Class
        {
            get { return Class.Closure; }
        }

        public override object  AsNativeObject
        {
            get { return function; }
        }

        public override Function AsFunction
        {
            get { return function; }
        }

        protected override bool UnsafeEquals(Dynamic other)
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
