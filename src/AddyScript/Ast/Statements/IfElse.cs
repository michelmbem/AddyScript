using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents an <b>if-else</b> statement.
    /// </summary>
    public class IfElse : FlowControlStatement
    {
        /// <summary>
        /// Initializes a new instance of IfElse
        /// </summary>
        /// <param name="test">The test expression</param>
        /// <param name="action">The statement to be executed if <paramref name="test"/> returns true</param>
        /// <param name="altAction">The statement to be executed if <paramref name="test"/> returns false</param>
        public IfElse(Expression test, Statement action, Statement altAction) : base(test, action)
        {
            AlternativeAction = altAction;
        }

        /// <summary>
        /// Initializes a new instance of IfThenElse
        /// </summary>
        /// <param name="test">The test expression</param>
        /// <param name="action">The statement to be executed if <paramref name="test"/> returns true</param>
        public IfElse(Expression test, Statement action) : base(test, action)
        {
        }

        /// <summary>
        /// The statement to be executed if <i>Test</i> returns <b>false</b>.
        /// </summary>
        public Statement AlternativeAction { get; private set; }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateIfElse(this);
        }
    }
}