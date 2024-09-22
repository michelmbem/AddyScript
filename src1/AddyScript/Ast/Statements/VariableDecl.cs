using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents an explicit variable's declaration.
    /// <remarks>Several variables can be declared once.</remarks>
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of VariableDecl
    /// </remarks>
    /// <param name="initializers">The set of (name, value) couples used to initialize variables.</param>
    public class VariableDecl(params PropertyInitializer[] initializers) : Statement
    {

        /// <summary>
        /// The set of (name, value) couples used to initialize variables.
        /// </summary>
        public PropertyInitializer[] Initializers { get; private set; } = initializers;

        /// <summary>
        /// A factory method to quickly create an instance with a single initializer.
        /// </summary>
        /// <param name="name">The variable's name</param>
        /// <param name="expr">An expression</param>
        /// <returns>A <see cref="VariableDecl"/></returns>
        public static VariableDecl Single(string name, Expression expr)
        {
            return new VariableDecl(new PropertyInitializer(name, expr));
        }

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateVariableDecl(this);
        }
    }
}