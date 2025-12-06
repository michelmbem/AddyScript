using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a reference to a list's or map's item.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ItemRef
    /// </remarks>
    /// <param name="owner">The collection to which this item belongs</param>
    /// <param name="index">The expression used to evaluate the index</param>
    public class ItemRef(Expression owner, Expression index) : Expression, IReference
    {

        /// <summary>
        /// >The collection to which this item belongs.
        /// </summary>
        public Expression Owner => owner;

        /// <summary>
        /// The expression used to evaluate the index.
        /// </summary>
        public Expression Index => index;

        /// <summary>
        /// Determines whether to stop null reference propagation or not.
        /// </summary>
        public bool Optional { get; set; } = false;

        /// <summary>
        /// A factory method to quickly create instances of <see cref="ItemRef"/>
        /// where the owner is always the keyword <i>this</i>.
        /// </summary>
        /// <param name="index">The expression used to evaluate the index</param>
        /// <returns>An <see cref="ItemRef"/></returns>
        public static Expression This(Expression index)
        {
            return new ItemRef(new SelfReference(), index);
        }

        /// <summary>
        /// Operates assignment to this reference.
        /// </summary>
        /// <param name="processor">The assignment processor to use</param>
        /// <param name="rValue">The value that should be assigned to this reference</param>
        public void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue)
        {
            processor.AssignToItem(this, rValue);
        }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateItemRef(this);
        }
    }
}