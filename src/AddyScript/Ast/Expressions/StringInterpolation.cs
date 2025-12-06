using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a literal string with embedded expressions to interpolate.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="StringInterpolation"/>.
    /// </remarks>
    /// <param name="pattern">The literal string that will be used as a pattern to build the final string</param>
    /// <param name="substitutions">The expressions that will be evaluated to get the dynamic parts of the final string</param>
    public class StringInterpolation(string pattern, params Expression[] substitutions) : Expression
    {

        /// <summary>
        /// The literal string that will be used as a pattern to build the final string.
        /// </summary>
        public string Pattern => pattern;

        /// <summary>
        /// The expressions that will be evaluated to get the dynamic parts of the final string.
        /// </summary>
        public Expression[] Substitions => substitutions;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateStringInterpolation(this);
        }
    }
}