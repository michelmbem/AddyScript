using System;
using System.Collections.Generic;


namespace AddyScript.Runtime.OOP;


/// <summary>
/// Represents a collection of <see cref="ClassMember"/>s that can be accessed either by index or by name.
/// </summary>
/// <typeparam name="T">Any subclass of <see cref="ClassMember"/></typeparam>
public class ClassMemberSet<T> : List<T>
    where T : ClassMember
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

    public int IndexOf(string name) => IndexOf(dictionary[name]);

    public new bool Remove(T item) => base.Remove(item) && dictionary.Remove(item.Name);

    public bool Remove(string name) => Remove(dictionary[name]);
}
