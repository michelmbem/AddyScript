using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a set's initializer: a series of arguments between braces.
/// </summary>
/// <remarks>
/// Initializes a new instance of SetInitializer. Can be used as lvalue to deconstruct an object
/// and initialize multiple values at once with its properties.
/// </remarks>
/// <param name="items">The <see cref="Argument"/>s that are listed between the delimiters</param>
public class SetInitializer(params Argument[] items) : SequenceInitializer(items), IReference
{
    /// <summary>
    /// Operates assignment to this reference.
    /// Handles object destructuring.
    /// </summary>
    /// <param name="processor">The assignment processor to use</param>
    /// <param name="rValue">The value that should be assigned to this reference</param>
    public void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue)
    {
        processor.AssignToSet(this, rValue);
    }

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateSetInitializer(this);
    }
}