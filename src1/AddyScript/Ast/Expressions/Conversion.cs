using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a conversion.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of Conversion
    /// </remarks>
    /// <param name="expr">Expression to convert</param>
    /// <param name="typeName">The target type's name</param>
    public class Conversion(Expression expr, string typeName) : TypeExpression(typeName)
    {

        /// <summary>
        /// The expression to convert.
        /// </summary>
        public Expression Expression { get; private set; } = expr;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateConversion(this);
        }
    }
}