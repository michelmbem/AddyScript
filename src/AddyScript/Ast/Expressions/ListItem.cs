namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a list or set initializer item.
    /// </summary>
    /// <param name="expr">The wrapped expression</param>
    public class ListItem(Expression expr, bool spread = false) : ScriptElement
    {
        /// <summary>
        /// The wrapped expression.
        /// </summary>
        public Expression Expression { get; private set; } = expr;

        /// <summary>
        /// Tells if <see cref="Expression"/> represents a collection that should be spread.
        /// </summary>
        public bool Spread { get; private set; } = spread;
    }
}
