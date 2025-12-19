using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents an object initializer: a set of field initializers into braces.
/// </summary>
/// <remarks>
/// Initializes a new instance of ObjectInitializer
/// </remarks>
/// <param name="setters">A list of setters for the object's fields</param>
public class ObjectInitializer(params VariableSetter[] setters) : Expression
{
    /// <summary>
    /// A list of setters for the object's fields.
    /// </summary>
    public VariableSetter[] PropertySetters => setters;

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateObjectInitializer(this);
    }
}