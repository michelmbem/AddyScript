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
    /// The implicit name of the default <see cref="CaseLabel"/>.
    /// </summary>
    public const string DEFAULT_LABEL_NAME = "@default";

    /// <summary>
    /// The value that follows the <b>case</b> keyword in the script.
    /// </summary>
    public DataItem Value => value;

    /// <summary>
    /// Gets the implicit name of this <see cref="CaseLabel"/>.
    /// </summary>
    /// <value>A <see cref="string"/>
    /// </value>
    public string LabelName => GetLabelName(Value);

    /// <summary>
    /// Gets the implicit name of a <see cref="CaseLabel"/> given its <see cref="Value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Value"/> of a <see cref="CaseLabel"/></param>
    /// <returns>A <see cref="string"/></returns>
    public static string GetLabelName(DataItem value) => "@case " + value switch
    {
        String => "'" + CodeGenerator.EscapedString(value.ToString(), false) + "'",
        _ => value.ToString(),
    };

    #region Overrides

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object obj) => obj is CaseLabel label && Equals(Value, label.Value);

    #endregion
}