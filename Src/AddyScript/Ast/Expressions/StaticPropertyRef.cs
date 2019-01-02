using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a reference to a static property.<br/>
    /// May also match an instance property.
    /// </summary>
    public class StaticPropertyRef : Expression
    {
        /// <summary>
        /// Initializes a new instance of StaticPropertyRef
        /// </summary>
        /// <param name="name">The qualified property's name</param>
        public StaticPropertyRef(QualifiedName name)
        {
            Name = name;
        }

        /// <summary>
        /// The qualified property's name.
        /// </summary>
        public QualifiedName Name { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileStaticPropertyRef(this);
        }
    }
}