using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;


namespace AddyScript.Parsers;


/// <summary>
/// Represents a terminal symbol.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="Token"/>
/// </remarks>
/// <param name="tokenID">The token's ID</param>
/// <param name="value">The optionally wrapped value of this token</param>
/// <param name="start">The starting position of the token in the source code</param>
/// <param name="end">The ending position of the token in the source code</param>
public class Token(TokenID tokenID, object value, ScriptLocation start, ScriptLocation end)
    : ScriptElement(start, end)
{

    /// <summary>
    /// The token's ID.
    /// </summary>
    public TokenID TokenID { get; private set; } = tokenID;

    /// <summary>
    /// The optionally wrapped value of this token.
    /// </summary>
    public object Value { get; private set; } = value;

    /// <summary>
    /// Gets if a token represent a literal number.
    /// </summary>
    public bool IsNumeric => TokenID.LT_Integer <= TokenID && TokenID <= TokenID.LT_Decimal;

    /// <summary>
    /// Gets if a token represent a literal string.
    /// </summary>
    public bool IsAlphabetic => TokenID == TokenID.LT_String || TokenID == TokenID.MutableString;

    /// <summary>
    /// Gets if a token represent a literal value.
    /// </summary>
    public bool IsLiteral => TokenID.LT_Null <= TokenID && TokenID <= TokenID.LT_String;

    /// <summary>
    /// Gets if a token represent a keyword.
    /// </summary>
    public bool IsKeyword => (TokenID.KW_TypeOf <= TokenID && TokenID <= TokenID.LT_Boolean) ||
                             (TokenID.TypeName <= TokenID && TokenID <= TokenID.Modifier);

    /// <summary>
    /// Gets if a token is a unary operator.
    /// </summary>
    public bool IsUnaryOperator => ToUnaryOperator(false) != UnaryOperator.None;

    /// <summary>
    /// Gets if a token is a binary operator.
    /// </summary>
    public bool IsBinaryOperator => ToBinaryOperator() != BinaryOperator.None;

    /// <summary>
    /// Gets if a token is a comment.
    /// </summary>
    public bool IsComment => TokenID == TokenID.BlockComment || TokenID == TokenID.LineComment;

    /// <summary>
    /// Gets the textual representation of a class of tokens.
    /// </summary>
    /// <param name="tokenID">The family's ID</param>
    /// <returns>A string</returns>
    public static string ToString(TokenID tokenID)
    {
        return tokenID switch
        {
            TokenID.Comma => ",",
            TokenID.Dot => ".",
            TokenID.DoubleDot => "..",
            TokenID.SemiColon => ";",
            TokenID.Colon => ":",
            TokenID.DoubleColon => "::",
            TokenID.Question => "?",
            TokenID.QuestionDot => "?.",
            TokenID.QuestionBracket => "?[",
            TokenID.DoubleQuestion => "??",
            TokenID.DoubleQuestionEqual => "??=",
            TokenID.LeftParenthesis => "(",
            TokenID.RightParenthesis => ")",
            TokenID.LeftBracket => "[",
            TokenID.RightBracket => "]",
            TokenID.LeftBrace => "{",
            TokenID.RightBrace => "}",
            TokenID.Equal => "=",
            TokenID.DoubleEqual => "==",
            TokenID.TripleEqual => "===",
            TokenID.Exclamation => "!",
            TokenID.ExclamationEqual => "!=",
            TokenID.ExclamationDoubleEqual => "!==",
            TokenID.LessThan => "<",
            TokenID.LessThanEqual => "<=",
            TokenID.GreaterThan => ">",
            TokenID.GreaterThanEqual => ">=",
            TokenID.Plus => "+",
            TokenID.DoublePlus => "++",
            TokenID.PlusEqual => "+=",
            TokenID.Minus => "-",
            TokenID.DoubleMinus => "--",
            TokenID.MinusEqual => "-=",
            TokenID.Asterisk => "*",
            TokenID.AsteriskEqual => "*=",
            TokenID.DoubleAsterisk => "**",
            TokenID.DoubleAsteriskEqual => "**=",
            TokenID.Slash => "/",
            TokenID.SlashEqual => "/=",
            TokenID.Percent => "%",
            TokenID.PercentEqual => "%=",
            TokenID.Tilda => "~",
            TokenID.Ampersand => "&",
            TokenID.DoubleAmpersand => "&&",
            TokenID.AmpersandEqual => "&=",
            TokenID.VerticalBar => "|",
            TokenID.DoubleVerticalBar => "||",
            TokenID.VerticalBarEqual => "|=",
            TokenID.Circumflex => "^",
            TokenID.CircumflexEqual => "^=",
            TokenID.Arrow => "=>",
            TokenID.DoubleLessThan => "<<",
            TokenID.DoubleLessThanEqual => "<<=",
            TokenID.DoubleGreaterThan => ">>",
            TokenID.DoubleGreaterThanEqual => ">>=",
            TokenID.Identifier or TokenID.MutableString or TokenID.TypeName or TokenID.Scope or
            TokenID.Modifier or TokenID.LineComment or TokenID.BlockComment or TokenID.EndOfFile => "[" + tokenID + "]",
            TokenID.LT_Boolean or TokenID.LT_Integer or TokenID.LT_Long or TokenID.LT_Float or
            TokenID.LT_Decimal or TokenID.LT_String or TokenID.LT_Date => "[" + tokenID.ToString()[3..] + "]",
            _ => tokenID.ToString()[3..].ToLower(),
        };
    }

    public override string ToString()
    {
        return TokenID switch
        {
            TokenID.Scope => Value.ToString().ToLower(),
            TokenID.Modifier => Value.Equals(Modifier.StaticFinal)
                              ? "static final"
                              : Value.ToString().ToLower(),
            _ => Value == null ? ToString(TokenID) : Value.ToString(),
        };
    }

    /// <summary>
    /// Converts a token to an unary operator.
    /// </summary>
    /// <returns>A member of the <see cref="UnaryOperator"/> enumeration</returns>
    public UnaryOperator ToUnaryOperator(bool postfix)
    {
        return TokenID switch
        {
            TokenID.Plus => UnaryOperator.Plus,
            TokenID.DoublePlus => postfix ? UnaryOperator.PostIncrement : UnaryOperator.PreIncrement,
            TokenID.Minus => UnaryOperator.Minus,
            TokenID.DoubleMinus => postfix ? UnaryOperator.PostDecrement : UnaryOperator.PreDecrement,
            TokenID.Exclamation => UnaryOperator.Not,
            TokenID.Tilda => UnaryOperator.BitwiseNot,
            _ => UnaryOperator.None,
        };
    }

    /// <summary>
    /// Converts a token to an binary operator.
    /// </summary>
    /// <returns>A member of the <see cref="BinaryOperator"/> enumeration</returns>
    public BinaryOperator ToBinaryOperator()
    {
        return TokenID switch
        {
            TokenID.Plus or TokenID.PlusEqual => BinaryOperator.Plus,
            TokenID.Minus or TokenID.MinusEqual => BinaryOperator.Minus,
            TokenID.Asterisk or TokenID.AsteriskEqual => BinaryOperator.Times,
            TokenID.Slash or TokenID.SlashEqual => BinaryOperator.Divide,
            TokenID.Percent or TokenID.PercentEqual => BinaryOperator.Modulo,
            TokenID.DoubleAsterisk or TokenID.DoubleAsteriskEqual => BinaryOperator.Power,
            TokenID.Ampersand or TokenID.AmpersandEqual => BinaryOperator.And,
            TokenID.DoubleAmpersand => BinaryOperator.AndAlso,
            TokenID.VerticalBar or TokenID.VerticalBarEqual => BinaryOperator.Or,
            TokenID.DoubleVerticalBar => BinaryOperator.OrElse,
            TokenID.Circumflex or TokenID.CircumflexEqual => BinaryOperator.ExclusiveOr,
            TokenID.DoubleEqual => BinaryOperator.Equal,
            TokenID.ExclamationEqual => BinaryOperator.NotEqual,
            TokenID.TripleEqual => BinaryOperator.Identical,
            TokenID.ExclamationDoubleEqual => BinaryOperator.NotIdentical,
            TokenID.LessThan => BinaryOperator.LessThan,
            TokenID.LessThanEqual => BinaryOperator.LessThanOrEqual,
            TokenID.GreaterThan => BinaryOperator.GreaterThan,
            TokenID.GreaterThanEqual => BinaryOperator.GreaterThanOrEqual,
            TokenID.KW_StartsWith => BinaryOperator.StartsWith,
            TokenID.KW_EndsWith => BinaryOperator.EndsWith,
            TokenID.KW_Contains => BinaryOperator.Contains,
            TokenID.KW_Matches => BinaryOperator.Matches,
            TokenID.DoubleLessThan or TokenID.DoubleLessThanEqual => BinaryOperator.ShiftLeft,
            TokenID.DoubleGreaterThan or TokenID.DoubleGreaterThanEqual => BinaryOperator.ShiftRight,
            TokenID.DoubleQuestion or TokenID.DoubleQuestionEqual => BinaryOperator.IfEmpty,
            _ => BinaryOperator.None,
        };
    }
}