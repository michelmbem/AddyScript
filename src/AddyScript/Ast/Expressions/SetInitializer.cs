using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a set's initializer: a set of item initializer into braces.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of SetInitializer
    /// </remarks>
    /// <param name="items">The <see cref="ListItem"/>s that are listed between the delimiters</param>
    public class SetInitializer(params ListItem[] items) : SequenceInitializer(items)
    {
        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateSetInitializer(this);
        }
    }
}