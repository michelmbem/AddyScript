using System.Collections.Generic;

using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Queue : Dynamic
    {
        private readonly Queue<Dynamic> queue;

        public Queue()
        {
            queue = new Queue<Dynamic>();
        }

        public Queue(int capacity)
        {
            queue = new Queue<Dynamic>(capacity);
        }

        public Queue(IEnumerable<Dynamic> initialContent)
        {
            queue = new Queue<Dynamic>(initialContent);
        }

        public override Class Class
        {
            get { return Class.Queue; }
        }

        public override List<Dynamic> AsList
        {
            get { return new List<Dynamic>(queue); }
        }

        public override HashSet<Dynamic> AsHashSet
        {
            get { return new HashSet<Dynamic>(queue); }
        }

        public override Queue<Dynamic> AsQueue
        {
            get { return queue; }
        }

        public override Stack<Dynamic> AsStack
        {
            get { return new Stack<Dynamic>(queue); }
        }

        public override object AsNativeObject
        {
            get { return queue; }
        }

        public override object Clone()
        {
            var content = queue.ToArray();

            for (int i = 0; i < content.Length; ++i)
                content[i] = (Dynamic) content[i].Clone();

            return new Queue(content);
        }

        protected override bool UnsafeEquals(Dynamic other)
        {
            return queue.Equals(other.AsQueue);
        }

        public override int GetHashCode()
        {
            return queue.GetHashCode();
        }

        public override IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetEnumerable()
        {
            foreach (Dynamic item in queue)
                yield return new KeyValuePair<Dynamic, Dynamic>(Void.Value, item);
        }
    }
}
