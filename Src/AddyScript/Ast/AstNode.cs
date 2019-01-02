using AddyScript.Compilers;


namespace AddyScript.Ast
{
    /// <summary>
    /// The base class of all AST nodes.
    /// </summary>
    public abstract class AstNode : ScriptElement
    {
        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public abstract void AcceptCompiler(ICompiler compiler);
    }
}