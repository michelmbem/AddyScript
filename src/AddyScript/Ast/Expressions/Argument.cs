namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a positional function argument or a tuple/list/set initializer item.
/// </summary>
/// <param name="value">The argument's value</param>
/// <param name="spread">
/// Tells whether <paramref name="value"/> represents a collection that should be expanded or not
/// </param>
public class Argument(Expression value, bool spread = false) : ScriptElement
{
    /// <summary>
    /// The argument's value.
    /// </summary>
    public Expression Value => value;

    /// <summary>
    /// Tells whether <see cref="Value"/> represents a collection that should be expanded or not.
    /// </summary>
    public bool Spread => spread;
}