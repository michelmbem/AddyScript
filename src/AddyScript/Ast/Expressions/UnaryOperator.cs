namespace AddyScript.Ast.Expressions;


/// <summary>
/// Enumeration of unary operators.
/// </summary>
public enum UnaryOperator
{
    /// <summary>
    /// Null operator
    /// </summary>
    None,

    /// <summary>
    /// Unary +
    /// </summary>
    Plus,

    /// <summary>
    /// Unary -
    /// </summary>
    Minus,

    /// <summary>
    /// Negation
    /// </summary>
    Not,

    /// <summary>
    /// Bitwise negation
    /// </summary>
    BitwiseNot,

    /// <summary>
    /// Pre-increment
    /// </summary>
    PreIncrement,

    /// <summary>
    /// Post-increment
    /// </summary>
    PostIncrement,

    /// <summary>
    /// Pre-decrement
    /// </summary>
    PreDecrement,

    /// <summary>
    /// Post-decrement
    /// </summary>
    PostDecrement,

    /// <summary>
    /// Emptyness checking operator
    /// </summary>
    NotEmpty
}