using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>do-while</b> statement
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of DoLoop
    /// </remarks>
    /// <param name="guard">The condition of the loop</param>
    /// <param name="action">The body of the loop</param>
    public class DoLoop(Expression guard, Statement action) : WhileLoop(guard, action)
    {

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateDoLoop(this);
        }
    }
}