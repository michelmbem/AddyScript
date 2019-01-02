using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a type's verification.
    /// </summary>
    public class TypeVerification : TypeExpression
    {
        /// <summary>
        /// Initializes a new instance of TypeVerification
        /// </summary>
        /// <param name="expr">The target expression</param>
        /// <param name="typeName">The type's name</param>
        public TypeVerification(Expression expr, string typeName)
            : base(typeName)
        {
            Expression = expr;
        }

        /// <summary>
        /// The target expression.
        /// </summary>
        public Expression Expression { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileTypeVerification(this);
        }
    }
}