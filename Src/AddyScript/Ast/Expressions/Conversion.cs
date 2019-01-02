using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a conversion.
    /// </summary>
    public class Conversion : TypeExpression
    {
        /// <summary>
        /// Initializes a new instance of Conversion
        /// </summary>
        /// <param name="expr">Expression to convert</param>
        /// <param name="typeName">The target type's name</param>
        public Conversion(Expression expr, string typeName)
            : base(typeName)
        {
            Expression = expr;
        }

        /// <summary>
        /// The expression to convert.
        /// </summary>
        public Expression Expression { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileConversion(this);
        }
    }
}