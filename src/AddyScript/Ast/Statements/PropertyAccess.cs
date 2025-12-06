using System;


namespace AddyScript.Ast.Statements;


[Flags]
public enum PropertyAccess
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}
