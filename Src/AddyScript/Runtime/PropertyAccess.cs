using System;


namespace AddyScript.Runtime
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
