using System.Collections.Generic;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a call to the original implementation of an overriden method.<br>
/// Looks in the code like: <code>super::doSomeStuff(/*...*/)</code>.
/// </summary>
/// <remarks>
/// Initializes a new instance of ParentMethodCall.
/// </remarks>
/// <param name="methodName">The name of the method to invoke</param>
/// <param name="positionalArgs">The list of positional arguments passed to the method</param>
/// <param name="namedArgs">The collection of named arguments passed to the method</param>
public class ParentMethodCall(string methodName, Argument[] positionalArgs, Dictionary<string, Expression> namedArgs)
    : FunctionCall(methodName, positionalArgs, namedArgs)
{
    /// <summary>
    /// Initializes a new instance of ParentMethodCall.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke</param>
    /// <param name="positionalArgs">The list of positional arguments passed to the method</param>
    public ParentMethodCall(string methodName, params Expression[] positionalArgs)
        : this(methodName, ToArguments(positionalArgs), null)
    {
    }

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateParentMethodCall(this);
    }
}