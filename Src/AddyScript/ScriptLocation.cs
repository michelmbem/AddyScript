namespace AddyScript
{
    /// <summary>
    /// Represents a location in a script.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ScriptLocation.
    /// </remarks>
    /// <param name="offset">An absolute position in the source file</param>
    /// <param name="lineOffset">The beginning of the corresponding line</param>
    /// <param name="lineNumber">The corresponding line's number</param>
    public class  ScriptLocation(int offset, int lineOffset, int lineNumber)
    {
        /// <summary>
        /// Represents an undefined location in a script.
        /// </summary>
        public static readonly ScriptLocation Empty = new (0, 0, 0);

        /// <summary>
        /// An absolute position in the source file.
        /// </summary>
        public int Offset { get; private set; } = offset;

        /// <summary>
        /// The beginning of the corresponding line.
        /// </summary>
        public int LineOffset { get; private set; } = lineOffset;

        /// <summary>
        /// The corresponding line's number.
        /// </summary>
        public int LineNumber { get; private set; } = lineNumber;

        /// <summary>
        /// Positions this <see cref="ScriptLocation"/> relatively to another.
        /// </summary>
        /// <param name="other">The <see cref="ScriptLocation"/> this instance should be positionned relatively to</param>
        /// <param name="gap">An additional gap to add to the offset</param>
        public void MoveRel(ScriptLocation other, int gap)
        {
            Offset += other.Offset + gap;
            LineOffset += other.LineOffset;
            LineNumber += other.LineNumber;
        }
    }
}
