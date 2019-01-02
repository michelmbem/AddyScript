using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an expression like <b>typeof</b>(<i>&lt;some-type&gt;</i>)'.
    /// </summary>
    public class TypeOfExpression : TypeExpression
    {
        /// <summary>
        /// Initializes a new instance of TypeOfExpression
        /// </summary>
        /// <param name="typeName">The type's name</param>
        public TypeOfExpression(string typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileTypeOfExpression(this);
        }
    }
}