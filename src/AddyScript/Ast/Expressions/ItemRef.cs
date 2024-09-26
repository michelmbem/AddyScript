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
    public class ItemRef(Expression owner, Expression index) : Expression
    {

        /// <summary>
        /// >The collection to which this item belongs.
        /// </summary>
        public Expression Owner { get; private set; } = owner;

        /// <summary>
        /// The expression used to evaluate the index.
        /// </summary>
        public Expression Index { get; private set; } = index;

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
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateItemRef(this);
        }
    }
}