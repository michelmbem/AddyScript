using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to the original implementation of an overriden method.
    /// </summary>
    public class ParentMethodCall : FunctionCall
    {
        /// <summary>
        /// Initializes a new instance of ParentMethodCall
        /// </summary>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="arguments">The list of arguments passed to the method</param>
        public ParentMethodCall(string methodName, params Expression[] arguments)
            : base(methodName, arguments)
        {
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileParentMethodCall(this);
        }
    }
}