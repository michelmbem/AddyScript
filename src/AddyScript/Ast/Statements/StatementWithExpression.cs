using AddyScript.Ast.Expressions;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents of all statements that have a single expression attached to them.
    /// </summary>
    /// <remarks>
    /// Intializes a new instance of <see cref="StatementWithExpression"/>.
    /// </remarks>
    /// <param name="expression">The expression that's attached to this statement</param>
    public abstract class StatementWithExpression(Expression expression) : Statement
    {

        /// <summary>
        /// The expression that's attached to this statement.
        /// </summary>
        public Expression Expression { get; private set; } = expression;
    }
}
