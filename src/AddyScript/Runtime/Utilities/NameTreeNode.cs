using System.Collections.Generic;


namespace AddyScript.Runtime.Utilities;


/// <summary>
/// Represents a node in a <see cref="NameTree"/>.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="NameTreeNode"/>.
/// </remarks>
/// <param name="name">The name given to the node</param>
/// <param name="value">The attached value</param>
public class NameTreeNode(string name, object value)
{

    /// <summary>
    /// The name given to the node.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The value attached to the node.
    /// </summary>
    public object Value { get; set; } = value;

    /// <summary>
    /// A reference to the parent node.
    /// </summary>
    public NameTreeNode Parent { get; set; } = null;

    /// <summary>
    /// The collection of children nodes.
    /// </summary>
    public List<NameTreeNode> Children { get; set; } = [];

    /// <summary>
    /// Gets the fummy qualified name of this node
    /// </summary>
    public string FullName => Parent == null ? Name : $"{Parent.FullName}::{Name}";

    ///<summary>
    ///Returns a <see cref="string" /> that represents the current <see cref="object" />.
    ///</summary>
    ///
    ///<returns>
    ///A <see cref="string" /> that represents the current <see cref="object" />.
    ///</returns>
    ///<filterpriority>2</filterpriority>
    public override string ToString() => FullName;
}
