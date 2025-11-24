using AddyScript.Ast.Statements;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a block of statements being used as an expression.
    /// </summary>
    public class BlockAsExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BlockAsExpression"/>.
        /// </summary>
        /// <param name="block">The wrapped <b>block</b> statement</param>
        public BlockAsExpression(Block block)
        {
            Block = block;
            CopyLocation(block);
        }
        
        /// <summary>
        /// The wrapped <b>block</b> statement.
        /// </summary>
        public Block Block { get; private set; }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateBlockAsExpression(this);
        }
    }
}