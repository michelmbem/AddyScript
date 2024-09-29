using System;
using System.Collections.Generic;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a call to an anonymous function.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of AnonymousCall.
    /// </remarks>
    /// <param name="callee">The expression to be used to get a function</param>
    /// <param name="positionalArgs">The list of positional arguments passed to the function</param>
    /// <param name="namedArgs">The collection of named arguments passed to the function</param>
    public class AnonymousCall(Expression callee, ListItem[] positionalArgs, Dictionary<string, Expression> namedArgs)
        : FunctionCall($"__anonymous_{Environment.TickCount}", positionalArgs, namedArgs)
    {
        /// <summary>
        /// Initializes a new instance of AnonymousCall.
        /// </summary>
        /// <param name="callee">The expression to be used to get a function</param>
        /// <param name="positionalArgs">The list of positional arguments passed to the function</param>
        public AnonymousCall(Expression callee, params Expression[] positionalArgs)
            : this(callee, ToListItems(positionalArgs), null)
        {
        }

        /// <summary>
        /// Represents the expression to be used to get a function.
        /// </summary>
        public Expression Callee { get; private set; } = callee;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateAnonymousCall(this);
        }
    }
}