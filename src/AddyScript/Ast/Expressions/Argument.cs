namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents an element in a list of positional arguments.
/// </summary>
/// <param name="expr">The argument's value</param>
public class Argument(Expression expr, bool spread = false) : ScriptElement
{
    /// <summary>
    /// The argument's value.
    /// </summary>
    public Expression Expression => expr;

    /// <summary>
    /// Tells whether <see cref="Expression"/> represents a collection that should be spread or not.
    /// </summary>
    public bool Spread => spread;
}