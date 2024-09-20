using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an assignment.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of Assignment
    /// </remarks>
    /// <param name="oper">The binary operator combined to the equal sign for this assignment</param>
    /// <param name="lValue">The variable that may be assigned</param>
    /// <param name="rValue">The value to assign to the variable</param>
    public class Assignment(BinaryOperator oper, Expression lValue, Expression rValue)
        : BinaryExpression(oper, lValue, rValue)
    {

        /// <summary>
        /// Initializes a new instance of Assignment
        /// </summary>
        /// <param name="lValue">The variable that may be assigned</param>
        /// <param name="rValue">The value to assign to the variable</param>
        public Assignment(Expression lValue, Expression rValue)
            : this(BinaryOperator.None, lValue, rValue)
        {
        }

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateAssignment(this);
        }
    }
}