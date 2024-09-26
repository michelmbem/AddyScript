using System.Collections.Generic;

using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a block of statements.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of Block
    /// </remarks>
    /// <param name="statements">Block's statements</param>
    public class Block(params Statement[] statements) : Statement
    {

        /// <summary>
        /// Block's statements
        /// </summary>
        public Statement[] Statements { get; private set; } = statements;

        /// <summary>
        /// The labels declared in the block.
        /// </summary>
        public Dictionary<string, Label> Labels { get; set; } = [];

        /// <summary>
        /// Represents an empty block.
        /// </summary>
        public static Block Empty => new ();

        /// <summary>
        /// Creates a block with a single return statement in it.
        /// </summary>
        /// <returns>Nothing</returns>
        public static Block Return() => new (new Return());

        /// <summary>
        /// Creates a block with a single return statement in it.
        /// </summary>
        /// <param name="expression">The expression to be returned</param>
        /// <returns>An <see cref="Expression"/></returns>
        public static Block Return(Expression expression) => new (new Return(expression));

        /// <summary>
        /// Appends a statement to the block.
        /// </summary>
        /// <param name="statement">The statement to append</param>
        public Block Append(Statement statement)
        {
            var list = new List<Statement>(Statements) { statement };
            Statements = [.. list];

            return this;
        }

        /// <summary>
        /// Inserts a statement in the block at the specified position.
        /// </summary>
        /// <param name="index">The position at which the statement will be inserted</param>
        /// <param name="statement">The statement to be inserted</param>
        public Block Insert(int index, Statement statement)
        {
            var list = new List<Statement>(Statements);
            list.Insert(index, statement);
            Statements = [.. list];

            return this;
        }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateBlock(this);
        }
    }
}