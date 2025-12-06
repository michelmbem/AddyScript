using AddyScript.Ast.Expressions;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Attribute's declaration, used to attach additional informations to an element in the code.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of AttributeDecl.
    /// </remarks>
    /// <param name="name">The attribute's name</param>
    /// <param name="initializers">A set of initializers for the properties of the declared attribute</param>
    public class AttributeDecl(string name, params PropertyInitializer[] initializers) : ScriptElement
    {
        /// <summary>
        /// The name of the default attribute's field.
        /// </summary>
        public const string DEFAULT_FIELD_NAME = "value";

        /// <summary>
        /// The attribute's name.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// A set of initializers for the properties of the declared attribute.
        /// </summary>
        public PropertyInitializer[] PropertyInitializers => initializers;

        /// <summary>
        /// Gets a property initializer in an attribute by its name.
        /// </summary>
        /// <param name="propertyName">The name of the property to find</param>
        /// <returns>A reference to <see cref="PropertyInitializer"/></returns>
        public PropertyInitializer GetPropertyInitializer(string propertyName)
        {
            if (PropertyInitializers != null)
                foreach (PropertyInitializer property in PropertyInitializers)
                    if (property.Name == propertyName)
                        return property;

            return null;
        }
    }
}