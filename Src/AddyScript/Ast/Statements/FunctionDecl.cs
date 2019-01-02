using AddyScript.Compilers;
using AddyScript.Runtime;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of a function.
    /// </summary>
    public class FunctionDecl : StatementWithAttributes
    {
        /// <summary>
        /// Initializes a new instance of FunctionDecl
        /// </summary>
        /// <param name="name">The function's name</param>
        /// <param name="function">The function's definition</param>
        public FunctionDecl(string name, Function function)
        {
            Name = name;
            Function = function;
        }

        /// <summary>
        /// The function's name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The function's definition
        /// </summary>
        public Function Function { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileFunctionDecl(this);
        }
    }
}