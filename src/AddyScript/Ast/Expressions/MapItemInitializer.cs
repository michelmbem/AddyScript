namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// An item as it appears in an map's initializer.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of MapItemInitializer
    /// </remarks>
    /// <param name="key">The item's key</param>
    /// <param name="value">The item's value</param>
    public class MapItemInitializer(Expression key, Expression value) : ScriptElement
    {

        /// <summary>
        /// The item's key.
        /// </summary>
        public Expression Key { get; private set; } = key;

        /// <summary>
        /// The item's value.
        /// </summary>
        public Expression Value { get; private set; } = value;

        #region Overrides

        public override int GetHashCode()
        {
            return Key == null ? base.GetHashCode() : Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is MapItemInitializer initializer) &&
                   (Key == null ? base.Equals(obj) : Key == initializer.Key);
        }

        #endregion
    }
}