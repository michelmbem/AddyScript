using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>while</b> loop.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of WhileLoop
    /// </remarks>
    /// <param name="guard">The loop condition</param>
    /// <param name="action">The body of the loop</param>
    public class WhileLoop(Expression guard, Statement action) : Statement
    {

        /// <summary>
        /// The loop condition
        /// </summary>
        public Expression Guard { get; private set; } = guard;

        /// <summary>
        /// The body of the loop
        /// </summary>
        public Statement Action { get; private set; } = action;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateWhileLoop(this);
        }
    }
}