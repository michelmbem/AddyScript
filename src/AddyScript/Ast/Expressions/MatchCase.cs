namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a case in a <b>switch</b> expression.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of MatchCase.
    /// </remarks>
    /// <param name="pattern">The pattern to match against</param>
    /// <param name="expression">The expression returned in case of a positive match</param>
    public class MatchCase(Pattern pattern, Expression expression) : ScriptElement
    {

        /// <summary>
        /// The pattern to match against
        /// </summary>
        public Pattern Pattern => pattern;

        /// <summary>
        /// The expression returned in case of a positive match
        /// </summary>
        public Expression Expression => expression;
    }
}
