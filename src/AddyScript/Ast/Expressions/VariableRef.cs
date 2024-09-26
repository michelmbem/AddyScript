using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents the reference to a variable.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of VariableRef
    /// </remarks>
    /// <param name="name">The name of the referred variable</param>
    public class VariableRef(string name) : Expression
    {

        /// <summary>
        /// The name of the referred variable
        /// </summary>
        public string Name { get; private set; } = name;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateVariableRef(this);
        }
    }
}