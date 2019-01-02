using AddyScript.Compilers;
using AddyScript.Runtime.Dynamics;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a literal value.
    /// </summary>
    public class Literal : Expression
    {
        /// <summary>
        /// Initializes a new instance of Literal.
        /// </summary>
        /// <param name="value">The literal value wrapped by this instance</param>
        public Literal(Dynamic value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of Literal.
        /// </summary>
        public Literal()
            : this(Void.Value)
        {
        }

        /// <summary>
        /// The literal value wrapped by this instance.
        /// </summary>
        public Dynamic Value { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileLiteral(this);
        }
    }
}