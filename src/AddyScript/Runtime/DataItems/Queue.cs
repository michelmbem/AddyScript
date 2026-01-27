using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Queue : DataItem
{
    private readonly Queue<DataItem> queue;

    public Queue() => queue = new ();

    public Queue(int capacity) => queue = new (capacity);

    public Queue(IEnumerable<DataItem> initialContent) => queue = new (initialContent);

    public override Class Class => Class.Queue;

    public override DataItem[] AsArray => [..queue];

    public override List<DataItem> AsList => [..queue];

    public override HashSet<DataItem> AsHashSet => [..queue];

    public override Queue<DataItem> AsQueue => queue;

    public override Stack<DataItem> AsStack => new (queue);

    public override object AsNativeObject => queue;

    public override object Clone()
    {
        var content = queue.ToArray();

        for (int i = 0; i < content.Length; ++i)
            content[i] = (DataItem) content[i].Clone();

        return new Queue(content);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        StringBuilder sb = new ();
        sb.Append($"<{Class.Name} {{size = {queue.Count}");

        if (queue.Count > 0)
        {
            sb.Append($", front = {queue.Peek().ToString(format, formatProvider)}");
        }

        return sb.Append("}>").ToString();
    }

    protected override bool UnsafeEquals(DataItem other) => queue.Equals(other.AsQueue);

    public override int GetHashCode() => queue.GetHashCode();

    public override bool IsEmpty() => queue.Count == 0;

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "empty" => Boolean.FromBool(IsEmpty()),
        "size" => new Integer(queue.Count),
        "front" => queue.Peek(),
        _ => base.GetProperty(propertyName),
    };

    public override IEnumerable<(DataItem, DataItem)> GetEnumerable() =>
        queue.Select(item => ((DataItem)Void.Value, item));
}
