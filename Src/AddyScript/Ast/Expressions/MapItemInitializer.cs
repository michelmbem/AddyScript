namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// An item as it appears in an map's initializer.
    /// </summary>
    public class MapItemInitializer : ScriptElement
    {
        /// <summary>
        /// Initializes a new instance of MapItemInitializer
        /// </summary>
        /// <param name="key">The item's key</param>
        /// <param name="value">The item's value</param>
        public MapItemInitializer(Expression key, Expression value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// The item's key.
        /// </summary>
        public Expression Key { get; private set; }

        /// <summary>
        /// The item's value.
        /// </summary>
        public Expression Value { get; private set; }

        #region Overrides

        public override int GetHashCode()
        {
            return Key == null ? base.GetHashCode() : Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is MapItemInitializer) &&
                   (Key == null ? base.Equals(obj) : Key == ((MapItemInitializer) obj).Key);
        }

        #endregion
    }
}