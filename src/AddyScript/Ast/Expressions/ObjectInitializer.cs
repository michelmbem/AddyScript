using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an object initializer: a set of field initializers into braces.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ObjectInitializer
    /// </remarks>
    /// <param name="initializers">A set of initializers for the object's fields</param>
    public class ObjectInitializer(params PropertyInitializer[] initializers) : Expression
    {

        /// <summary>
        /// A set of initializers for the object's fields.
        /// </summary>
        public PropertyInitializer[] PropertyInitializers { get; private set; } = initializers;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateObjectInitializer(this);
        }
    }
}