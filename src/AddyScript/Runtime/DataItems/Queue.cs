using System.Collections.Generic;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Queue : DataItem
    {
        private readonly Queue<DataItem> queue;

        public Queue()
        {
            queue = new Queue<DataItem>();
        }

        public Queue(int capacity)
        {
            queue = new Queue<DataItem>(capacity);
        }

        public Queue(IEnumerable<DataItem> initialContent)
        {
            queue = new Queue<DataItem>(initialContent);
        }

        public override Class Class => Class.Queue;

        public override List<DataItem> AsList
        {
            get { return new List<DataItem>(queue); }
        }

        public override HashSet<DataItem> AsHashSet
        {
            get { return new HashSet<DataItem>(queue); }
        }

        public override Queue<DataItem> AsQueue => queue;

        public override Stack<DataItem> AsStack
        {
            get { return new Stack<DataItem>(queue); }
        }

        public override object AsNativeObject => queue;

        public override object Clone()
        {
            var content = queue.ToArray();

            for (int i = 0; i < content.Length; ++i)
                content[i] = (DataItem) content[i].Clone();

            return new Queue(content);
        }

        protected override bool UnsafeEquals(DataItem other)
        {
            return queue.Equals(other.AsQueue);
        }

        public override int GetHashCode()
        {
            return queue.GetHashCode();
        }

        public override bool IsEmpty()
        {
            return queue.Count <= 0;
        }

        public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
        {
            foreach (DataItem item in queue)
                yield return new KeyValuePair<DataItem, DataItem>(Void.Value, item);
        }
    }
}
