namespace AddyScript.Runtime.OOP;


/// <summary>
/// Class identifiers: uniquely identify each primitive type.<br/>
/// Play the same role as TypeCodes in .NET.
/// </summary>
public enum ClassID
{
    Void,
    Boolean,
    Integer,
    Long,
    Rational,
    Float,
    Decimal,
    Complex,
    Date,
    Duration,
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