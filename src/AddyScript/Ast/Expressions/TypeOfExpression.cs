using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an expression like <b>typeof</b>(<i>&lt;some-type&gt;</i>)'.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of TypeOfExpression
    /// </remarks>
    /// <param name="typeName">The type's name</param>
    public class TypeOfExpression(string typeName) : TypeExpression(typeName)
    {

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateTypeOfExpression(this);
        }
    }
}