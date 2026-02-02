using System.Collections.Generic;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a call to the superclass constructor.
/// </summary>
/// <remarks>
/// Initializes a new instance of ParentConstructorCall.
/// </remarks>
/// <param name="positionalArgs">The list of positional arguments passed to the constructor</param>
/// <param name="namedArgs">The collection of named arguments passed to the constructor</param>
public class ParentConstructorCall(Argument[] positionalArgs, Dictionary<string, Expression> namedArgs)
    : CallWithNamedArgs(positionalArgs, namedArgs)
{
    /// <summary>
    /// Initializes a new instance of ParentConstructorCall.
    /// </summary>
    /// <param name="positionalArgs">The list of positional arguments passed to the constructor</param>
    public ParentConstructorCall(params Expression[] positionalArgs)
        : this(ToArguments(positionalArgs), null) { }

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateParentConstructorCall(this);
    }
}