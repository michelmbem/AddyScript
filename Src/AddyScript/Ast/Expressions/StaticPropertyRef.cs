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
    public class StaticPropertyRef(QualifiedName name) : Expression
    {

        /// <summary>
        /// The qualified property's name.
        /// </summary>
        public QualifiedName Name { get; private set; } = name;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateStaticPropertyRef(this);
        }
    }
}