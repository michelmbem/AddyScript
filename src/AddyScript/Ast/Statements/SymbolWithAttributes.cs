using System.Linq;


namespace AddyScript.Ast.Statements;


/// <summary>
/// The base class of all script elements that can be decorated with attributes whitout being statements.
/// </summary>
public abstract class SymbolWithAttributes : ScriptElement
{
    /// <summary>
    /// The element's attributes.
    /// </summary>
    public AttributeDecl[] Attributes { get; set; }

    /// <summary>
    /// Gets an attribute by its name.
    /// </summary>
    /// <param name="name">The name of an attribute</param>
    /// <returns><see cref="AttributeDecl"/></returns>
    public AttributeDecl GetAttribute(string name) =>
        Attributes?.FirstOrDefault(attribute => attribute.Name == name);
}