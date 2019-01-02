using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to a function.
    /// </summary>
    public class FunctionCall : Call
    {
        /// <summary>
        /// Initializes a new instance of FunctionCall
        /// </summary>
        /// <param name="functionName">The name of the function to invoke</param>
        /// <param name="arguments">The list of arguments passed to the function</param>
        public FunctionCall(string functionName, params Expression[] arguments)
            : base(arguments)
        {
            FunctionName = functionName;
        }

        /// <summary>
        /// Represents the name of the function to invoke.
        /// </summary>
        public string FunctionName { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileFunctionCall(this);
        }
    }
}