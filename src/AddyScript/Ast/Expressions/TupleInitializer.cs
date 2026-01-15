using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a tuples's initializer: a set of item initializer into parentheses.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="TupleInitializer"/>.
/// Can be used as lvalue to initialize multiple values at once.
/// </remarks>
/// <param name="items">The <see cref="Argument"/>s that are listed between the delimiters</param>
public class TupleInitializer(params Argument[] items) : SequenceInitializer(items), IReference
{
    /// <summary>
    /// Operates assignment to this reference.
    /// </summary>
    /// <param name="processor">The assignment processor to use</param>
    /// <param name="rValue">The value that should be assigned to this reference</param>
    public void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue)
    {
        processor.AssignToTuple(this, rValue);
    }

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateTupleInitializer(this);
    }
}