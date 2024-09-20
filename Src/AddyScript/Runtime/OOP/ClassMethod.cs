using System;

using AddyScript.Ast.Expressions;


namespace AddyScript.Runtime.OOP
{
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
        private static readonly Tuple<UnaryOperator, string>[] UnaryOperatorNames = new[]
        {
            new Tuple<UnaryOperator, string>(UnaryOperator.Plus, "__op_plus"),
            new Tuple<UnaryOperator, string>(UnaryOperator.Minus, "__op_minus"),
            new Tuple<UnaryOperator, string>(UnaryOperator.PreIncrement, "__op_pre_inc"),
            new Tuple<UnaryOperator, string>(UnaryOperator.PreDecrement, "__op_pre_dec"),
            new Tuple<UnaryOperator, string>(UnaryOperator.PostIncrement, "__op_post_inc"),
            new Tuple<UnaryOperator, string>(UnaryOperator.PostDecrement, "__op_post_dec"),
            new Tuple<UnaryOperator, string>(UnaryOperator.BitwiseNot, "__op_bw_not")
        };

        /// <summary>
        /// Stores the internal binary-operator <-> method-name mappings.
        /// </summary>
        private static readonly Tuple<BinaryOperator, string>[] BinaryOperatorNames = new[]
        {
            new Tuple<BinaryOperator, string>(BinaryOperator.Plus, "__op_add"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Minus, "__op_sub"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Times, "__op_mul"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Divide, "__op_div"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Modulo, "__op_mod"),
            new Tuple<BinaryOperator, string>(BinaryOperator.ShiftLeft, "__op_shl"),
            new Tuple<BinaryOperator, string>(BinaryOperator.ShiftRight, "__op_shr"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Power, "__op_pow"),
            new Tuple<BinaryOperator, string>(BinaryOperator.And, "__op_and"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Or, "__op_or"),
            new Tuple<BinaryOperator, string>(BinaryOperator.ExclusiveOr, "__op_xor"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Equal, "__op_eq"),
            new Tuple<BinaryOperator, string>(BinaryOperator.NotEqual, "__op_neq"),
            new Tuple<BinaryOperator, string>(BinaryOperator.LessThan, "__op_lt"),
            new Tuple<BinaryOperator, string>(BinaryOperator.GreaterThan, "__op_gt"),
            new Tuple<BinaryOperator, string>(BinaryOperator.LessThanOrEqual, "__op_lte"),
            new Tuple<BinaryOperator, string>(BinaryOperator.GreaterThanOrEqual, "__op_gte"),
            new Tuple<BinaryOperator, string>(BinaryOperator.StartsWith, "__op_startswith"),
            new Tuple<BinaryOperator, string>(BinaryOperator.EndsWith, "__op_endswith"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Contains, "__op_contains"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Matches, "__op_matches")
        };

        /// <summary>
        /// Encapsulates the logic of the method.
        /// </summary>
        public Function Function { get; private set; } = function;

        /// <summary>
        /// Gets the name of the method that's mapped to an overloaded unary operator.
        /// </summary>
        /// <param name="_operator">The given unary operator</param>
        /// <returns>A string</returns>
        public static string GetMethodName(UnaryOperator _operator)
        {
            foreach (var couple in UnaryOperatorNames)
                if (couple.Item1 == _operator)
                    return couple.Item2;

            return null;
        }

        /// <summary>
        /// Gets the name of the method that's mapped to an overloaded binary operator.
        /// </summary>
        /// <param name="_operator">The given binary operator</param>
        /// <returns>A string</returns>
        public static string GetMethodName(BinaryOperator _operator)
        {
            foreach (var couple in BinaryOperatorNames)
                if (couple.Item1 == _operator)
                    return couple.Item2;

            return null;
        }

        /// <summary>
        /// Gets a unary operator by its corresponding method name.
        /// </summary>
        /// <param name="methodName">The given method name</param>
        /// <returns>A <see cref="UnaryOperator"/></returns>
        public static UnaryOperator GetUnaryOperator(string methodName)
        {
            foreach (var couple in UnaryOperatorNames)
                if (couple.Item2 == methodName)
                    return couple.Item1;

            return UnaryOperator.None;
        }

        /// <summary>
        /// Gets a binary operator by its corresponding method name.
        /// </summary>
        /// <param name="methodName">The given method name</param>
        /// <returns>A <see cref="UnaryOperator"/></returns>
        public static BinaryOperator GetBinaryOperator(string methodName)
        {
            foreach (var couple in BinaryOperatorNames)
                if (couple.Item2 == methodName)
                    return couple.Item1;

            return BinaryOperator.None;
        }
    }
}