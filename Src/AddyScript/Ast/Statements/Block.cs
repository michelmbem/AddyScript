using System.Collections.Generic;

using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a block of statements.
    /// </summary>
    public class Block : Statement
    {
        /// <summary>
        /// Initializes a new instance of Block
        /// </summary>
        /// <param name="statements">Block's statements</param>
        public Block(params Statement[] statements)
        {
            Statements = statements;
            Labels = new Dictionary<string, Label>();
        }

        /// <summary>
        /// Block's statements
        /// </summary>
        public Statement[] Statements { get; private set; }

        /// <summary>
        /// The labels declared in the block.
        /// </summary>
        public Dictionary<string, Label> Labels { get; set; }

        /// <summary>
        /// Represents an empty block.
        /// </summary>
        public static Block Empty
        {
            get { return new Block(); }
        }

        /// <summary>
        /// Creates a block with a single return statement in it.
        /// </summary>
        /// <returns>Nothing</returns>
        public static Block Return()
        {
            return new Block(new Return());
        }

        /// <summary>
        /// Creates a block with a single return statement in it.
        /// </summary>
        /// <param name="expression">The expression to be returned</param>
        /// <returns>An <see cref="AddyScript.Ast.Expressions.Expression"/></returns>
        public static Block Return(Expression expression)
        {
            return new Block(new Return(expression));
        }

        /// <summary>
        /// Appends a statement to the block.
        /// </summary>
        /// <param name="statement">The statement to append</param>
        public Block Append(Statement statement)
        {
            var list = new List<Statement>(Statements) { statement };
            Statements = list.ToArray();

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
            Statements = list.ToArray();

            return this;
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileBlock(this);
        }
    }
}