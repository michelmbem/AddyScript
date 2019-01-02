using System;


namespace AddyScript.Runtime
{
    [Flags]
    public enum MemberKind
    {
        None = 0,
        Constructor = 1,
        Field = 2,
        Property = 4,
        Method = 8,
        Event = 16,
        All = Constructor | Field | Property | Method | Event
    }
}
