using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>while</b> loop.
    /// </summary>
    public class WhileLoop : Statement
    {
        /// <summary>
        /// Initializes a new instance of WhileLoop
        /// </summary>
        /// <param name="guard">The loop condition</param>
        /// <param name="body">The body of the loop</param>
        public WhileLoop(Expression guard, Statement body)
        {
            Guard = guard;
            Body = body;
        }

        /// <summary>
        /// The loop condition
        /// </summary>
        public Expression Guard { get; private set; }

        /// <summary>
        /// The body of the loop
        /// </summary>
        public Statement Body { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileWhileLoop(this);
        }
    }
}