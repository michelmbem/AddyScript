using AddyScript.Ast.Expressions;


namespace AddyScript.Runtime.OOP;


/// <summary>
/// Represents a method in a class.
/// </summary>
/// <remarks>
/// Initializes a new instance of ClassMethod.
/// </remarks>
/// <param name="name">The method's name</param>
/// <param name="scope">The scope of this method</param>
/// <param name="modifier">Determines whether this method is abstract, final, static or none</param>
/// <param name="function">The logic of this method</param>
public class ClassMethod(string name, Scope scope, Modifier modifier, Function function)
    : ClassMember(name, scope, modifier)
{
    /// <summary>
    /// Stores the internal unary-operator <-> method-name mappings.
    /// </summary>
    private static readonly (UnaryOperator, string)[] UnaryOperatorNames =
    [
        (UnaryOperator.Plus, "__op_plus"),
        (UnaryOperator.Minus, "__op_minus"),
        (UnaryOperator.PreIncrement, "__op_pre_inc"),
        (UnaryOperator.PreDecrement, "__op_pre_dec"),
        (UnaryOperator.PostIncrement, "__op_post_inc"),
        (UnaryOperator.PostDecrement, "__op_post_dec"),
        (UnaryOperator.BitwiseNot, "__op_bw_not")
    ];

    /// <summary>
    /// Stores the internal binary-operator <-> method-name mappings.
    /// </summary>
    private static readonly (BinaryOperator, string)[] BinaryOperatorNames =
    [
        (BinaryOperator.Plus, "__op_add"),
        (BinaryOperator.Minus, "__op_sub"),
        (BinaryOperator.Times, "__op_mul"),
        (BinaryOperator.Divide, "__op_div"),
        (BinaryOperator.Modulo, "__op_mod"),
        (BinaryOperator.ShiftLeft, "__op_shl"),
        (BinaryOperator.ShiftRight, "__op_shr"),
        (BinaryOperator.Power, "__op_pow"),
        (BinaryOperator.And, "__op_and"),
        (BinaryOperator.Or, "__op_or"),
        (BinaryOperator.ExclusiveOr, "__op_xor"),
        (BinaryOperator.Equal, "__op_eq"),
        (BinaryOperator.NotEqual, "__op_neq"),
        (BinaryOperator.LessThan, "__op_lt"),
        (BinaryOperator.GreaterThan, "__op_gt"),
        (BinaryOperator.LessThanOrEqual, "__op_lte"),
        (BinaryOperator.GreaterThanOrEqual, "__op_gte"),
        (BinaryOperator.StartsWith, "__op_startswith"),
        (BinaryOperator.EndsWith, "__op_endswith"),
        (BinaryOperator.Contains, "__op_contains"),
        (BinaryOperator.Matches, "__op_matches")
    ];

    /// <summary>
    /// Encapsulates the logic of the method.
    /// </summary>
    public Function Function => function;

    /// <summary>
    /// Gets the name of the method that's mapped to an overloaded unary operator.
    /// </summary>
    /// <param name="_operator">The given unary operator</param>
    /// <returns>A string</returns>
    public static string GetMethodName(UnaryOperator _operator)
    {
        foreach (var (oper, name) in UnaryOperatorNames)
            if (oper == _operator) return name;

        return null;
    }

    /// <summary>
    /// Gets the name of the method that's mapped to an overloaded binary operator.
    /// </summary>
    /// <param name="_operator">The given binary operator</param>
    /// <returns>A string</returns>
    public static string GetMethodName(BinaryOperator _operator)
    {
        foreach (var (oper, name) in BinaryOperatorNames)
            if (oper == _operator) return name;

        return null;
    }

    /// <summary>
    /// Gets a unary operator by its corresponding method name.
    /// </summary>
    /// <param name="methodName">The given method name</param>
    /// <returns>A <see cref="UnaryOperator"/></returns>
    public static UnaryOperator GetUnaryOperator(string methodName)
    {
        foreach (var (oper, name) in UnaryOperatorNames)
            if (name == methodName) return oper;

        return UnaryOperator.None;
    }

    /// <summary>
    /// Gets a binary operator by its corresponding method name.
    /// </summary>
    /// <param name="methodName">The given method name</param>
    /// <returns>A <see cref="UnaryOperator"/></returns>
    public static BinaryOperator GetBinaryOperator(string methodName)
    {
        foreach (var (oper, name) in BinaryOperatorNames)
            if (name == methodName) return oper;

        return BinaryOperator.None;
    }
}