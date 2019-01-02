using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an object initializer: a set of field initializers into braces.
    /// </summary>
    public class ObjectInitializer : Expression
    {
        /// <summary>
        /// Initializes a new instance of ObjectInitializer
        /// </summary>
        /// <param name="initializers">A set of initializers for the object's fields</param>
        public ObjectInitializer(params PropertyInitializer[] initializers)
        {
            PropertyInitializers = initializers;
        }

        /// <summary>
        /// A set of initializers for the object's fields.
        /// </summary>
        public PropertyInitializer[] PropertyInitializers { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileObjectInitializer(this);
        }
    }
}