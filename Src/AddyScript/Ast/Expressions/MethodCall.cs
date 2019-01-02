using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to a method.
    /// </summary>
    public class MethodCall : FunctionCall
    {
        /// <summary>
        /// Initializes a new instance of MethodCall
        /// </summary>
        /// <param name="caller">Holds the value of 'this'</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="arguments">The list of arguments passed to the method</param>
        public MethodCall(Expression caller, string methodName, params Expression[] arguments)
            : base(methodName, arguments)
        {
            Caller = caller;
        }

        /// <summary>
        /// Holds the value of 'this'.
        /// </summary>
        public Expression Caller { get; private set; }

        /// <summary>
        /// A factory method to quickly create instances of <see cref="MethodCall"/>
        /// where the caller is always <i>this</i>.
        /// </summary>
        /// <param name="methodName">Ths method's name</param>
        /// <param name="arguments">The list of arguments passed to the method</param>
        /// <returns>A <see cref="MethodCall"/></returns>
        public static MethodCall OfThis(string methodName, params Expression[] arguments)
        {
            return new MethodCall(new ThisReference(), methodName, arguments);
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileMethodCall(this);
        }
    }
}