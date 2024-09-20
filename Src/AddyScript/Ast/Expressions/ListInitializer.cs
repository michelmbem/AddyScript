using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a list's initializer: a set of item initializers into brackets.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ListInitializer
    /// </remarks>
    /// <param name="items">The expressions that are listed between the delimiters</param>
    public class ListInitializer(params Expression[] items) : Expression
    {

        /// <summary>
        /// The expressions that are listed between the delimiters.
        /// </summary>
        public Expression[] Items { get; private set; } = items;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateListInitializer(this);
        }
    }
}