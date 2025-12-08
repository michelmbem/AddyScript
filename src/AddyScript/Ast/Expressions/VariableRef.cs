using AddyScript.Runtime.DataItems;
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
    public class VariableRef(string name) : Expression, IReference
    {

        /// <summary>
        /// The name of the referred variable
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Operates assignment to this reference.
        /// </summary>
        /// <param name="processor">The assignment processor to use</param>
        /// <param name="rValue">The value that should be assigned to this reference</param>
        public void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue)
        {
            processor.AssignToVariable(this, rValue);
        }

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