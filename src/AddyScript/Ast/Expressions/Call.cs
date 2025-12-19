using System.Linq;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// An generic representation of a call to a function or method.
/// </summary>
/// <remarks>
/// Initializes a new instance of Call
/// </remarks>
/// <param name="arguments">The list of arguments passed to the function or method</param>
public abstract class Call(Argument[] arguments) : Expression
{
    /// <summary>
    /// Represents the list of arguments passed to the function or method.
    /// </summary>
    public Argument[] Arguments => arguments;

    /// <summary>
    /// Converts an array of expressions to an array of <see cref="Argument"/>s.
    /// </summary>
    /// <param name="arguments">The array of <see cref="Expression"/>s to convert</param>
    /// <returns>An array of <see cref="Argument"/>s</returns>
    public static Argument[] ToArguments(Expression[] arguments) =>
        [.. arguments.Select(arg => new Argument(arg))];
}