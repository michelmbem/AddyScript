using System.Collections.Generic;

using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a call to a constructor.
/// </summary>
/// <remarks>
/// Initializes a new instance of ConstructorCall.
/// </remarks>
/// <param name="name">The name of the class to instanciate</param>
/// <param name="positionalArgs">The list of positional arguments passed to the constructor</param>
/// <param name="namedArgs">The collection of named arguments passed to the constructor</param>
/// <param name="setters">A set of property setters for the new object</param>
public class ConstructorCall(QualifiedName name, Argument[] positionalArgs,
                             Dictionary<string, Expression> namedArgs,
                             VariableSetter[] setters)

    : StaticMethodCall(name, positionalArgs, namedArgs)
{
    /// <summary>
    /// Initializes a new instance of ConstructorCall.
    /// </summary>
    /// <param name="name">The name of the class to instanciate</param>
    /// <param name="positionalArgs">The list of positional arguments passed to the constructor</param>
    public ConstructorCall(QualifiedName name, params Expression[] positionalArgs)
        : this(name, ToArguments(positionalArgs), null, null) { }

    /// <summary>
    /// A set of property setters for the newly created object.
    /// </summary>
    public VariableSetter[] PropertySetters => setters;

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateConstructorCall(this);
    }
}