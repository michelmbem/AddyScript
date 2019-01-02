using System.Collections.Generic;

using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Stack : Dynamic
    {
        private readonly Stack<Dynamic> stack;

        public Stack()
        {
            stack = new Stack<Dynamic>();
        }

        public Stack(int capacity)
        {
            stack = new Stack<Dynamic>(capacity);
        }

        public Stack(IEnumerable<Dynamic> initialContent)
        {
            stack = new Stack<Dynamic>(initialContent);
        }

        public override Class Class
        {
            get { return Class.Stack; }
        }

        public override List<Dynamic> AsList
        {
            get { return new List<Dynamic>(stack); }
        }

        public override HashSet<Dynamic> AsHashSet
        {
            get { return new HashSet<Dynamic>(stack); }
        }

        public override Queue<Dynamic> AsQueue
        {
            get { return new Queue<Dynamic>(stack); }
        }

        public override Stack<Dynamic> AsStack
        {
            get { return stack; }
        }

        public override object AsNativeObject
        {
            get { return stack; }
        }

        public override object Clone()
        {
            var content = stack.ToArray();

            for (int i = 0; i < content.Length; ++i)
                content[i] = (Dynamic) content[i].Clone();

            return new Stack(content);
        }

        protected override bool UnsafeEquals(Dynamic other)
        {
            return stack.Equals(other.AsStack);
        }

        public override int GetHashCode()
        {
            return stack.GetHashCode();
        }

        public override IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetEnumerable()
        {
            foreach (Dynamic item in stack)
                yield return new KeyValuePair<Dynamic, Dynamic>(Void.Value, item);
        }
    }
}
