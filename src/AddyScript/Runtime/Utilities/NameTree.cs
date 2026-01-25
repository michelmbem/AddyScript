using System.Collections.Generic;

using AddyScript.Ast.Expressions;


namespace AddyScript.Runtime.Utilities;


/// <summary>
/// A tree used to optimize the caching of qualified names.
/// </summary>
public class NameTree
{
    private readonly List<NameTreeNode> roots = [];

    #region Public interface

    /// <summary>
    /// Gets the value attached to a qualified name from the tree.
    /// </summary>
    /// <param name="name">The given name</param>
    /// <returns>The object attached to <paramref name="name"/> if any or null</returns>
    public object this[QualifiedName name] => Find(roots, name, 0);

    /// <summary>
    /// Adds a qualified name to the tree.
    /// </summary>
    /// <param name="name">The name to be added</param>
    /// <param name="value">The value attached to the given name</param>
    public void Add(QualifiedName name, object value)
    {
        Insert(value, null, roots, name, 0);
    }

    /// <summary>
    /// Gets if a qualified name is stored in the tree.
    /// </summary>
    /// <param name="name">The name to find</param>
    /// <returns><b>true</b> if found; <b>false</b> otherwise</returns>
    public bool Contains(QualifiedName name) => Find(roots, name, 0) != null;

    /// <summary>
    /// Removes a qualified name and its attached value from the tree.
    /// </summary>
    /// <param name="name">The name to be removed</param>
    /// <returns><b>true</b> on success; <b>false</b> on failure</returns>
    public bool Remove(QualifiedName name) => Remove(roots, name, 0);

    /// <summary>
    /// Empties the tree.
    /// </summary>
    public void Clear()
    {
        roots.Clear();
    }

    #endregion

    #region Utility

    private static void Insert(object value, NameTreeNode parent, List<NameTreeNode> nodes, QualifiedName name, int offset)
    {
        if (offset >= name.Length) return;

        int index = BinarySearch(nodes, name[offset].ToString(), out bool found);
        if (!found)
        {
            object nodeValue = offset < name.Length - 1 ? null : value;
            var node = new NameTreeNode(name[offset].ToString(), nodeValue) {Parent = parent};

            if (index < nodes.Count)
                nodes.Insert(index, node);
            else
                nodes.Add(node);
        }

        Insert(value, nodes[index], nodes[index].Children, name, offset + 1);
    }

    private static object Find(List<NameTreeNode> nodes, QualifiedName name, int offset)
    {
        int index = BinarySearch(nodes, name[offset].ToString(), out bool found);
        if (!found) return null;

        return offset < name.Length - 1
             ? Find(nodes[index].Children, name, offset + 1)
             : nodes[index].Value;
    }

    private static bool Remove(List<NameTreeNode> nodes, QualifiedName name, int offset)
    {
        int index = BinarySearch(nodes, name[offset].ToString(), out bool found);
        if (!found) return false;

        if (offset < name.Length - 1)
            return Remove(nodes[index].Children, name, offset + 1);

        nodes.RemoveAt(index);
        return true;
    }

    private static int BinarySearch(List<NameTreeNode> nodes, string name, out bool found)
    {
        int l = 0, r = nodes.Count - 1;
        found = false;

        while (l <= r)
        {
            int m = (l + r) / 2;
            found = nodes[m].Name == name;
            if (found) return m;

            if (string.CompareOrdinal(nodes[m].Name, name) < 0)
                l = m + 1;
            else
                r = m - 1;
        }

        return l;
    }

    #endregion
}
