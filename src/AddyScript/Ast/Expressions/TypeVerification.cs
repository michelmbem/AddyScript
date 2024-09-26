using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a type's verification.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of TypeVerification
    /// </remarks>
    /// <param name="expr">The target expression</param>
    /// <param name="typeName">The type's name</param>
    public class TypeVerification(Expression expr, string typeName) : TypeExpression(typeName)
    {

        /// <summary>
        /// The target expression.
        /// </summary>
        public Expression Expression { get; private set; } = expr;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateTypeVerification(this);
        }
    }
}