using System.Linq;

using AddyScript.Translators;
using AddyScript.Runtime;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of a function.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of FunctionDecl
    /// </remarks>
    /// <param name="name">The function's name</param>
    /// <param name="parameters">The function's parameters</param>
    /// <param name="body">The function's body</param>
    public class FunctionDecl(string name, ParameterDecl[] parameters, Block body)
        : ExternalFunctionDecl(name, parameters)
    {

        /// <summary>
        /// The body of this function if it is user defined.
        /// </summary>
        public Block Body => body;

        /// <summary>
        /// Create a <see cref="Function"/> from this instance.
        /// </summary>
        /// <returns>A <see cref="Function"/></returns>
        public Function ToFunction()
        {
            return new Function(Parameters.Select(p => p.ToParameter()).ToArray(), Body);
        }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateFunctionDecl(this);
        }
    }
}