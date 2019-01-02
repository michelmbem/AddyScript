namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// A property as it appears in an object's initializer.
    /// </summary>
    public class PropertyInitializer : ScriptElement
    {
        /// <summary>
        /// Initializes a new instance of PropertyInitializer
        /// </summary>
        /// <param name="name">The property's name</param>
        /// <param name="expr">The value assigned to the property</param>
        public PropertyInitializer(string name, Expression expr)
        {
            Name = name;
            Expression = expr;
        }

        /// <summary>
        /// The property's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The value assigned to the property.
        /// </summary>
        public Expression Expression { get; private set; }

        #region Overrides

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PropertyInitializer)) return false;
            return Name == ((PropertyInitializer) obj).Name;
        }

        #endregion
    }
}