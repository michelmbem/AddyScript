using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a unary expression.
    /// </summary>
    public class UnaryExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of UnaryExpression
        /// </summary>
        /// <param name="oper">A unary operator</param>
        /// <param name="expr">The operand</param>
        public UnaryExpression(UnaryOperator oper, Expression expr)
        {
            Operator = oper;
            Operand = expr;
        }

        /// <summary>
        /// Represents the operator of this unary expession.
        /// </summary>
        public UnaryOperator Operator { get; private set; }

        /// <summary>
        /// Represents the operand of this unary expession.
        /// </summary>
        public Expression Operand { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileUnaryExpression(this);
        }
    }
}