using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a map's initializer: a set of item initializers into braces.
    /// </summary>
    public class MapInitializer : Expression
    {
        /// <summary>
        /// Initializes a new instance of ArrayInitializer
        /// </summary>
        /// <param name="itemInitializers">The item initializers that are listed between the braces</param>
        public MapInitializer(params MapItemInitializer[] itemInitializers)
        {
            ItemInitializers = itemInitializers;
        }

        /// <summary>
        /// The expressions that are listed between the braces.
        /// </summary>
        public MapItemInitializer[] ItemInitializers { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileMapInitializer(this);
        }
    }
}