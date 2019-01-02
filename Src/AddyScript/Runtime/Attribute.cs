namespace AddyScript.Runtime
{
    /// <summary>
    /// Attach additional information to a code element.
    /// </summary>
    public class Attribute : ScriptElement
    {
        /// <summary>
        /// Initializes a new instance of Attribute.
        /// </summary>
        /// <param name="name">The name of the attribute</param>
        /// <param name="properties">The properties of the attribute</param>
        public Attribute(string name, params AttributeProperty[] properties)
        {
            Name = name;
            Properties = properties;
        }

        /// <summary>
        /// The name of this attribute.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The properties of this attribute.
        /// </summary>
        public AttributeProperty[] Properties { get; private set; }

        /// <summary>
        /// Gets a property in an attribute by its name.
        /// </summary>
        /// <param name="propertyName">The name of the property to find</param>
        /// <returns>A reference to <see cref="AttributeProperty"/></returns>
        public AttributeProperty GetProperty(string propertyName)
        {
            foreach (AttributeProperty property in Properties)
                if (property.Name == propertyName)
                    return property;

            return null;
        }

        #region Overrides

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is Attribute) && Name == ((Attribute) obj).Name;
        }

        #endregion
    }
}