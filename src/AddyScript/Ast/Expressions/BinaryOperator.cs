namespace AddyScript.Ast.Expressions;


/// <summary>
/// Enumeration of binary operators.
/// </summary>
public enum BinaryOperator
{
    /// <summary>
    /// Null operator
    /// </summary>
    None,

    /// <summary>
    /// Addition
    /// </summary>
    Plus,

    /// <summary>
    /// Subtraction
    /// </summary>
    Minus,

    /// <summary>
    /// Multiplication
    /// </summary>
    Times,

    /// <summary>
    /// Division
    /// </summary>
    Divide,

    /// <summary>
    /// Modulo
    /// </summary>
    Modulo,

    /// <summary>
    /// Power
    /// </summary>
    Power,

    /// <summary>
    /// Logical And
    /// </summary>
    And,

    /// <summary>
    /// Logical And (the second operand is not evaluated if the first is false)
    /// </summary>
    AndAlso,

    /// <summary>
    /// Logical Or
    /// </summary>
    Or,

    /// <summary>
    /// Logical Or (the second operand is not evaluated if the first is true)
    /// </summary>
    OrElse,

    /// <summary>
    /// Exclusive Or
    /// </summary>
    ExclusiveOr,

    /// <summary>
    /// Shift bits to left
    /// </summary>
    ShiftLeft,

    /// <summary>
    /// Shift bits to right
    /// </summary>
    ShiftRight,

    /// <summary>
    /// Equality
    /// </summary>
    Equal,

    /// <summary>
    /// Difference
    /// </summary>
    NotEqual,

    /// <summary>
    /// Equality in type and value
    /// </summary>
    Identical,

    /// <summary>
    /// Difference in type or value
    /// </summary>
    NotIdentical,

    /// <summary>
    /// Strict inferiority
    /// </summary>
    LessThan,

    /// <summary>
    /// Relative inferiority
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Strict superiority
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Relative superiority
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// String comparison operator : startswith
    /// </summary>
    StartsWith,

    /// <summary>
    /// String comparison operator : endswith
    /// </summary>
    EndsWith,

    /// <summary>
    /// String comparison operator : contains<br/>
    /// Also used for tuples, lists and sets
    /// </summary>
    Contains,

    /// <summary>
    /// String comparison operator : matches
    /// </summary>
    Matches,

    /// <summary>
    /// Empty value checking operator (??)
    /// </summary>
    IfEmpty
}