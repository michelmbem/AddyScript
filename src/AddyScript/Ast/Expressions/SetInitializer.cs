using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a set's initializer: a set of item initializer into braces prefixed with @.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of SetInitializer
    /// </remarks>
    /// <param name="items">The expressions that are listed between the delimiters</param>
    public class SetInitializer(params Expression[] items) : ListInitializer(items)
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