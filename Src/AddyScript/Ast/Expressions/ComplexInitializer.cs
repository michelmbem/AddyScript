using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a complex number's initializer: a couple of floating numbers between parenthesis.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ComplexInitializer
    /// </remarks>
    /// <param name="realInit">The expressions used to initialize the real part of the complex number</param>
    /// <param name="imagInit">The expressions used to initialize the imaginary part of the complex number</param>
    public class ComplexInitializer(Expression realInit, Expression imagInit) : Expression
    {

        /// <summary>
        /// The expressions used to initialize the real part of the complex number.
        /// </summary>
        public Expression RealPartInitializer { get; private set; } = realInit;

        /// <summary>
        /// The expressions used to initialize the imaginary part of the complex number.
        /// </summary>
        public Expression ImaginaryPartInitializer { get; private set; } = imagInit;

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateComplexInitializer(this);
        }
    }
}