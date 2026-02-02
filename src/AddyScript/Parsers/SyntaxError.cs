using System;

using AddyScript.Properties;


namespace AddyScript.Parsers;


public class SyntaxError : ScriptError
{
    public SyntaxError(string fileName, Token token, Exception innerException)
        : base(fileName, token, innerException) { }

    public SyntaxError(string fileName, Token token, string message)
        : base(fileName, token, message) { }

    public SyntaxError(string fileName, Token token)
        : this(fileName, token, string.Format(Resources.UnexpectedToken, token)) { }

    public Token Token => (Token)Element;
}