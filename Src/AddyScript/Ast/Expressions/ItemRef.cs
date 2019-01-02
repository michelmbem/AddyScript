using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a reference to a list's or map's item.
    /// </summary>
    public class ItemRef : Expression
    {
        /// <summary>
        /// Initializes a new instance of ItemRef
        /// </summary>
        /// <param name="owner">The collection to which this item belongs</param>
        /// <param name="index">The expression used to evaluate the index</param>
        public ItemRef(Expression owner, Expression index)
        {
            Owner = owner;
            Index = index;
        }

        /// <summary>
        /// >The collection to which this item belongs.
        /// </summary>
        public Expression Owner { get; private set; }

        /// <summary>
        /// The expression used to evaluate the index.
        /// </summary>
        public Expression Index { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileItemRef(this);
        }
    }
}