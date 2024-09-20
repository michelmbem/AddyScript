namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// A generic representation of type related expressions.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of TypeExpression
    /// </remarks>
    /// <param name="typeName">The type's name</param>
    public abstract class TypeExpression(string typeName) : Expression
    {

        /// <summary>
        /// The type's name.
        /// </summary>
        public string TypeName { get; private set; } = typeName;
    }
}