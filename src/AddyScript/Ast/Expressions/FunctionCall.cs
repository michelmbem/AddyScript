using System.Collections.Generic;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to a function.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of FunctionCall
    /// </remarks>
    /// <param name="functionName">The name of the function to invoke</param>
    /// <param name="positionalArgs">The list of positional arguments passed to the function</param>
    /// <param name="namedArgs">The collection of named arguments passed to the function</param>
    public class FunctionCall(string functionName, ListItem[] positionalArgs, Dictionary<string, Expression> namedArgs)
        : CallWithNamedArgs(positionalArgs, namedArgs)
    {

        /// <summary>
        /// Initializes a new instance of FunctionCall
        /// </summary>
        /// <param name="functionName">The name of the function to invoke</param>
        /// <param name="arguments">The list of arguments passed to the function</param>
        public FunctionCall(string functionName, params Expression[] arguments)
            : this(functionName, ToListItems(arguments), null)
        {
        }

        /// <summary>
        /// Represents the name of the function to invoke.
        /// </summary>
        public string FunctionName { get; private set; } = functionName;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateFunctionCall(this);
        }
    }
}