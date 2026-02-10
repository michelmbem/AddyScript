using System;
using System.Reflection;

using AddyScript.Ast;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Runtime;


public class RuntimeError : ScriptError
{
    public RuntimeError(string fileName, AstNode astNode, Exception exception)
        : base(fileName, astNode, exception is TargetInvocationException ? exception.InnerException : exception) { }

    public RuntimeError(string fileName, AstNode astNode, string message)
        : base(fileName, astNode, message) { }

    public RuntimeError(string fileName, AstNode astNode, DataItem thrown)
        : base(fileName, astNode, thrown.GetProperty("__message").ToString())
    {
        Thrown = thrown;
        thrown.SetProperty("__source", new String(FileName));
        thrown.SetProperty("__line", new Integer(AstNode.Start.LineNumber));
    }

    public AstNode AstNode => (AstNode) Element;

    public DataItem Thrown { get; }
}