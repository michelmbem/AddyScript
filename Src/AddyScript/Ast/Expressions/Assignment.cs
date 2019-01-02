using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an assignment.
    /// </summary>
    public class Assignment : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of Assignment
        /// </summary>
        /// <param name="oper">The binary operator combined to the equal sign for this assignment</param>
        /// <param name="lValue">The variable that may be assigned</param>
        /// <param name="rValue">The value to assign to the variable</param>
        public Assignment(BinaryOperator oper, Expression lValue, Expression rValue)
            : base(oper, lValue, rValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of Assignment
        /// </summary>
        /// <param name="lValue">The variable that may be assigned</param>
        /// <param name="rValue">The value to assign to the variable</param>
        public Assignment(Expression lValue, Expression rValue)
            : this(BinaryOperator.None, lValue, rValue)
        {
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileAssignment(this);
        }
    }
}