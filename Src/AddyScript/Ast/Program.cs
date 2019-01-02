using System.Collections.Generic;

using AddyScript.Ast.Statements;
using AddyScript.Compilers;


namespace AddyScript.Ast
{
    /// <summary>
    /// Represents the whole script.
    /// </summary>
    public class Program : AstNode
    {
        /// <summary>
        /// Initializes a new instance of Program
        /// </summary>
        /// <param name="fileName">The program's source file's name</param>
        /// <param name="statements">Program's statements</param>
        public Program(string fileName, params Statement[] statements)
        {
            FileName = fileName;
            Statements = statements;
            Labels = new Dictionary<string, Label>();
        }

        /// <summary>
        /// The program's source file's name.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Program's AST nodes.
        /// </summary>
        public Statement[] Statements { get; private set; }

        /// <summary>
        /// The labels declared in the program.
        /// </summary>
        public Dictionary<string, Label> Labels { get; set; }

        /// <summary>
        /// Appends a statement to the program.
        /// </summary>
        /// <param name="statement">The statement to append</param>
        public Program Append(Statement statement)
        {
            var statements = new List<Statement>(Statements) { statement };
            Statements = statements.ToArray();

            return this;
        }

        /// <summary>
        /// Inserts a statement in the program at the specified position.
        /// </summary>
        /// <param name="index">The position at which the statement will be inserted</param>
        /// <param name="statement">The statement to be inserted</param>
        public Program Insert(int index, Statement statement)
        {
            var statements = new List<Statement>(Statements);
            statements.Insert(index, statement);
            Statements = statements.ToArray();

            return this;
        }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileProgram(this);
        }
    }
}