using System;


namespace AddyScript
{
    public class ScriptException : ApplicationException
    {
        public ScriptException(string fileName, ScriptElement element, Exception innerException)
            : base(innerException.Message, innerException)
        {
            FileName = fileName;
            Element = element;
        }

        public ScriptException(string fileName, ScriptElement element, string message)
            : base(message)
        {
            FileName = fileName;
            Element = element;
        }

        public string FileName { get; private set; }

        public ScriptElement Element { get; private set; }
    }
}