using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a call to the superclass constructor.
    /// </summary>
    public class ParentConstructorCall : Statement
    {
        /// <summary>
        /// Initializes a new instance of ParentConstructorCall.
        /// </summary>
        /// <param name="args"></param>
        public ParentConstructorCall(params Expression[] args)
        {
            Arguments = args;
        }

        /// <summary>
        /// Represents the list of arguments passed to the constructor
        /// </summary>
        public Expression[] Arguments { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileParentConstructorCall(this);
        }
    }
}