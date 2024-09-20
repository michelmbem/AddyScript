using System;
using System.Collections.Generic;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to an anonymous function.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of AnonymousCall
    /// </remarks>
    /// <param name="callee">The expression to be used to get a function</param>
    /// <param name="positionalArgs">The list of positional arguments passed to the function</param>
    /// <param name="namedArgs">The collection of named arguments passed to the function</param>
    public class AnonymousCall(Expression callee, Expression[] positionalArgs, Dictionary<string, Expression> namedArgs)
        : FunctionCall($"__anonymous_{Environment.TickCount}", positionalArgs, namedArgs)
    {

        /// <summary>
        /// Represents the expression to be used to get a function.
        /// </summary>
        public Expression Callee { get; private set; } = callee;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateAnonymousCall(this);
        }
    }
}