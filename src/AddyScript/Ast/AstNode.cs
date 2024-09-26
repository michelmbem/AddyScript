using AddyScript.Translators;


namespace AddyScript.Ast
{
    /// <summary>
    /// The base class of all AST nodes.
    /// </summary>
    public abstract class AstNode : ScriptElement
    {
        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public abstract void AcceptTranslator(ITranslator translator);
    }
}