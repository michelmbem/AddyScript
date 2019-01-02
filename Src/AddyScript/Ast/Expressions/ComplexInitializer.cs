using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a complex number's initializer: a couple of floating numbers between parenthesis.
    /// </summary>
    public class ComplexInitializer : Expression
    {
        /// <summary>
        /// Initializes a new instance of ComplexInitializer
        /// </summary>
        /// <param name="realInit">The expressions used to initialize the real part of the complex number</param>
        /// <param name="imagInit">The expressions used to initialize the imaginary part of the complex number</param>
        public ComplexInitializer(Expression realInit, Expression imagInit)
        {
            RealPartInitializer = realInit;
            ImaginaryPartInitializer = imagInit;
        }

        /// <summary>
        /// The expressions used to initialize the real part of the complex number.
        /// </summary>
        public Expression RealPartInitializer { get; private set; }

        /// <summary>
        /// The expressions used to initialize the imaginary part of the complex number.
        /// </summary>
        public Expression ImaginaryPartInitializer { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileComplexInitializer(this);
        }
    }
}