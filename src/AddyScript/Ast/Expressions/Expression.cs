using AddyScript.Ast.Statements;

namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// The base class of all expressions.
    /// </summary>
    public abstract class Expression : Statement
    {
        /// <summary>
        /// Gets/Sets if this expression is parenthesized in the source code.<br/>
        /// This is only used to correctly regenerate source code.
        /// </summary>
        public bool IsParenthesized { get; set; }
    }
}