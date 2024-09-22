using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a 'break' statement.
    /// </summary>
    public class Break : Statement
    {
        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateBreak(this);
        }
    }
}