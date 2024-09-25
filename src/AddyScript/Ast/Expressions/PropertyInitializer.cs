namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// A property as it appears in an object's initializer.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of PropertyInitializer
    /// </remarks>
    /// <param name="name">The property's name</param>
    /// <param name="expr">The value assigned to the property</param>
    public class PropertyInitializer(string name, Expression expr) : ScriptElement
    {

        /// <summary>
        /// The property's name.
        /// </summary>
        public string Name { get; private set; } = name;

        /// <summary>
        /// The value assigned to the property.
        /// </summary>
        public Expression Expression { get; private set; } = expr;

        #region Overrides

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is PropertyInitializer pi && Name == pi.Name;
        }

        #endregion
    }
}