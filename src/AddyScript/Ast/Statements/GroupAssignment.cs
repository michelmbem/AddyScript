using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represent a <see cref="Statement"/> where several variables are set a value at once.
    /// </summary>
    /// <param name="lValues">The variables to set values to</param>
    /// <param name="rValues">The values that should be set to the variables</param>
    public class GroupAssignment(Expression[] lValues, Expression[] rValues) : Statement
    {
        /// <summary>
        /// The variables to set values to.
        /// </summary>
        public Expression[] LValues { get; private set; } = lValues;

        /// <summary>
        /// The values that should be set to the variables.
        /// </summary>
        public Expression[] RValues { get; private set; } = rValues;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateGroupAssignment(this);
        }
    }
}
