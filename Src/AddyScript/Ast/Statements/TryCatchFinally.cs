using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>try-catch-finally</b> statement.
    /// </summary>
    public class TryCatchFinally : Statement
    {
        /// <summary>
        /// Initializes a new instance of TryCatchFinally.
        /// </summary>
        /// <param name="tryBlock">The try block</param>
        /// <param name="exception">The name of the caught exception</param>
        /// <param name="catchBlock">The catch block</param>
        /// <param name="finallyBlock">The finally block</param>
        public TryCatchFinally(Block tryBlock, string exception, Block catchBlock, Block finallyBlock)
        {
            TryBlock = tryBlock;
            ExceptionName = exception;
            CatchBlock = catchBlock;
            FinallyBlock = finallyBlock;
        }

        /// <summary>
        /// The try block.
        /// </summary>
        public Block TryBlock { get; private set; }

        /// <summary>
        /// The name of the caught exception.
        /// </summary>
        public string ExceptionName { get; private set; }

        /// <summary>
        /// The catch block. Only one is allowed.
        /// </summary>
        public Block CatchBlock { get; private set; }

        /// <summary>
        /// The optional finally block.
        /// </summary>
        public Block FinallyBlock { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileTryCatchFinally(this);
        }
    }
}