namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents the initializer of a sequential collection.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="SequenceInitializer"/>
/// </remarks>
/// <param name="arguments">The <see cref="Argument"/>s that are listed between the delimiters</param>
public abstract class SequenceInitializer(params Argument[] arguments) : Expression
{
    /// <summary>
    /// The <see cref="Argument"/> that are listed between the delimiters.
    /// </summary>
    public Argument[] Items => arguments;
}