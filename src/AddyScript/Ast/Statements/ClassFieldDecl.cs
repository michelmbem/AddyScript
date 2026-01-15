using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents the declaration of class field.
/// </summary>
public class ClassFieldDecl : ClassMemberDecl
{
    /// <summary>
    /// Initializes a new instance of ClassFieldDecl.
    /// </summary>
    /// <param name="name">The field's name</param>
    /// <param name="scope">The scope of this field</param>
    /// <param name="modifier">Determines whether this field is final, static or not</param>
    /// <param name="initializer">Provides the default value of the field</param>
    public ClassFieldDecl(string name, Scope scope, Modifier modifier, Expression initializer)
        : base(name, scope, modifier)
    {
        Initializer = initializer;
    }

    /// <summary>
    /// Provides the default value of the field.
    /// </summary>
    public Expression Initializer { get; }

    /// <summary>
    /// Creates a <see cref="ClassMember"/> from this instance.
    /// </summary>
    public override ClassMember ToClassMember() => new ClassField(Name, Scope, Modifier, Initializer);
}