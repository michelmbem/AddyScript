using AddyScript.Translators;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a literal value.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of Literal.
    /// </remarks>
    /// <param name="value">The literal value wrapped by this instance</param>
    public class Literal(DataItem value = null) : Expression
    {

        /// <summary>
        /// The literal value wrapped by this instance.
        /// </summary>
        public DataItem Value { get; private set; } = value ?? Void.Value;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateLiteral(this);
        }
    }
}