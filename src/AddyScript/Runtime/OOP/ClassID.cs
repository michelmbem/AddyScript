namespace AddyScript.Runtime.OOP;


/// <summary>
/// Class identifiers: uniquely identify each primitive type.<br/>
/// Play the same role than TypeCodes in .Net.
/// </summary>
public enum ClassID
{
    Undefined,
    Void,
    Boolean,
    Integer,
    Long,
    Rational,
    Float,
    Decimal,
    Complex,
    Date,
    String,
    Blob,
    Tuple,
    List,
    Set,
    Queue,
    Stack,
    Map,
    Object,
    Resource,
    Closure
}