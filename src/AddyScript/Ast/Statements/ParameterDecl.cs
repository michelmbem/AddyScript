using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents the declaration of a function's parameter.
/// </summary>
/// <remarks>
/// Initializes a new instance of ParameterDecl.
/// </remarks>
/// <param name="name">The parameter's name.</param>
/// <param name="byRef">Determines if the parameter is passed by reference or not</param>
/// <param name="vaList">Determines if the parameter is a variably sized arguments list or not</param>
/// <param name="defaultValue">The default value for this parameter if any</param>
/// <param name="canBeEmpty">Tells if empty values are allowed for this parameter or not</param>
public class ParameterDecl(string name, bool byRef, bool vaList,
                           DataItem defaultValue, bool canBeEmpty)
    : SymbolWithAttributes
{
    /// <summary>
    /// The parameter's name.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Determines if the parameter is passed by reference or not.
    /// </summary>
    public bool ByRef => byRef;

    /// <summary>
    /// Determines if the parameter is a variably sized arguments list or not.
    /// </summary>
    public bool VaList => vaList;

    /// <summary>
    /// The parameter's default value if any.
    /// </summary>
    public DataItem DefaultValue => defaultValue;

    /// <summary>
    /// Tells if empty values are allowed for this parameter or not.
    /// </summary>
    public bool CanBeEmpty => canBeEmpty;

    /// <summary>
    /// Create a <see cref="Parameter"/> from this instance.
    /// </summary>
    /// <returns>A <see cref="Parameter"/></returns>
    public Parameter ToParameter() => new (Name, ByRef, VaList, DefaultValue, CanBeEmpty);

    #region Overrides

    public override int GetHashCode() => Name.GetHashCode();

    public override bool Equals(object obj) => obj is ParameterDecl pd && Name == pd.Name;

    #endregion
}