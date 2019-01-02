namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// An generic representation of a call to a function or a method.
    /// </summary>
    public abstract class Call : Expression
    {
        /// <summary>
        /// Initializes a new instance of Call
        /// </summary>
        /// <param name="arguments">The list of arguments passed to the function or method</param>
        protected Call(params Expression[] arguments)
        {
            Arguments = arguments;
        }

        /// <summary>
        /// Represents the list of arguments passed to the function or method.
        /// </summary>
        public Expression[] Arguments { get; private set; }
    }
}