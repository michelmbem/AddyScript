using AddyScript.Compilers;
using AddyScript.Runtime;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of an external function.
    /// </summary>
    public class ExternalFunctionDecl : StatementWithAttributes
    {
        /// <summary>
        /// Initializes a new instance of ExternalFunctionDecl
        /// </summary>
        /// <param name="name">The function's name</param>
        /// <param name="parameters">The function's parameters</param>
        public ExternalFunctionDecl(string name, params Parameter[] parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        /// <summary>
        /// The function's name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The function's parameters
        /// </summary>
        public Parameter[] Parameters { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileExternalFunctionDecl(this);
        }
    }
}