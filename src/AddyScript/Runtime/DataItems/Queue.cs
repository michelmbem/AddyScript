﻿using System.Collections.Generic;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Queue : DataItem
{
    private readonly Queue<DataItem> queue;

    public Queue() => queue = new();

    public Queue(int capacity) => queue = new(capacity);

    public Queue(IEnumerable<DataItem> initialContent) => queue = new(initialContent);

    public override Class Class => Class.Queue;

    public override DataItem[] AsArray => [.. queue];

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

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "empty" => Boolean.FromBool(IsEmpty()),
        "size" => new Integer(queue.Count),
        "front" => queue.Peek(),
        _ => base.GetProperty(propertyName),
    };

    public override IEnumerable<(DataItem, DataItem)> GetEnumerable()
    {
        foreach (DataItem item in queue)
            yield return (Void.Value, item);
    }
}
