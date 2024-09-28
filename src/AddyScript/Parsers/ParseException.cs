using System;

using AddyScript.Properties;


namespace AddyScript.Parsers;


public class ParseException : ScriptException
{
    public ParseException(string fileName, Token token, Exception innerException)
        : base(fileName, token, innerException)
    {
    }

    public ParseException(string fileName, Token token, string message)
        : base(fileName, token, message)
    {
    }

    public ParseException(string fileName, Token token)
        : this(fileName, token, string.Format(Resources.UnexpectedToken, token))
    {
    }

    public Token Token => (Token)Element;
}