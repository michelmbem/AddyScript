using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents a <b>for</b> statement
/// </summary>
/// <remarks>
/// Initializes a new instance of ForLoop
/// </remarks>
/// <param name="initializers">The set of statements that run before enterring the loop</param>
/// <param name="guard">The condition of the loop</param>
/// <param name="incrementers">The set of statements that run at the end of each iteration</param>
/// <param name="action">The body of the loop</param>
public class ForLoop(Statement[] initializers, Expression guard,
                     Expression[] incrementers, Statement action)
    : FlowControlStatement(guard, action)
{
    /// <summary>
    /// Represents the set of statements that run before enterring the loop.
    /// </summary>
    public Statement[] Initializers => initializers;

    /// <summary>
    /// Represents the set of statements that run at the end of each iteration.
    /// </summary>
    public Expression[] Incrementers => incrementers;

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateForLoop(this);
    }
}