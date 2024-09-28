namespace AddyScript;


/// <summary>
/// The base class of all script elements :<br/>
/// tokens, statements, expressions, parameters and class's members.
/// </summary>
public class ScriptElement
{
    /// <summary>
    /// Initializes a new instance of Element.
    /// </summary>
    /// <param name="start">The starting position of the element in the source code</param>
    /// <param name="end">The ending position of the element in the source code</param>
    public ScriptElement(ScriptLocation start, ScriptLocation end)
    {
        SetLocation(start, end);
    }

    /// <summary>
    /// Initializes a new instance of Element.
    /// </summary>
    public ScriptElement()
        : this(ScriptLocation.Empty, ScriptLocation.Empty)
    {
    }

    /// <summary>
    /// The starting position of the element in the source code.
    /// </summary>
    public ScriptLocation Start { get; private set; }

    /// <summary>
    /// The ending position of the element in the source code.
    /// </summary>
    public ScriptLocation End { get; private set; }

    /// <summary>
    /// The length of the element in the source code.
    /// </summary>
    public int Length => End.Offset - Start.Offset;

    /// <summary>
    /// Initializes the element's properties.
    /// </summary>
    /// <param name="start">The position of the element in the source code</param>
    /// <param name="end">The end (in characters) of the element</param>
    public void SetLocation(ScriptLocation start, ScriptLocation end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Copies the properties of another element.
    /// </summary>
    /// <param name="element">The element to copy</param>
    public void CopyLocation(ScriptElement element)
    {
        Start = element.Start;
        End = element.End;
    }

    /// <summary>
    /// Positions this <see cref="ScriptElement"/> relatively to another.
    /// </summary>
    /// <param name="other">The <see cref="ScriptElement"/> this instance should be positionned relatively to</param>
    /// <param name="gap">An additional gap to add to the offset</param>
    public void MoveRel(ScriptElement other, int gap)
    {
        Start.MoveRel(other.Start, gap);
        End.MoveRel(other.Start, gap);
    }
}