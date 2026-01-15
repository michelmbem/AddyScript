using System.Linq;
using AddyScript.Ast.Expressions;


namespace AddyScript.Ast.Statements;


/// <summary>
/// The declaration of an attribute, used to attach additional information to some symbols in the code.
/// </summary>
/// <remarks>
/// Initializes a new instance of AttributeDecl.
/// </remarks>
/// <param name="name">The name of this attribute</param>
/// <param name="setters">A list of (field name, field value) pairs</param>
public class AttributeDecl(string name, params VariableSetter[] setters) : ScriptElement
{
    /// <summary>
    /// The name of the default field.
    /// </summary>
    public const string DEFAULT_FIELD_NAME = "value";

    /// <summary>
    /// The name of this attribute.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// The list of (field name, field value) pairs supplied for this attribute.
    /// </summary>
    public VariableSetter[] Fields => setters;

    /// <summary>
    /// Gets a <see cref="VariableSetter"/> in an attribute given a field name.
    /// </summary>
    /// <param name="fieldName">The name of the field to find</param>
    /// <returns>A reference to <see cref="VariableSetter"/></returns>
    public VariableSetter GetField(string fieldName) =>
        Fields?.FirstOrDefault(field => field.Name == fieldName);
}