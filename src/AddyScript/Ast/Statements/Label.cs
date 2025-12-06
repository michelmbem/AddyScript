using AddyScript.Runtime.Frames;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a label.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of Label.
    /// </remarks>
    /// <param name="address">The address of the statement that follows the label</param>
    public class Label(int address) : ScriptElement, IFrameItem
    {

        /// <summary>
        /// Gets the kind of this frame's item.
        /// </summary>
        public FrameItemKind Kind => FrameItemKind.Label;

        /// <summary>
        /// The address of the statement that follows the label.
        /// </summary>
        public int Address => address;
    }
}