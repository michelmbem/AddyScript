using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to a static method.<br/>
    /// May also match an instance method.
    /// </summary>
    public class StaticMethodCall : Call
    {
        /// <summary>
        /// Initializes a new instance of StaticMethodCall
        /// </summary>
        /// <param name="name">The qualified method's name</param>
        /// <param name="arguments">The arguments passed to the method</param>
        public StaticMethodCall(QualifiedName name, params Expression[] arguments)
            : base(arguments)
        {
            Name = name;
        }

        /// <summary>
        /// The qualified method's name.
        /// </summary>
        public QualifiedName Name { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileStaticMethodCall(this);
        }
    }
}