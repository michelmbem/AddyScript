using System.Reflection;

using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents the way to call a native function.
    /// </summary>
    public class ExternalFunctionCall : Expression
    {
        /// <summary>
        /// Initializes a new instance of ExternalFunctionCall
        /// </summary>
        /// <param name="method">A wrapper around the target native function</param>
        /// <param name="arguments">The arguments passed to the target function</param>
        public ExternalFunctionCall(MethodInfo method, params Expression[] arguments)
        {
            Method = method;
            Arguments = arguments;
        }

        /// <summary>
        /// Represents a wrapper around the target native function.
        /// </summary>
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// Represents the arguments passed to the target function.
        /// </summary>
        public Expression[] Arguments { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileExternalFunctionCall(this);
        }
    }
}