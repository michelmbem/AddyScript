using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>for</b> statement
    /// </summary>
    public class ForLoop : Statement
    {
        /// <summary>
        /// Initializes a new instance of ForLoop
        /// </summary>
        /// <param name="initializers">The set of statements that run before enterring the loop</param>
        /// <param name="guard">The condition</param>
        /// <param name="updaters">The set of statements that run at the end of each iteration</param>
        /// <param name="body">The body of the loop</param>
        public ForLoop(Statement[] initializers, Expression guard, Expression[] updaters, Statement body)
        {
            Initializers = initializers;
            Guard = guard;
            Updaters = updaters;
            Body = body;
        }

        /// <summary>
        /// Represents the set of statements that run before enterring the loop.
        /// </summary>
        public Statement[] Initializers { get; private set; }

        /// <summary>
        /// Represents the condition of the loop.
        /// </summary>
        public Expression Guard { get; private set; }

        /// <summary>
        /// Represents the set of statements that run at the end of each iteration.
        /// </summary>
        public Expression[] Updaters { get; private set; }

        /// <summary>
        /// Represents the body of the loop.
        /// </summary>
        public Statement Body { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileForLoop(this);
        }
    }
}