using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// The base class of all statements.<br/>
    /// Also represents an empty statement.
    /// </summary>
    public class Statement : AstNode
    {
        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            // Simply does nothing
        }
    }
}