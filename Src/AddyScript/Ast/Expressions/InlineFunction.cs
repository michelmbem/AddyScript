using AddyScript.Ast.Statements;
using AddyScript.Compilers;
using AddyScript.Runtime;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an inline function's declaration or a lambda expression.
    /// </summary>
    public class InlineFunction : Expression
    {
        /// <summary>
        /// Initializes a new instance of InlineFunction
        /// </summary>
        /// <param name="function">The function's definition</param>
        public InlineFunction(Function function)
        {
            Function = function;
        }

        /// <summary>
        /// The function's definition
        /// </summary>
        public Function Function { get; private set; }

        /// <summary>
        /// Verifies that an inline function is a lambda expression or not.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the first statement is a <b>return</b> with an expression.
        /// <b>false</b> otherwise
        /// </returns>
        public bool IsLambda()
        {
            Statement[] statements = Function.Body.Statements;
            return statements.Length > 0 &&
                   statements[0] is Return &&
                   ((Return) statements[0]).Expression != null;
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileInlineFunction(this);
        }
    }
}