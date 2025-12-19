using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents the declaration of a set of constants.
/// </summary>
/// <remarks>
/// Initializes a new instance of ConstantDecl
/// </remarks>
/// <param name="setters">The list of (name, value) pairs used to define constants.</param>
public class ConstantDecl(params VariableSetter[] setters) : VariableDecl(setters)
{
    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateConstantDecl(this);
    }
}