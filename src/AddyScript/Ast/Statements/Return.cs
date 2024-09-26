using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a 'return' statement.
    /// </summary>
    public class Return : Statement
    {
        /// <summary>
        /// Initializes a new instance of Return
        /// </summary>
        public Return()
        {
        }

        /// <summary>
        /// Initializes a new instance of Return
        /// </summary>
        /// <param name="expr">The expression to be returned</param>
        public Return(Expression expr)
        {
            Expression = expr;
        }

        /// <summary>
        /// The expression to be returned
        /// </summary>
        public Expression Expression { get; private set; }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateReturn(this);
        }
    }
}