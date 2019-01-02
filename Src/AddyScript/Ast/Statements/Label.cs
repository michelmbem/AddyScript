using AddyScript.Runtime.Frames;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a label.
    /// </summary>
    public class Label : ScriptElement, IFrameItem
    {
        /// <summary>
        /// Initializes a new instance of Label.
        /// </summary>
        /// <param name="address">The address of the statement that follows the label</param>
        public Label(int address)
        {
            Address = address;
        }

        /// <summary>
        /// Gets the kind of this frame's item.
        /// </summary>
        public FrameItemKind Kind
        {
            get { return FrameItemKind.Label; }
        }

        /// <summary>
        /// The address of the statement that follows the label.
        /// </summary>
        public int Address { get; private set; }
    }
}