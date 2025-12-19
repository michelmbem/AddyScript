using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a reference to the original implementation of an overriden indexer.<br>
/// Looks in the code like: <code>super[index]</code>.
/// </summary>
/// <remarks>
/// Initializes a new instance of ParentIndexerRef
/// </remarks>
/// <param name="index">The expression used to evaluate the index</param>
public class ParentIndexerRef(Expression index) : Expression, IReference
{
    /// <summary>
    /// The expression used to evaluate the index.
    /// </summary>
    public Expression Index => index;


    /// <summary>
    /// Operates assignment to this reference.
    /// </summary>
    /// <param name="processor">The assignment processor to use</param>
    /// <param name="rValue">The value that should be assigned to this reference</param>
    public void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue)
    {
        processor.AssignToParentItem(this, rValue);
    }

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateParentIndexerRef(this);
    }
}