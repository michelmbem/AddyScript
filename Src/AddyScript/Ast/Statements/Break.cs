using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a 'break' statement.
    /// </summary>
    public class Break : Statement
    {
        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileBreak(this);
        }
    }
}