using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to a constructor.
    /// </summary>
    public class ConstructorCall : StaticMethodCall
    {
        /// <summary>
        /// Initializes a new instance of ConstructorCall
        /// </summary>
        /// <param name="name">The name of the class to instanciate</param>
        /// <param name="arguments">The list of arguments passed to the constructor</param>
        /// <param name="initializers">A set of property initializers for the new object</param>
        public ConstructorCall(QualifiedName name, Expression[] arguments, PropertyInitializer[] initializers)
            : base(name, arguments)
        {
            PropertyInitializers = initializers;
        }

        /// <summary>
        /// A set of property initializers for the newly created object.
        /// </summary>
        public PropertyInitializer[] PropertyInitializers { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileConstructorCall(this);
        }
    }
}