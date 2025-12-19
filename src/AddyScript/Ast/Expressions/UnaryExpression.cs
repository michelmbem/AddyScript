using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a unary expression.
/// </summary>
/// <remarks>
/// Initializes a new instance of UnaryExpression
/// </remarks>
/// <param name="oper">A unary operator</param>
/// <param name="expr">The operand</param>
public class UnaryExpression(UnaryOperator oper, Expression expr) : Expression
{
    /// <summary>
    /// Represents the operator of this unary expession.
    /// </summary>
    public UnaryOperator Operator => oper;

    /// <summary>
    /// Represents the operand of this unary expession.
    /// </summary>
    public Expression Operand => expr;

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateUnaryExpression(this);
    }
}