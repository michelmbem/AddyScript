using System;

using AddyScript.Ast.Expressions;


namespace AddyScript.Runtime
{
    /// <summary>
    /// Represents a method in a class.
    /// </summary>
    public class ClassMethod : ClassMember
    {
        /// <summary>
        /// Stores the internal unary-operator <-> method-name mappings.
        /// </summary>
        private static readonly Tuple<UnaryOperator, string>[] UnaryOperatorNames = new[]
        {
            new Tuple<UnaryOperator, string>(UnaryOperator.Plus, "__op_plus"),
            new Tuple<UnaryOperator, string>(UnaryOperator.Minus, "__op_minus"),
            new Tuple<UnaryOperator, string>(UnaryOperator.PreIncrement, "__op_plus_plus"),
            new Tuple<UnaryOperator, string>(UnaryOperator.PreDecrement, "__op_minus_minus"),
            new Tuple<UnaryOperator, string>(UnaryOperator.BitwiseNot, "__op_not")
        };

        /// <summary>
        /// Stores the internal binary-operator <-> method-name mappings.
        /// </summary>
        private static readonly Tuple<BinaryOperator, string>[] BinaryOperatorNames = new[]
        {
            new Tuple<BinaryOperator, string>(BinaryOperator.Plus, "__op_add"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Minus, "__op_subtract"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Times, "__op_multiply"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Divide, "__op_divide"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Modulo, "__op_modulo"),
            new Tuple<BinaryOperator, string>(BinaryOperator.ShiftLeft, "__op_shift_left"),
            new Tuple<BinaryOperator, string>(BinaryOperator.ShiftRight, "__op_shift_right"),
            new Tuple<BinaryOperator, string>(BinaryOperator.Power, "__op_power"),
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
        /// Initializes a new instance of ClassMethod.
        /// </summary>
        /// <param name="name">The method's name</param>
        /// <param name="scope">The scope of this method</param>
        /// <param name="modifier">Determines whether this method is abstract, final, static or none</param>
        /// <param name="function">The logic of this method</param>
        public ClassMethod(string name, Scope scope, Modifier modifier, Function function)
            : base(name, scope, modifier)
        {
            Function = function;
        }

        /// <summary>
        /// Encapsulates the logic of the method.
        /// </summary>
        public Function Function { get; private set; }

        /// <summary>
        /// Gets if a method has the same signature than another one.
        /// </summary>
        /// <param name="other">The other method</param>
        /// <returns><b>true</b> if both methods have the same scope and prototype. <b>false</b> otherwise</returns>
        public bool MatchesSignature(ClassMethod other)
        {
            return Name == other.Name &&
                   Scope == other.Scope &&
                   Function.MatchesSignature(other.Function);
        }

        /// <summary>
        /// Gets the name of a method which may be mapping a unary operator.
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
        /// Gets the name of a method which may be mapping a binary operator.
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
        /// Gets a unary operator by its mapping method name.
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
        /// Gets a binary operator by its mapping method name.
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