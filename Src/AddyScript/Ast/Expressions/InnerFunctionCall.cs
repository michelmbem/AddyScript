using AddyScript.Compilers;
using AddyScript.Runtime;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents the way to call a built-in function.
    /// </summary>
    public class InnerFunctionCall : Call
    {
        /// <summary>
        /// Initializes a new instance of InnerFunctionCall
        /// </summary>
        /// <param name="function">The inner function to call</param>
        /// <param name="arguments">The list of arguments</param>
        public InnerFunctionCall(InnerFunction function, params Expression[] arguments)
            : base(arguments)
        {
            Function = function;
        }

        /// <summary>
        /// Represents the inner function to call.
        /// </summary>
        public InnerFunction Function { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileInnerFunctionCall(this);
        }
    }
}