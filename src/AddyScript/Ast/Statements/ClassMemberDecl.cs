using AddyScript.Runtime.OOP;


namespace AddyScript.Ast.Statements;


/// <summary>
/// The base class of all declarations of class members.
/// </summary>
/// <remarks>
/// Initializes a new instance of ClassMemberDecl.
/// </remarks>
/// <param name="name">The member's name</param>
/// <param name="scope">The scope of this member</param>
/// <param name="modifier">Determines whether this member is abstract, final, static or none</param>
public abstract class ClassMemberDecl(string name, Scope scope, Modifier modifier) : SymbolWithAttributes
{
    /// <summary>
    /// The member's name.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// The scope of this member.
    /// </summary>
    public Scope Scope => scope;

    /// <summary>
    /// Determines whether this member is abstract, final, static or none.
    /// </summary>
    public Modifier Modifier => modifier;

    /// <summary>
    /// Gets if this member is static.
    /// </summary>
    public bool IsStatic => Modifier is Modifier.Static or Modifier.StaticFinal;

    /// <summary>
    /// Gets if this member is final.
    /// </summary>
    public bool IsFinal => Modifier is Modifier.Final or Modifier.StaticFinal;

    /// <summary>
    /// Creates a <see cref="ClassMember"/> from this instance.
    /// </summary>
    public abstract ClassMember ToClassMember();
}