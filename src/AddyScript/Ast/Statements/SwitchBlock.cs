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
    /// <param name="test">The test expression.</param>
    /// <param name="cases">The case labels</param>
    /// <param name="defCase">The default case label</param>
    /// <param name="stmts">The switch statements.</param>
    public class SwitchBlock(Expression test, CaseLabel[] cases, int defCase, Statement[] stmts)
        : Block(stmts)
    {

        /// <summary>
        /// The test expression.
        /// </summary>
        public Expression Test => test;

        /// <summary>
        /// The case labels.
        /// </summary>
        public CaseLabel[] Cases => cases;

        /// <summary>
        /// The default case label.
        /// </summary>
        public int DefaultCase => defCase;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateSwitchBlock(this);
        }
    }
}