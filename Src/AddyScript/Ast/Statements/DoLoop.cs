using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>do-while</b> statement
    /// </summary>
    public class DoLoop : WhileLoop
    {
        /// <summary>
        /// Initializes a new instance of DoLoop
        /// </summary>
        /// <param name="guard">The condition of the loop</param>
        /// <param name="body">The body of the loop</param>
        public DoLoop(Expression guard, Statement body)
            : base(guard, body)
        {
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileDoLoop(this);
        }
    }
}