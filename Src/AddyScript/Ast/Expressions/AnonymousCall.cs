using System;

using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to an anonymous function.
    /// </summary>
    public class AnonymousCall : FunctionCall
    {
        /// <summary>
        /// Initializes a new instance of AnonymousCall
        /// </summary>
        /// <param name="callee">The expression to be used to get a function</param>
        /// <param name="arguments">The list of arguments passed to the function</param>
        public AnonymousCall(Expression callee, params Expression[] arguments)
            : base("__anonymous_" + Environment.TickCount, arguments)
        {
            Callee = callee;
        }

        /// <summary>
        /// Represents the expression to be used to get a function.
        /// </summary>
        public Expression Callee { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileAnonymousCall(this);
        }
    }
}