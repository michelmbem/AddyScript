namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a list or set initializer item.
    /// Also used to represent functions positional arguments.
    /// </summary>
    /// <param name="expr">The item's value</param>
    public class ListItem(Expression expr, bool spread = false) : ScriptElement
    {
        /// <summary>
        /// The item's value.
        /// </summary>
        public Expression Expression => expr;

        /// <summary>
        /// Tells if <see cref="Expression"/> represents a collection that should be spread.
        /// </summary>
        public bool Spread => spread;
    }
}
