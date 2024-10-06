using System.Collections.Generic;

using AddyScript.Ast.Statements;
using AddyScript.Translators;


namespace AddyScript.Ast
{
    /// <summary>
    /// Represents an entire script.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of Program
    /// </remarks>
    /// <param name="fileName">The program's source file's name</param>
    /// <param name="statements">Program's statements</param>
    public class Program(string fileName, params Statement[] statements) : AstNode
    {

        /// <summary>
        /// The program's source file's name.
        /// </summary>
        public string FileName { get; private set; } = fileName;

        /// <summary>
        /// Program's AST nodes.
        /// </summary>
        public Statement[] Statements { get; private set; } = statements;

        /// <summary>
        /// The labels declared in the program.
        /// </summary>
        public Dictionary<string, Label> Labels { get; set; } = [];

        /// <summary>
        /// Appends a statement to the program.
        /// </summary>
        /// <param name="statement">The statement to append</param>
        public Program Append(Statement statement)
        {
            Statements = [..Statements, statement];
            return this;
        }

        /// <summary>
        /// Inserts a statement in the program at the specified position.
        /// </summary>
        /// <param name="index">The position at which the statement will be inserted</param>
        /// <param name="statement">The statement to be inserted</param>
        public Program Insert(int index, Statement statement)
        {
            Statements = [..Statements[..index], statement, ..Statements[index..]];
            return this;
        }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateProgram(this);
        }
    }
}