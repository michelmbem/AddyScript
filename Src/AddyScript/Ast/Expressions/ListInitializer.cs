using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a list's initializer: a set of item initializers into brackets.
    /// </summary>
    public class ListInitializer : Expression
    {
        /// <summary>
        /// Initializes a new instance of ListInitializer
        /// </summary>
        /// <param name="items">The expressions that are listed between the delimiters</param>
        public ListInitializer(params Expression[] items)
        {
            Items = items;
        }

        /// <summary>
        /// The expressions that are listed between the delimiters.
        /// </summary>
        public Expression[] Items { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileListInitializer(this);
        }
    }
}