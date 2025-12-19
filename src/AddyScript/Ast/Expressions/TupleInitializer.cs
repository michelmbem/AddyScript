using System;
using AddyScript.Properties;
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
        DataItem[] rValueItems = rValue.AsArray;
        
        if (rValueItems.Length != Items.Length)
            throw new InvalidOperationException(Resources.ListLengthMismatch);

        for (int i = 0; i < Items.Length; ++i)
        {
            var item = Items[i];
            
            if (item.Spread || item.Expression is not IReference reference)
                throw new InvalidOperationException(Resources.NotAReference);
            
            reference.AcceptAssignmentProcessor(processor, rValueItems[i]);
        }
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