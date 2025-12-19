using System.Linq;
using AddyScript.Ast.Expressions;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Attribute's declaration, used to attach additional informations to an element in the code.
/// </summary>
/// <remarks>
/// Initializes a new instance of AttributeDecl.
/// </remarks>
/// <param name="name">The attribute's name</param>
/// <param name="setters">A list of setters for the properties of the declared attribute</param>
public class AttributeDecl(string name, params VariableSetter[] setters) : ScriptElement
{
    /// <summary>
    /// The name of the default attribute's field.
    /// </summary>
    public const string DEFAULT_FIELD_NAME = "value";

    /// <summary>
    /// The attribute's name.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// A list of setters for the properties of the declared attribute.
    /// </summary>
    public VariableSetter[] PropertySetters => setters;

    /// <summary>
    /// Gets a property initializer in an attribute by its name.
    /// </summary>
    /// <param name="propertyName">The name of the property to find</param>
    /// <returns>A reference to <see cref="VariableSetter"/></returns>
    public VariableSetter GetPropertySetter(string propertyName) =>
        PropertySetters?.FirstOrDefault(setter => setter.Name == propertyName);
}