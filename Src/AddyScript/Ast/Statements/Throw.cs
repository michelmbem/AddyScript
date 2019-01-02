using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>throw</b> statement.
    /// </summary>
    public class Throw : Statement
    {
        /// <summary>
        /// Initializes a new instance of Throw
        /// </summary>
        /// <param name="expr">The exception to be thrown</param>
        public Throw(Expression expr)
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
            compiler.CompileThrow(this);
        }
    }
}