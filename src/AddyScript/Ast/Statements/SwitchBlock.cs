using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>switch</b> block.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of SwitchBlock
    /// </remarks>
    /// <param name="expr">The test expression.</param>
    /// <param name="cases">The case labels</param>
    /// <param name="defCase">The default case label</param>
    /// <param name="stmts">The switch statements.</param>
    public class SwitchBlock(Expression expr, CaseLabel[] cases, int defCase, Statement[] stmts)
        : Block(stmts)
    {

        /// <summary>
        /// The test expression.
        /// </summary>
        public Expression Expression { get; private set; } = expr;

        /// <summary>
        /// The case labels.
        /// </summary>
        public CaseLabel[] Cases { get; private set; } = cases;

        /// <summary>
        /// The default case label.
        /// </summary>
        public int DefaultCase { get; private set; } = defCase;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateSwitchBlock(this);
        }
    }
}