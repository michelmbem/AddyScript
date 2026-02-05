using System;
using System.Collections.Generic;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a call to an anonymous function.
/// </summary>
/// <remarks>
/// Initializes a new instance of AnonymousCall.
/// </remarks>
/// <param name="called">The expression that produces the function being called</param>
/// <param name="positionalArgs">The list of positional arguments passed to the function</param>
/// <param name="namedArgs">The collection of named arguments passed to the function</param>
public class AnonymousCall(Expression called, Argument[] positionalArgs, Dictionary<string, Expression> namedArgs)
    : FunctionCall($"__anonymous_{Environment.TickCount}", positionalArgs, namedArgs)
{
    /// <summary>
    /// Initializes a new instance of AnonymousCall.
    /// </summary>
    /// <param name="called">The expression that produces the function being called</param>
    /// <param name="positionalArgs">The list of positional arguments passed to the function</param>
    public AnonymousCall(Expression called, params Expression[] positionalArgs)
        : this(called, ToArguments(positionalArgs), null) { }

    /// <summary>
    /// Represents the expression that produces the function being called.
    /// </summary>
    public Expression Called => called;

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateAnonymousCall(this);
    }
}