using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents a 'return' statement.
/// </summary>
public class Return : StatementWithExpression
{
    /// <summary>
    /// Initializes a new instance of Return
    /// </summary>
    public Return() : base(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of Return
    /// </summary>
    /// <param name="expression">The returned expression</param>
    public Return(Expression expression) : base(expression)
    {
    }

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateReturn(this);
    }
}