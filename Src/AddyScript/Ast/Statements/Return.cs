using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a 'return' statement.
    /// </summary>
    public class Return : Statement
    {
        /// <summary>
        /// Initializes a new instance of Return
        /// </summary>
        public Return()
        {
        }

        /// <summary>
        /// Initializes a new instance of Return
        /// </summary>
        /// <param name="expr">The expression to be returned</param>
        public Return(Expression expr)
        {
            Expression = expr;
        }

        /// <summary>
        /// The expression to be returned
        /// </summary>
        public Expression Expression { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileReturn(this);
        }
    }
}