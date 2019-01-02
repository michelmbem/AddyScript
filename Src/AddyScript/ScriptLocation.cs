namespace AddyScript
{
    /// <summary>
    /// Represents a location in a script.
    /// </summary>
    public class  ScriptLocation
    {
        /// <summary>
        /// Represents an undefined location in a script.
        /// </summary>
        public static readonly ScriptLocation Empty = new ScriptLocation(0, 0, 0);

        /// <summary>
        /// Initializes a new instance of ScriptLocation.
        /// </summary>
        /// <param name="offset">An absolute position in the source file</param>
        /// <param name="lineOffset">The beginning of the corresponding line</param>
        /// <param name="lineNumber">The corresponding line's number</param>
        public ScriptLocation(int offset, int lineOffset, int lineNumber)
        {
            Offset = offset;
            LineOffset = lineOffset;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// An absolute position in the source file.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// The beginning of the corresponding line.
        /// </summary>
        public int LineOffset { get; private set; }

        /// <summary>
        /// The corresponding line's number.
        /// </summary>
        public int LineNumber { get; private set; }
    }
}
