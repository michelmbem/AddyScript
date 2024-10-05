namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents the initializer of a sequential collection.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="SequenceInitializer"/>
    /// </remarks>
    /// <param name="items">The <see cref="ListItem"/>s that are listed between the delimiters</param>
    public abstract class SequenceInitializer(params ListItem[] items) : Expression
    {

        /// <summary>
        /// The <see cref="ListItem"/> that are listed between the delimiters.
        /// </summary>
        public ListItem[] Items { get; private set; } = items;
    }
}