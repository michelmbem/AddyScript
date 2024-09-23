using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>for</b> statement
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ForLoop
    /// </remarks>
    /// <param name="initializers">The set of statements that run before enterring the loop</param>
    /// <param name="guard">The condition</param>
    /// <param name="updaters">The set of statements that run at the end of each iteration</param>
    /// <param name="action">The body of the loop</param>
    public class ForLoop(Statement[] initializers, Expression guard, Expression[] updaters, Statement action) : Statement
    {

        /// <summary>
        /// Represents the set of statements that run before enterring the loop.
        /// </summary>
        public Statement[] Initializers { get; private set; } = initializers;

        /// <summary>
        /// Represents the condition of the loop.
        /// </summary>
        public Expression Guard { get; private set; } = guard;

        /// <summary>
        /// Represents the set of statements that run at the end of each iteration.
        /// </summary>
        public Expression[] Updaters { get; private set; } = updaters;

        /// <summary>
        /// Represents the body of the loop.
        /// </summary>
        public Statement Action { get; private set; } = action;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateForLoop(this);
        }
    }
}