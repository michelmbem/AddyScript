using System;


namespace AddyScript;


public class ScriptError : Exception
{
    public ScriptError(string fileName, ScriptElement element, Exception innerException)
        : base(innerException.Message, innerException)
    {
        FileName = fileName;
        Element = element;
    }

    public ScriptError(string fileName, ScriptElement element, string message)
        : base(message)
    {
        FileName = fileName;
        Element = element;
    }

    public string FileName { get; }

    public ScriptElement Element { get; }

    public ScriptError LocatedAt(ScriptElement parent)
    {
        if (Element.Start.IsEmpty && !parent.Start.IsEmpty)
            Element.CopyLocation(parent);

        return this;
    }
}