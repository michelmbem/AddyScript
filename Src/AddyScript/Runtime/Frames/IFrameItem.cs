namespace AddyScript.Runtime.Frames
{
    /// <summary>
    /// Represents anything that could be stored in a <see cref="Frame"/>.
    /// </summary>
    public interface IFrameItem
    {
        /// <summary>
        /// Gets the kind of item this object is.
        /// </summary>
        FrameItemKind Kind { get; }
    }
}
