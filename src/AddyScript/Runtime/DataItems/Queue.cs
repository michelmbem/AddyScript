using System.Collections.Generic;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Queue : DataItem
{
    private readonly Queue<DataItem> queue;

    public Queue() => queue = new();

    public Queue(int capacity) => queue = new(capacity);

    public Queue(IEnumerable<DataItem> initialContent) => queue = new(initialContent);

    public override Class Class => Class.Queue;

    public override List<DataItem> AsList => new(queue);

    public override HashSet<DataItem> AsHashSet => new(queue);

    public override Queue<DataItem> AsQueue => queue;

    public override Stack<DataItem> AsStack => new(queue);

    public override object AsNativeObject => queue;

    public override object Clone()
    {
        var content = queue.ToArray();

        for (int i = 0; i < content.Length; ++i)
            content[i] = (DataItem) content[i].Clone();

        return new Queue(content);
    }

    protected override bool UnsafeEquals(DataItem other) => queue.Equals(other.AsQueue);

    public override int GetHashCode() => queue.GetHashCode();

    public override bool IsEmpty() => queue.Count <= 0;

    public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
    {
        foreach (DataItem item in queue)
            yield return new KeyValuePair<DataItem, DataItem>(Void.Value, item);
    }
}
