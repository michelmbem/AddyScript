using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a set's initializer: a set of item initializer into braces prefixed with @.
    /// </summary>
    public class SetInitializer : ListInitializer
    {
        /// <summary>
        /// Initializes a new instance of SetInitializer
        /// </summary>
        /// <param name="items">The expressions that are listed between the delimiters</param>
        public SetInitializer(params Expression[] items)
            : base(items)
        {
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileSetInitializer(this);
        }
    }
}