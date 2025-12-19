using System.Collections.Generic;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a call to a method.
/// </summary>
/// <remarks>
/// Initializes a new instance of MethodCall
/// </remarks>
/// <param name="target">Holds the value of 'this'</param>
/// <param name="methodName">The name of the method to invoke</param>
/// <param name="positionalArgs">The list of positional arguments passed to the method</param>
/// <param name="namedArgs">The collection of named arguments passed to the method</param>
public class MethodCall(Expression target, string methodName,
                        Argument[] positionalArgs,
                        Dictionary<string, Expression> namedArgs)
    : FunctionCall(methodName, positionalArgs, namedArgs)
{
    /// <summary>
    /// Initializes a new instance of MethodCall
    /// </summary>
    /// <param name="target">Holds the value of 'this'</param>
    /// <param name="methodName">The name of the method to invoke</param>
    /// <param name="arguments">The list of arguments passed to the method</param>
    public MethodCall(Expression target, string methodName, params Expression[] arguments)
        : this(target, methodName, ToArguments(arguments), null)
    {
    }

    /// <summary>
    /// Holds the value of 'this'.
    /// </summary>
    public Expression Target => target;

    /// <summary>
    /// Determines whether to stop null reference propagation or not.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// A factory method to quickly create instances of <see cref="MethodCall"/>
    /// where the target is always <i>this</i>.
    /// </summary>
    /// <param name="methodName">Ths method's name</param>
    /// <param name="arguments">The list of arguments passed to the method</param>
    /// <returns>A <see cref="MethodCall"/></returns>
    public static MethodCall OfSelf(string methodName, params Expression[] arguments) =>
        new(new SelfReference(), methodName, arguments);

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateMethodCall(this);
    }
}