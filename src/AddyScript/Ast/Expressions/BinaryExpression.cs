using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a binary expression.
/// </summary>
public class BinaryExpression : Expression
{
    /// <summary>
    /// Initializes a new instance of BinaryExpression
    /// </summary>
    /// <param name="oper">A binary operator</param>
    /// <param name="left">The left operand</param>
    /// <param name="right">The right operand</param>
    public BinaryExpression(BinaryOperator oper, Expression left, Expression right)
    {
        Operator = oper;
        LeftOperand = left;
        RightOperand = right;
        SetLocation(left.Start, right.End);
    }

    /// <summary>
    /// Represents a binary operator of this expression
    /// </summary>
    public BinaryOperator Operator { get; private set; }

    /// <summary>
    /// Represents the left operand of this expression
    /// </summary>
    public Expression LeftOperand { get; private set; }

    /// <summary>
    /// Represents the right operand of this expression
    /// </summary>
    public Expression RightOperand { get; private set; }

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateBinaryExpression(this);
    }
}