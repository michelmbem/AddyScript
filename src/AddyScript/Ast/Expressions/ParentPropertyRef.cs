using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a reference to the original implementation of an overriden property.<br>
    /// Looks in the code like: <code>super::someProp</code>.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ParentPropertyRef
    /// </remarks>
    /// <param name="propertyName">The property's name</param>
    public class ParentPropertyRef(string propertyName) : Expression, IReference
    {

        /// <summary>
        /// The field name.
        /// </summary>
        public string PropertyName { get; private set; } = propertyName;

        /// <param name="processor">The assignment processor to use</param>
        /// <param name="rValue">The value that should be assigned to this reference</param>
        public void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue)
        {
            processor.AssignToParentProperty(this, rValue);
        }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateParentPropertyRef(this);
        }
    }
}