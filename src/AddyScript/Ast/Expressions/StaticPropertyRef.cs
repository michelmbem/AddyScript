using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a reference to a static property.<br/>
    /// May also match an instance property.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of StaticPropertyRef
    /// </remarks>
    /// <param name="name">The qualified property's name</param>
    public class StaticPropertyRef(QualifiedName name) : Expression, IReference
    {

        /// <summary>
        /// The qualified property's name.
        /// </summary>
        public QualifiedName Name => name;


        /// <summary>
        /// Operates assignment to this reference.
        /// </summary>
        /// <param name="processor">The assignment processor to use</param>
        /// <param name="rValue">The value that should be assigned to this reference</param>
        public void AcceptAssignmentProcessor(IAssignmentProcessor processor, DataItem rValue)
        {
            processor.AssignToStaticProperty(this, rValue);
        }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateStaticPropertyRef(this);
        }
    }
}