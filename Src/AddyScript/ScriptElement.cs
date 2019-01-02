namespace AddyScript
{
    /// <summary>
    /// The base class of all script elements :<br/>
    /// tokens, statements, expressions, parameters and class's members.
    /// </summary>
    public class ScriptElement
    {
        /// <summary>
        /// Initializes a new instance of ScriptElement.
        /// </summary>
        /// <param name="start">The starting position of the element in the source code</param>
        /// <param name="end">The ending position of the element in the source code</param>
        public ScriptElement(ScriptLocation start, ScriptLocation end)
        {
            SetLocation(start, end);
        }

        /// <summary>
        /// Initializes a new instance of ScriptElement.
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
        /// Initializes the element's properties.
        /// </summary>
        /// <param name="start">The position of the element in the source code</param>
        /// <param name="end">The end (in characters) of the element</param>
        internal void SetLocation(ScriptLocation start, ScriptLocation end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Copies the properties of another element.
        /// </summary>
        /// <param name="element">The element to copy</param>
        internal void CopyLocation(ScriptElement element)
        {
            Start = element.Start;
            End = element.End;
        }
    }
}