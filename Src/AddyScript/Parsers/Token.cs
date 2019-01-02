using AddyScript.Ast.Expressions;


namespace AddyScript.Parsers
{
    /// <summary>
    /// Represents a token.
    /// </summary>
    public class Token : ScriptElement
    {
        /// <summary>
        /// Initializes a new instance of Token
        /// </summary>
        /// <param name="tokenID">The token's ID</param>
        /// <param name="value">The value of this token</param>
        /// <param name="start">The starting position of the token in the source code</param>
        /// <param name="end">The ending position of the token in the source code</param>
        public Token(TokenID tokenID, object value, ScriptLocation start, ScriptLocation end)
            : base(start, end)
        {
            TokenID = tokenID;
            Value = value;
        }

        /// <summary>
        /// The token's ID.
        /// </summary>
        public TokenID TokenID { get; private set; }

        /// <summary>
        /// The value of this token.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Gets if a token is a comment.
        /// </summary>
        public bool IsComment
        {
            get { return TokenID == TokenID.BlockComment || TokenID == TokenID.LineComment; }
        }

        /// <summary>
        /// Gets the textual representation of a class of tokens.
        /// </summary>
        /// <param name="tokenID">The family's ID</param>
        /// <returns>A string</returns>
        public static string ToString(TokenID tokenID)
        {
            switch (tokenID)
            {
                case TokenID.Comma:
                    return ",";
                case TokenID.Dot:
                    return ".";
                case TokenID.SemiColon:
                    return ";";
                case TokenID.Colon:
                    return ":";
                case TokenID.DoubleColon:
                    return "::";
                case TokenID.Question:
                    return "?";
                case TokenID.DoubleQuestion:
                    return "??";
                case TokenID.LeftParenthesis:
                    return "(";
                case TokenID.RightParenthesis:
                    return ")";
                case TokenID.LeftBracket:
                    return "[";
                case TokenID.RightBracket:
                    return "]";
                case TokenID.LeftBrace:
                    return "{";
                case TokenID.RightBrace:
                    return "}";
                case TokenID.Equal:
                    return "=";
                case TokenID.DoubleEqual:
                    return "==";
                case TokenID.TripleEqual:
                    return "===";
                case TokenID.Exclamation:
                    return "!";
                case TokenID.ExclamationEqual:
                    return "!=";
                case TokenID.ExclamationDoubleEqual:
                    return "!==";
                case TokenID.LessThan:
                    return "<";
                case TokenID.LessThanEqual:
                    return "<=";
                case TokenID.GreaterThan:
                    return ">";
                case TokenID.GreaterThanEqual:
                    return ">=";
                case TokenID.Plus:
                    return "+";
                case TokenID.DoublePlus:
                    return "++";
                case TokenID.PlusEqual:
                    return "+=";
                case TokenID.Minus:
                    return "-";
                case TokenID.DoubleMinus:
                    return "--";
                case TokenID.MinusEqual:
                    return "-=";
                case TokenID.Asterisk:
                    return "*";
                case TokenID.AsteriskEqual:
                    return "*=";
                case TokenID.DoubleAsterisk:
                    return "**";
                case TokenID.DoubleAsteriskEqual:
                    return "**=";
                case TokenID.Slash:
                    return "/";
                case TokenID.SlashEqual:
                    return "/=";
                case TokenID.Percent:
                    return "%";
                case TokenID.PercentEqual:
                    return "%=";
                case TokenID.Tilda:
                    return "~";
                case TokenID.Ampersand:
                    return "&";
                case TokenID.DoubleAmpersand:
                    return "&&";
                case TokenID.AmpersandEqual:
                    return "&=";
                case TokenID.VerticalBar:
                    return "|";
                case TokenID.DoubleVerticalBar:
                    return "||";
                case TokenID.VerticalBarEqual:
                    return "|=";
                case TokenID.Circumflex:
                    return "^";
                case TokenID.CircumflexEqual:
                    return "^=";
                case TokenID.Arrow:
                    return "=>";
                case TokenID.DoubleLessThan:
                    return "<<";
                case TokenID.DoubleLessThanEqual:
                    return "<<=";
                case TokenID.DoubleGreaterThan:
                    return ">>";
                case TokenID.DoubleGreaterThanEqual:
                    return ">>=";
                case TokenID.Identifier:
                case TokenID.TypeName:
                case TokenID.Scope:
                case TokenID.Modifier:
                case TokenID.LineComment:
                case TokenID.BlockComment:
                case TokenID.EndOfFile:
                    return "[" + tokenID + "]";
                case TokenID.LT_Boolean:
                case TokenID.LT_Integer:
                case TokenID.LT_Long:
                case TokenID.LT_Float:
                case TokenID.LT_Decimal:
                case TokenID.LT_String:
                case TokenID.LT_Date:
                    return "[" + tokenID.ToString().Substring(3) + "]";
                default:
                    return tokenID.ToString().Substring(3).ToLower();
            }
        }

        public override string ToString()
        {
            switch (TokenID)
            {
                case TokenID.Scope:
                    return Value.ToString().ToLower();
                case TokenID.Modifier:
                    return Value.Equals(Runtime.Modifier.StaticFinal)
                        ? "static final"
                        : Value.ToString().ToLower();
                default:
                    return Value == null ? ToString(TokenID) : Value.ToString();
            }
        }

        /// <summary>
        /// Converts a token to an unary operator.
        /// </summary>
        /// <returns>A member of the <see cref="UnaryOperator"/> enumeration</returns>
        public UnaryOperator ToUnaryOperator()
        {
            switch (TokenID)
            {
                case TokenID.Plus:
                    return UnaryOperator.Plus;
                case TokenID.DoublePlus:
                    return UnaryOperator.PreIncrement;
                case TokenID.Minus:
                    return UnaryOperator.Minus;
                case TokenID.DoubleMinus:
                    return UnaryOperator.PreDecrement;
                case TokenID.Exclamation:
                    return UnaryOperator.Not;
                case TokenID.Tilda:
                    return UnaryOperator.BitwiseNot;
                default:
                    return UnaryOperator.None;
            }
        }

        /// <summary>
        /// Converts a token to an binary operator.
        /// </summary>
        /// <returns>A member of the <see cref="BinaryOperator"/> enumeration</returns>
        public BinaryOperator ToBinaryOperator()
        {
            switch (TokenID)
            {
                case TokenID.Plus:
                case TokenID.PlusEqual:
                    return BinaryOperator.Plus;
                case TokenID.Minus:
                case TokenID.MinusEqual:
                    return BinaryOperator.Minus;
                case TokenID.Asterisk:
                case TokenID.AsteriskEqual:
                    return BinaryOperator.Times;
                case TokenID.Slash:
                case TokenID.SlashEqual:
                    return BinaryOperator.Divide;
                case TokenID.Percent:
                case TokenID.PercentEqual:
                    return BinaryOperator.Modulo;
                case TokenID.DoubleAsterisk:
                case TokenID.DoubleAsteriskEqual:
                    return BinaryOperator.Power;
                case TokenID.Ampersand:
                case TokenID.AmpersandEqual:
                    return BinaryOperator.And;
                case TokenID.DoubleAmpersand:
                    return BinaryOperator.AndAlso;
                case TokenID.VerticalBar:
                case TokenID.VerticalBarEqual:
                    return BinaryOperator.Or;
                case TokenID.DoubleVerticalBar:
                    return BinaryOperator.OrElse;
                case TokenID.Circumflex:
                case TokenID.CircumflexEqual:
                    return BinaryOperator.ExclusiveOr;
                case TokenID.DoubleEqual:
                    return BinaryOperator.Equal;
                case TokenID.ExclamationEqual:
                    return BinaryOperator.NotEqual;
                case TokenID.TripleEqual:
                    return BinaryOperator.Identical;
                case TokenID.ExclamationDoubleEqual:
                    return BinaryOperator.NotIdentical;
                case TokenID.LessThan:
                    return BinaryOperator.LessThan;
                case TokenID.LessThanEqual:
                    return BinaryOperator.LessThanOrEqual;
                case TokenID.GreaterThan:
                    return BinaryOperator.GreaterThan;
                case TokenID.GreaterThanEqual:
                    return BinaryOperator.GreaterThanOrEqual;
                case TokenID.KW_StartsWith:
                    return BinaryOperator.StartsWith;
                case TokenID.KW_EndsWith:
                    return BinaryOperator.EndsWith;
                case TokenID.KW_Contains:
                    return BinaryOperator.Contains;
                case TokenID.KW_Matches:
                    return BinaryOperator.Matches;
                case TokenID.DoubleLessThan:
                case TokenID.DoubleLessThanEqual:
                    return BinaryOperator.ShiftLeft;
                case TokenID.DoubleGreaterThan:
                case TokenID.DoubleGreaterThanEqual:
                    return BinaryOperator.ShiftRight;
                case TokenID.DoubleQuestion:
                    return BinaryOperator.IfNull;
                default:
                    return BinaryOperator.None;
            }
        }
    }
}