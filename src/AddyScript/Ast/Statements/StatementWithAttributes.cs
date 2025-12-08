namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// The base class of all statements that can be decorated with attributes.
    /// </summary>
    public abstract class StatementWithAttributes : Statement
    {
        /// <summary>
        /// The statement's attributes.
        /// </summary>
        public AttributeDecl[] Attributes { get; set; }

        /// <summary>
        /// Gets an attribute by its name.
        /// </summary>
        /// <param name="name">The name of the target attribute</param>
        /// <returns><see cref="AttributeDecl"/></returns>
        public AttributeDecl GetAttribute(string name)
        {
            if (Attributes == null) return null;
            
            foreach (AttributeDecl attribute in Attributes)
                if (attribute.Name == name) return attribute;

            return null;
        }
    }
}