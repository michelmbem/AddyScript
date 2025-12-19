using AddyScript.Ast.Expressions;


namespace AddyScript.Ast.Statements;


/// <summary>
/// A generic representation of a flow control statement.
/// </summary>
/// <param name="guard">The expression that's tested for the <paramref name="action"/> to be executed</param>
/// <param name="action">The statement that's conditionally or repeatedly executed</param>
public abstract class FlowControlStatement(Expression guard, Statement action) : Statement
{
    /// <summary>
    /// The expression that's tested for the action to be executed.
    /// </summary>
    public Expression Guard => guard;

    /// <summary>
    /// The statement that's conditionally or repeatedly executed.
    /// </summary>
    public Statement Action => action;
}