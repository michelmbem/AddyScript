using AddyScript.Runtime.DataItems;


namespace AddyScript.Runtime.OOP;


/// <summary>
/// The base class of all class members.
/// </summary>
/// <remarks>
/// Initializes a new instance of ClassMember.
/// </remarks>
/// <param name="name">The member's name</param>
/// <param name="scope">The scope of this member</param>
/// <param name="modifier">Determines whether this member is abstract, final, static or none</param>
public abstract class ClassMember(string name, Scope scope, Modifier modifier)
{

    /// <summary>
    /// The member's name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The scope of this member.
    /// </summary>
    public Scope Scope { get; } = scope;

    /// <summary>
    /// Determines whether this member is abstract, final, static or none.
    /// </summary>
    public Modifier Modifier { get; } = modifier;

    /// <summary>
    /// Represents the class that holds this member (the class in which the member is declared).
    /// </summary>
    public Class Holder { get; set; }

    /// <summary>
    /// The name of this member prefixed by the class name.
    /// </summary>
    public string FullName => Holder != null ? $"{Holder.Name}::{Name}" : Name;

    /// <summary>
    /// The member's attributes.
    /// </summary>
    public DataItem[] Attributes { get; set; }
}