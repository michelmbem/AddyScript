using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements;


/// <summary>
/// A <b>case</b> label in a <b>switch</b> block.
/// </summary>
/// <remarks>
/// Initializes a new instance of CaseLabel.
/// </remarks>
/// <param name="address">The address of the statement that follows the case.</param>
/// <param name="value">The value that follows the 'case' keyword in the script.</param>
public class CaseLabel(int address, DataItem value) : Label(address)
{
    /// <summary>
    /// The value that follows the <b>case</b> keyword in the script.
    /// </summary>
    public DataItem Value => value;

    /// <summary>
    /// Gets the implicit name of a <see cref="CaseLabel"/> given its <see cref="Value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Value"/> of a <see cref="CaseLabel"/></param>
    /// <returns>A <see cref="string"/></returns>
    public static string GetLabelName(DataItem value) => "@case " + value.Class.ClassID switch
    {
        Runtime.OOP.ClassID.String => "'" + CodeGenerator.EscapedString(value.ToString(), false) + "'",
        _ => value.ToString(),
    };

    /// <summary>
    /// Gets the implicit name of the default <see cref="CaseLabel"/>.
    /// </summary>
    /// <returns>A <see cref="string"/></returns>
    public static string GetDefaultLabelName() => "@default";

    /// <summary>
    /// Gets the implicit name of this <see cref="CaseLabel"/>.
    /// </summary>
    /// <returns>A <see cref="string"/></returns>
    public string GetLabelName() => GetLabelName(Value);

    #region Overrides

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return (obj is CaseLabel label) && Value.Equals(label.Value);
    }

    #endregion
}