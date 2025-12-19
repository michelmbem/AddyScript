namespace AddyScript.Ast.Expressions;


/// <summary>
/// An item as it appears in an map initializer.
/// </summary>
/// <remarks>
/// Initializes a new instance of MapEntry
/// </remarks>
/// <param name="key">The item's key</param>
/// <param name="value">The item's value</param>
public class MapEntry(Expression key, Expression value) : ScriptElement
{
    /// <summary>
    /// The item's key.
    /// </summary>
    public Expression Key => key;

    /// <summary>
    /// The item's value.
    /// </summary>
    public Expression Value => value;

    #region Overrides

    public override int GetHashCode() =>
        Key == null ? base.GetHashCode() : Key.GetHashCode();

    public override bool Equals(object obj) =>
        obj is MapEntry entry && (Key == null ? base.Equals(obj) : Key == entry.Key);

    #endregion
}