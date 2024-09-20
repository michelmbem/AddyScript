using System;


namespace AddyScript.Runtime.OOP
{
    [Flags]
    public enum PropertyAccess
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = Read | Write
    }
}
