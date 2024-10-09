using AddyScript.Ast.Statements;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a <b>throw</b> statement being used as an expression.
    /// </summary>
    public class ThrowExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ThrowExpression"/>.
        /// </summary>
        /// <param name="_throw">The wrapped <b>throw</b> statement</param>
        public ThrowExpression(Throw _throw)
        {
            Throw = _throw;
            CopyLocation(_throw);
        }
        
        /// <summary>
        /// The wrapped <b>throw</b> statement.
        /// </summary>
        public Throw Throw { get; private set; }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            Throw.AcceptTranslator(translator);
        }
    }
}
