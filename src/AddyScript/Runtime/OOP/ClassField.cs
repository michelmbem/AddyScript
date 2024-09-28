using AddyScript.Ast.Expressions;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Runtime.OOP;


/// <summary>
/// Represents a field in a class.
/// </summary>
public class ClassField : ClassMember
{
    /// <summary>
    /// Initializes a new instance of ClassField.
    /// </summary>
    /// <param name="name">The field's name</param>
    /// <param name="scope">The scope of this field</param>
    /// <param name="modifier">Determines whether this field is static or not</param>
    /// <param name="init">Provides the default value of the field</param>
    public ClassField(string name, Scope scope, Modifier modifier, Expression init)
        : base(name, scope, modifier)
    {
        Initializer = init;
        if (modifier == Modifier.Static)
            SharedValue = Void.Value;
    }

    /// <summary>
    /// Provides the default value of the field.
    /// </summary>
    public Expression Initializer { get; private set; }

    /// <summary>
    /// Holds the value of a static field.
    /// </summary>
    public DataItem SharedValue { get; set; }
}