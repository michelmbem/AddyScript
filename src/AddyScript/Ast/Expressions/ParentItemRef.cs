using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a reference to the original implementation of an overriden indexer.<br>
    /// Looks in the code like: <code>super[index]</code>.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ParentIndexerRef
    /// </remarks>
    /// <param name="index">The expression used to evaluate the index</param>
    public class ParentIndexerRef(Expression index) : Expression
    {

        /// <summary>
        /// The expression used to evaluate the index.
        /// </summary>
        public Expression Index { get; private set; } = index;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateParentIndexerRef(this);
        }
    }
}