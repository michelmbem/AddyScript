using AddyScript.Translators;
using AddyScript.Runtime;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents the way to call a built-in function.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of InnerFunctionCall
    /// </remarks>
    /// <param name="function">The inner function to call</param>
    /// <param name="arguments">The list of arguments</param>
    public class InnerFunctionCall(InnerFunction function, params Expression[] arguments)
        : Call(arguments)
    {

        /// <summary>
        /// Represents the inner function to call.
        /// </summary>
        public InnerFunction Function { get; private set; } = function;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateInnerFunctionCall(this);
        }
    }
}