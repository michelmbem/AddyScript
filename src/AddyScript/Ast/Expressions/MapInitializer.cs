using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a map's initializer: a set of item initializers into braces.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ArrayInitializer
    /// </remarks>
    /// <param name="itemInitializers">The item initializers that are listed between the braces</param>
    public class MapInitializer(params MapItemInitializer[] itemInitializers) : Expression
    {

        /// <summary>
        /// The expressions that are listed between the braces.
        /// </summary>
        public MapItemInitializer[] ItemInitializers { get; private set; } = itemInitializers;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateMapInitializer(this);
        }
    }
}