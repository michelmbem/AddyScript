using System.Reflection;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents the way to call a native function.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ExternalFunctionCall
    /// </remarks>
    /// <param name="method">A wrapper around the target native function</param>
    /// <param name="arguments">The arguments passed to the target function</param>
    public class ExternalFunctionCall(MethodInfo method, params Expression[] arguments) : Call(arguments)
    {

        /// <summary>
        /// Represents a wrapper around the target native function.
        /// </summary>
        public MethodInfo Method { get; private set; } = method;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateExternalFunctionCall(this);
        }
    }
}