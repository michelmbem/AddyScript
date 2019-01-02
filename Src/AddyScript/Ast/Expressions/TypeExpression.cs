namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// A generic representation of type related expressions.
    /// </summary>
    public abstract class TypeExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of TypeExpression
        /// </summary>
        /// <param name="typeName">The type's name</param>
        protected TypeExpression(string typeName)
        {
            TypeName = typeName;
        }

        /// <summary>
        /// The type's name.
        /// </summary>
        public string TypeName { get; private set; }
    }
}