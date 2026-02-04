using System;
using System.Collections.Generic;


namespace AddyScript.Runtime.OOP;


/// <summary>
/// Represents a collection of <see cref="ClassMember"/>s that can be accessed either by index or by name.
/// </summary>
/// <typeparam name="T">Any subclass of <see cref="ClassMember"/></typeparam>
public class ClassMemberSet<T> : List<T> where T : ClassMember
{
    private readonly Dictionary<string, T> dictionary = [];

    public ClassMemberSet()
    {
    }

    public ClassMemberSet(int capacity) : base(capacity)
    {
    }

    public ClassMemberSet(IEnumerable<T> items)
    {
        AddRange(items);
    }

    public T this[string name] => dictionary[name];

    public bool TryGetValue(string name, out T item) => dictionary.TryGetValue(name, out item);

    public new void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (dictionary.ContainsKey(item.Name))
            throw new ArgumentException($"Item '{item.Name}' already exists in the collection");

        base.Add(item);
        dictionary.Add(item.Name, item);
    }

    public new void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;
        foreach (var item in items)
            Add(item);
    }

    public bool Contains(string name) => dictionary.ContainsKey(name);

    public int IndexOf(string name) =>
        dictionary.TryGetValue(name, out var item) ? base.IndexOf(item) : -1;

    public new bool Remove(T item) => base.Remove(item) && dictionary.Remove(item.Name);

    public bool Remove(string name) =>
        dictionary.TryGetValue(name, out var item) && Remove(item) && dictionary.Remove(name);

    public new void RemoveRange(int index, int count)
    {
        for (var i = index; i < index + count; i++)
            dictionary.Remove(this[i].Name);

        base.RemoveRange(index, count);
    }

    public new void Clear()
    {
        base.Clear();
        dictionary.Clear();
    }
}
