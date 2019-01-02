using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of a set of constants.
    /// </summary>
    public class ConstantDecl : VariableDecl
    {
        /// <summary>
        /// Initializes a new instance of ConstantDecl
        /// </summary>
        /// <param name="initializers">The set of (name, value) couples used to define constants.</param>
        public ConstantDecl(params PropertyInitializer[] initializers)
            : base(initializers)
        {
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileConstantDecl(this);
        }
    }
}