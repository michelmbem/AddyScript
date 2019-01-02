using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents an explicit variable's declaration.
    /// <remarks>Several variables can be declared once.</remarks>
    /// </summary>
    public class VariableDecl : Statement
    {
        /// <summary>
        /// Initializes a new instance of VariableDecl
        /// </summary>
        /// <param name="initializers">The set of (name, value) couples used to initialize variables.</param>
        public VariableDecl(params PropertyInitializer[] initializers)
        {
            Initializers = initializers;
        }

        /// <summary>
        /// The set of (name, value) couples used to initialize variables.
        /// </summary>
        public PropertyInitializer[] Initializers { get; private set; }

        /// <summary>
        /// A factory method to quickly create an instance with a single initializer.
        /// </summary>
        /// <param name="name">The variable's name</param>
        /// <param name="expr">An expression</param>
        /// <returns>A <see cref="VariableDecl"/></returns>
        public static VariableDecl Single(string name, Expression expr)
        {
            return new VariableDecl(new PropertyInitializer(name, expr));
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileVariableDecl(this);
        }
    }
}