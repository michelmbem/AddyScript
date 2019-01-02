using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents an <b>if-then-else</b> statement.
    /// </summary>
    public class IfThenElse : Statement
    {
        /// <summary>
        /// Initializes a new instance of IfThenElse
        /// </summary>
        /// <param name="condition">The test expression</param>
        /// <param name="ifBlock">The statement to be executed if the test returns true</param>
        /// <param name="elseBlock">The statement to be executed if the test returns false</param>
        public IfThenElse(Expression condition, Statement ifBlock, Statement elseBlock)
        {
            Condition = condition;
            IfBlock = ifBlock;
            ElseBlock = elseBlock;
        }

        /// <summary>
        /// Initializes a new instance of IfThenElse
        /// </summary>
        /// <param name="condition">The test expression</param>
        /// <param name="ifBlock">The statement to be executed if the test returns true</param>
        public IfThenElse(Expression condition, Statement ifBlock)
        {
            Condition = condition;
            IfBlock = ifBlock;
        }

        /// <summary>
        /// The test expression
        /// </summary>
        public Expression Condition { get; private set; }

        /// <summary>
        /// The statement to be executed if the test returns true
        /// </summary>
        public Statement IfBlock { get; private set; }

        /// <summary>
        /// The statement to be executed if the test returns false
        /// </summary>
        public Statement ElseBlock { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileIfThenElse(this);
        }
    }
}