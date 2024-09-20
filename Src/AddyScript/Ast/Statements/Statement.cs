using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// The base class of all statements.<br/>
    /// Also represents an empty statement.
    /// </summary>
    public class Statement : AstNode
    {
        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            // Simply does nothing
        }
    }
}