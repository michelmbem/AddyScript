using System;
using System.Reflection;

using AddyScript.Ast;
using AddyScript.Runtime.Dynamics;
using String = AddyScript.Runtime.Dynamics.String;


namespace AddyScript.Runtime
{
    public class RuntimeException : ScriptException
    {
        public RuntimeException(string fileName, AstNode astNode, Exception exception)
            : base(fileName, astNode, exception is TargetInvocationException ? exception.InnerException : exception)
        {
        }

        public RuntimeException(string fileName, AstNode astNode, string message)
            : base(fileName, astNode, message)
        {
        }

        public RuntimeException(string fileName, AstNode astNode, Dynamic thrown)
            : base(fileName, astNode, thrown.GetProperty("message").ToString())
        {
            Thrown = thrown;
            thrown.SetProperty("name", new String(thrown.Class.Name));
            thrown.SetProperty("source", new String(FileName));
            thrown.SetProperty("line", new Integer(AstNode.Start.LineNumber));
        }

        public AstNode AstNode
        {
            get { return (AstNode) ScriptElement; }
        }

        public Dynamic Thrown { get; private set; }
    }
}