namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// An generic representation of a call to a function or method.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of Call
    /// </remarks>
    /// <param name="arguments">The list of arguments passed to the function or method</param>
    public abstract class Call(Expression[] arguments) : Expression
    {

        /// <summary>
        /// Represents the list of arguments passed to the function or method.
        /// </summary>
        public Expression[] Arguments { get; private set; } = arguments;
    }
}