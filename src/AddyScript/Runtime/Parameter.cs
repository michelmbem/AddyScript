using AddyScript.Runtime.DataItems;


namespace AddyScript.Runtime;


/// <summary>
/// Represents a function's parameter.
/// </summary>
/// <remarks>
/// Initializes a new instance of Parameter.
/// </remarks>
/// <param name="name">The parameter's name.</param>
/// <param name="byRef">Determines if the parameter is passed by reference or not</param>
/// <param name="vaList">Determines if the parameter is a variably sized arguments list or not</param>
/// <param name="defaultValue">The default value for this parameter if any</param>
/// <param name="canBeEmpty">Tells if empty values are allowed for this parameter or not</param>
public class Parameter(string name, bool byRef, bool vaList, DataItem defaultValue = null, bool canBeEmpty = true)
{
    /// <summary>
    /// Initializes a new instance of Parameter.
    /// </summary>
    /// <param name="name">The parameter's name.</param>
    /// <param name="defaultValue">The default value for this parameter if any</param>
    /// <param name="canBeEmpty">Tells if empty values are allowed for this parameter or not</param>
    public Parameter(string name, DataItem defaultValue = null, bool canBeEmpty = true)
        : this(name, false, false, defaultValue, canBeEmpty)
    {
    }

    /// <summary>
    /// The parameter's name.
    /// </summary>
    public string Name { get; private set; } = name;

    /// <summary>
    /// Determines if the parameter is passed by reference or not.
    /// </summary>
    public bool ByRef { get; private set; } = byRef;

    /// <summary>
    /// Determines if the parameter is a variably sized arguments list or not.
    /// </summary>
    public bool VaList { get; private set; } = vaList;

    /// <summary>
    /// The parameter's default value if any.
    /// </summary>
    public DataItem DefaultValue { get; private set; } = defaultValue;

    /// <summary>
    /// Tells if empty values are allowed for this parameter or not.
    /// </summary>
    public bool CanBeEmpty => canBeEmpty;

    /// <summary>
    /// Gets if this parameter is optional or not.
    /// </summary>
    public bool Optional => VaList || DefaultValue != null;

    /// <summary>
    /// The parameter's attributes.
    /// </summary>
    public DataItem[] Attributes { get; set; }
}