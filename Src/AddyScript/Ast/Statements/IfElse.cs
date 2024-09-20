using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents an <b>if-else</b> statement.
    /// </summary>
    public class IfElse : Statement
    {
        /// <summary>
        /// Initializes a new instance of IfElse
        /// </summary>
        /// <param name="condition">The test expression</param>
        /// <param name="positiveAction">The statement to be executed if <paramref name="condition"/> returns true</param>
        /// <param name="negativeAction">The statement to be executed if <paramref name="condition"/> returns false</param>
        public IfElse(Expression condition, Statement positiveAction, Statement negativeAction)
        {
            Condition = condition;
            PositiveAction = positiveAction;
            NegativeAction = negativeAction;
        }

        /// <summary>
        /// Initializes a new instance of IfThenElse
        /// </summary>
        /// <param name="condition">The test expression</param>
        /// <param name="positiveAction">The statement to be executed if <paramref name="condition"/> returns true</param>
        public IfElse(Expression condition, Statement positiveAction)
        {
            Condition = condition;
            PositiveAction = positiveAction;
        }

        /// <summary>
        /// The test expression
        /// </summary>
        public Expression Condition { get; private set; }

        /// <summary>
        /// The statement to be executed if <see cref="Condition"/> evaluates to true
        /// </summary>
        public Statement PositiveAction { get; private set; }

        /// <summary>
        /// The statement to be executed if <see cref="Condition"/> evaluates to false
        /// </summary>
        public Statement NegativeAction { get; private set; }

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateIfElse(this);
        }
    }
}