using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents a <b>throw</b> statement.
/// </summary>
/// <remarks>
/// Initializes a new instance of Throw
/// </remarks>
/// <param name="expr">The exception to be thrown</param>
public class Throw(Expression expr) : StatementWithExpression(expr)
{
    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateThrow(this);
    }
}