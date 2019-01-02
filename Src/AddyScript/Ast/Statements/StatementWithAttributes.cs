using AddyScript.Runtime;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// The base class of all statements that can be decorated with attributes.
    /// </summary>
    public abstract class StatementWithAttributes : Statement
    {
        /// <summary>
        /// The node's attributes.
        /// </summary>
        public Attribute[] Attributes { get; set; }

        /// <summary>
        /// Gets an attribute by its name.
        /// </summary>
        /// <param name="name">The name of an annotation</param>
        /// <returns><see cref="Attribute"/></returns>
        public Attribute GetAttribute(string name)
        {
            if (Attributes != null)
                foreach (Attribute attribute in Attributes)
                    if (attribute.Name == name)
                        return attribute;

            return null;
        }
    }
}