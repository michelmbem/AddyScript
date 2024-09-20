using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of a set of constants.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ConstantDecl
    /// </remarks>
    /// <param name="initializers">The set of (name, value) couples used to define constants.</param>
    public class ConstantDecl(params PropertyInitializer[] initializers) : VariableDecl(initializers)
    {

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateConstantDecl(this);
        }
    }
}