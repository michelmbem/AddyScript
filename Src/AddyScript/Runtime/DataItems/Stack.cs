using System.Collections.Generic;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Stack : DataItem
    {
        private readonly Stack<DataItem> stack;

        public Stack()
        {
            stack = new Stack<DataItem>();
        }

        public Stack(int capacity)
        {
            stack = new Stack<DataItem>(capacity);
        }

        public Stack(IEnumerable<DataItem> initialContent)
        {
            stack = new Stack<DataItem>(initialContent);
        }

        public override Class Class => Class.Stack;

        public override List<DataItem> AsList
        {
            get { return new List<DataItem>(stack); }
        }

        public override HashSet<DataItem> AsHashSet
        {
            get { return new HashSet<DataItem>(stack); }
        }

        public override Queue<DataItem> AsQueue
        {
            get { return new Queue<DataItem>(stack); }
        }

        public override Stack<DataItem> AsStack => stack;

        public override object AsNativeObject => stack;

        public override object Clone()
        {
            var content = stack.ToArray();

            for (int i = 0; i < content.Length; ++i)
                content[i] = (DataItem)content[i].Clone();

            return new Stack(content);
        }

        protected override bool UnsafeEquals(DataItem other)
        {
            return stack.Equals(other.AsStack);
        }

        public override int GetHashCode()
        {
            return stack.GetHashCode();
        }

        public override bool IsEmpty()
        {
            return stack.Count <= 0;
        }

        public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
        {
            foreach (DataItem item in stack)
                yield return new KeyValuePair<DataItem, DataItem>(Void.Value, item);
        }
    }
}
