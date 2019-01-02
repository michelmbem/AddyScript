using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>switch</b> block.
    /// </summary>
    public class SwitchBlock : Block
    {
        /// <summary>
        /// Initializes a new instance of SwitchBlock
        /// </summary>
        /// <param name="expr">The test expression.</param>
        /// <param name="defCase">The default case label</param>
        /// <param name="stmts">The switch statements.</param>
        /// <param name="cases">The case labels</param>
        public SwitchBlock(Expression expr, CaseLabel[] cases, int defCase, Statement[] stmts)
            : base(stmts)
        {
            Expression = expr;
            Cases = cases;
            DefaultCase = defCase;
        }

        /// <summary>
        /// The test expression.
        /// </summary>
        public Expression Expression { get; private set; }

        /// <summary>
        /// The case labels.
        /// </summary>
        public CaseLabel[] Cases { get; private set; }

        /// <summary>
        /// The default case label.
        /// </summary>
        public int DefaultCase { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileSwitchBlock(this);
        }
    }
}