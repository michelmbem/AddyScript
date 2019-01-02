using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>goto</b> statement.
    /// </summary>
    public class Goto : Statement
    {
        /// <summary>
        /// Initializes a new instance of Goto.
        /// </summary>
        /// <param name="labelName">The label following the goto</param>
        public Goto(string labelName)
        {
            LabelName = labelName;
        }

        /// <summary>
        /// The label following the goto.
        /// </summary>
        public string LabelName { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileGoto(this);
        }
    }
}