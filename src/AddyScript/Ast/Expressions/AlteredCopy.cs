using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an expression with the <b>with</b> operator.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="AlteredCopy"/>
    /// </remarks>
    /// <param name="original">The object that's being copied</param>
    /// <param name="initializers">A set of initializers for some of the resulting object's properties</param>
    public class AlteredCopy(Expression original, params PropertyInitializer[] initializers)
        : ObjectInitializer(initializers)
    {

        /// <summary>
        /// Represents the object that's being copied.
        /// </summary>
        public Expression Original { get; private set; } = original;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateAlteredCopy(this);
        }
    }
}
