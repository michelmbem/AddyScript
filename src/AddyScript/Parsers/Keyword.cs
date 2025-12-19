using System.Collections.Generic;

using AddyScript.Runtime.OOP;


namespace AddyScript.Parsers;


/// <summary>
/// Represents a keyword.
/// </summary>
/// <remarks>
/// Initializes ab instance of <see cref="Keyword"/>
/// </remarks>
/// <param name="tokenID">The keyword's <see cref="TokenID"/></param>
/// <param name="value">The keyword's value (if any)</param>
public class Keyword(TokenID tokenID, object value = null)
{
    /// <summary>
    /// A registry for the keywords.
    /// </summary>
    private static readonly Dictionary<string, Keyword> Registry = [];

    #region Class Initializer

    /// <summary>
    /// Fills up the repository
    /// </summary>
    static Keyword()
    {
        Register("const", TokenID.KW_Const);
        Register("var", TokenID.KW_Var);
        Register("let", TokenID.KW_Let);

        Register("if", TokenID.KW_If);
        Register("else", TokenID.KW_Else);
        Register("switch", TokenID.KW_Switch);
        Register("case", TokenID.KW_Case);
        Register("default", TokenID.KW_Default);
        Register("for", TokenID.KW_For);
        Register("foreach", TokenID.KW_ForEach);
        Register("while", TokenID.KW_While);
        Register("do", TokenID.KW_Do);
        Register("continue", TokenID.KW_Continue);
        Register("break", TokenID.KW_Break);
        Register("goto", TokenID.KW_Goto);
        Register("import", TokenID.KW_Import);
        Register("extern", TokenID.KW_Extern);
        Register("function", TokenID.KW_Function);
        Register("return", TokenID.KW_Return);
        Register("yield", TokenID.KW_Yield);
        Register("class", TokenID.KW_Class);
        Register("constructor", TokenID.KW_Constructor);
        Register("property", TokenID.KW_Property);
        Register("operator", TokenID.KW_Operator);
        Register("event", TokenID.KW_Event);
        Register("this", TokenID.KW_This);
        Register("super", TokenID.KW_Super);

        Register("throw", TokenID.KW_Throw);
        Register("try", TokenID.KW_Try);
        Register("catch", TokenID.KW_Catch);
        Register("finally", TokenID.KW_Finally);
        
        Register("as", TokenID.KW_As);
        Register("typeof", TokenID.KW_TypeOf);
        Register("is", TokenID.KW_Is);
        Register("not", TokenID.KW_Not);
        Register("startswith", TokenID.KW_StartsWith);
        Register("endswith", TokenID.KW_EndsWith);
        Register("contains", TokenID.KW_Contains);
        Register("matches", TokenID.KW_Matches);
        Register("in", TokenID.KW_In);
        Register("when", TokenID.KW_When);
        Register("and", TokenID.KW_And);
        Register("or", TokenID.KW_Or);
        Register("new", TokenID.KW_New);
        Register("with", TokenID.KW_With);

        Register("public", TokenID.Scope, Scope.Public);
        Register("protected", TokenID.Scope, Scope.Protected);
        Register("private", TokenID.Scope, Scope.Private);

        Register("abstract", TokenID.Modifier, Modifier.Abstract);
        Register("final", TokenID.Modifier, Modifier.Final);
        Register("static", TokenID.Modifier, Modifier.Static);

        Register("null", TokenID.LT_Null);
        Register("false", TokenID.LT_Boolean, false);
        Register("true", TokenID.LT_Boolean, true);

        Register("void", TokenID.TypeName, "void");
        Register("bool", TokenID.TypeName, "bool");
        Register("int", TokenID.TypeName, "int");
        Register("long", TokenID.TypeName, "long");
        Register("rational", TokenID.TypeName, "rational");
        Register("float", TokenID.TypeName, "float");
        Register("decimal", TokenID.TypeName, "decimal");
        Register("complex", TokenID.TypeName, "complex");
        Register("date", TokenID.TypeName, "date");
        Register("string", TokenID.TypeName, "string");
        Register("blob", TokenID.TypeName, "blob");
        Register("tuple", TokenID.TypeName, "tuple");
        Register("list", TokenID.TypeName, "list");
        Register("set", TokenID.TypeName, "set");
        Register("queue", TokenID.TypeName, "queue");
        Register("stack", TokenID.TypeName, "stack");
        Register("map", TokenID.TypeName, "map");
        Register("object", TokenID.TypeName, "object");
        Register("resource", TokenID.TypeName, "resource");
        Register("closure", TokenID.TypeName, "closure");
    }

    #endregion

    #region Properties

    /// <summary>
    /// The keyword's <see cref="TokenID"/>.
    /// </summary>
    public TokenID TokenID => tokenID;

    /// <summary>
    /// The keyword's value (if any).
    /// </summary>
    public object Value => value;

    #endregion

    #region Static methods

    /// <summary>
    /// Registers a keyword in the repository.
    /// </summary>
    /// <param name="text">The textual representation of the keyword</param>
    /// <param name="tokenID">The keyword's <see cref="TokenID"/></param>
    /// <param name="value">The keyword's value (if any)</param>
    private static void Register(string text, TokenID tokenID, object value = null) =>
        Registry.Add(text, new Keyword(tokenID, value));

    /// <summary>
    /// Gets if the given string matches a registered keyword.
    /// </summary>
    /// <param name="text">The textual representation of the keyword</param>
    /// <returns>A <see cref="bool"/></returns>
    public static bool IsDefined(string text) => Registry.ContainsKey(text);

    /// <summary>
    /// Gets the keyword matching the given string.
    /// </summary>
    /// <param name="text">The textual representation of the keyword</param>
    /// <returns>A <see cref="Keyword"/></returns>
    public static Keyword Get(string text) => Registry[text];

    #endregion
}
