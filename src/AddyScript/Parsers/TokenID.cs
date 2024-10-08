﻿namespace AddyScript.Parsers;


/// <summary>
/// Any member represents a lexical unit.<br/>
/// Names are self-explanatory.
/// </summary>
public enum TokenID
{
    Unknown,
    Comma,
    Dot,
    DoubleDot,
    SemiColon,
    Colon,
    DoubleColon,
    Question,
    QuestionDot,
    QuestionBracket,
    DoubleQuestion,
    DoubleQuestionEqual,
    LeftParenthesis,
    RightParenthesis,
    LeftBracket,
    RightBracket,
    LeftBrace,
    RightBrace,
    Equal,
    DoubleEqual,
    TripleEqual,
    Exclamation,
    ExclamationEqual,
    ExclamationDoubleEqual,
    LessThan,
    LessThanEqual,
    GreaterThan,
    GreaterThanEqual,
    Plus,
    DoublePlus,
    PlusEqual,
    Minus,
    DoubleMinus,
    MinusEqual,
    Asterisk,
    AsteriskEqual,
    DoubleAsterisk,
    DoubleAsteriskEqual,
    Slash,
    SlashEqual,
    Percent,
    PercentEqual,
    DoubleLessThan,
    DoubleLessThanEqual,
    DoubleGreaterThan,
    DoubleGreaterThanEqual,
    Tilda,
    Ampersand,
    DoubleAmpersand,
    AmpersandEqual,
    VerticalBar,
    DoubleVerticalBar,
    VerticalBarEqual,
    Circumflex,
    CircumflexEqual,
    Arrow,
    KW_TypeOf,
    KW_Is,
    KW_Not,
    KW_StartsWith,
    KW_EndsWith,
    KW_Contains,
    KW_Matches,
    KW_With,
    KW_Import,
    KW_As,
    KW_Const,
    KW_Var,
    KW_If,
    KW_Else,
    KW_Switch,
    KW_Case,
    KW_Default,
    KW_For,
    KW_ForEach,
    KW_In,
    KW_While,
    KW_Do,
    KW_Continue,
    KW_Break,
    KW_Goto,
    KW_Yield,
    KW_Return,
    KW_Throw,
    KW_Function,
    KW_Extern,
    KW_Ref,
    KW_Params,
    KW_Class,
    KW_Constructor,
    KW_Property,
    KW_Operator,
    KW_Event,
    KW_This,
    KW_Super,
    KW_New,
    KW_Try,
    KW_Catch,
    KW_Finally,
    LT_Null,
    LT_Boolean,
    LT_Integer,
    LT_Long,
    LT_Float,
    LT_Decimal,
    LT_Complex,
    LT_Date,
    LT_String,
    LT_Blob,
    TypeName,
    Scope,
    Modifier,
    Identifier,
    MutableString,
    LineComment,
    BlockComment,
    EndOfFile
}