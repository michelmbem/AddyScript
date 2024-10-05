using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a list's initializer: a set of item initializers into brackets.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="ListInitializer"/>
    /// </remarks>
    /// <param name="items">The <see cref="ListItem"/>s that are listed between the delimiters</param>
    public class ListInitializer(params ListItem[] items) : SequenceInitializer(items)
    {
        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateListInitializer(this);
        }
    }
}