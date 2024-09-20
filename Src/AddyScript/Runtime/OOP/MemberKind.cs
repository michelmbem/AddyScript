using System;


namespace AddyScript.Runtime.OOP
{
    [Flags]
    public enum MemberKind
    {
        None = 0,
        Constructor = 1,
        Indexer = 2,
        Field = 4,
        Property = 8,
        Method = 16,
        Event = 32,
        All = Constructor| Indexer | Field | Property | Method | Event
    }
}
