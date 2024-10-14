using System.Collections;
using System.Collections.Generic;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Stack : DataItem
{
    private readonly Stack<DataItem> stack;

    public Stack() => stack = new();

    public Stack(int capacity) => stack = new(capacity);

    public Stack(IEnumerable<DataItem> initialContent) => stack = new(initialContent);

    public override Class Class => Class.Stack;

    public override DataItem[] AsArray => [.. stack];

    public override List<DataItem> AsList => new(stack);

    public override HashSet<DataItem> AsHashSet => new(stack);

    public override Queue<DataItem> AsQueue => new(stack);

    public override Stack<DataItem> AsStack => stack;

    public override object AsNativeObject => stack;

    public override object Clone()
    {
        var content = stack.ToArray();

        for (int i = 0; i < content.Length; ++i)
            content[i] = (DataItem)content[i].Clone();

        return new Stack(content);
    }

    protected override bool UnsafeEquals(DataItem other) => stack.Equals(other.AsStack);

    public override int GetHashCode() => stack.GetHashCode();

    public override bool IsEmpty() => stack.Count <= 0;

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "empty" => Boolean.FromBool(IsEmpty()),
        "size" => new Integer(stack.Count),
        "top" => stack.Peek(),
        _ => base.GetProperty(propertyName),
    };

    public override IEnumerable<(DataItem, DataItem)> GetEnumerable()
    {
        foreach (DataItem item in stack)
            yield return (Void.Value, item);
    }
}
