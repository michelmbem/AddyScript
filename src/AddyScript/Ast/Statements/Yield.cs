using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>yield</b> statement.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of Yield
    /// </remarks>
    /// <param name="expr">The expression to yield</param>
    public class Yield(Expression expr) : Statement
    {

        /// <summary>
        /// The expression to yield.
        /// </summary>
        public Expression Expression { get; private set; } = expr;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateYield(this);
        }
    }
}