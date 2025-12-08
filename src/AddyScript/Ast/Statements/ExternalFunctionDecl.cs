using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of an external function.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ExternalFunctionDecl
    /// </remarks>
    /// <param name="name">The function's name</param>
    /// <param name="parameters">The function's parameters</param>
    public class ExternalFunctionDecl(string name, params ParameterDecl[] parameters) : StatementWithAttributes
    {

        /// <summary>
        /// The function's name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// The function's parameters
        /// </summary>
        public ParameterDecl[] Parameters => parameters;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateExternalFunctionDecl(this);
        }
    }
}