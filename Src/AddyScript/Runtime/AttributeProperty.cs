using AddyScript.Runtime.Dynamics;


namespace AddyScript.Runtime
{
    /// <summary>
    /// Represents an attribute's property.
    /// </summary>
    public class AttributeProperty : ScriptElement
    {
        /// <summary>
        /// Initializes a new instance of AttributeProperty
        /// </summary>
        /// <param name="name">The property's name</param>
        /// <param name="value">The property's value</param>
        public AttributeProperty(string name, Dynamic value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// The property's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The property field's value.
        /// </summary>
        public Dynamic Value { get; private set; }

        #region Overrides

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is AttributeProperty) && Name == ((AttributeProperty) obj).Name;
        }

        #endregion
    }
}