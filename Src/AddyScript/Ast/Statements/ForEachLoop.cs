using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>for-each</b> statement
    /// </summary>
    public class ForEachLoop : Statement
    {
        /// <summary>
        /// The default value of <see cref="KeyName"/>
        /// </summary>
        public const string DEFAULT_KEY_NAME = "__key";

        /// <summary>
        /// Initializes a new instance of ForEachLoop
        /// </summary>
        /// <param name="keyName">The variable used to iterate through keys</param>
        /// <param name="valueName">The variable used to iterate through values</param>
        /// <param name="enumerated">The collection on which enumeration is done</param>
        /// <param name="body">The body of the loop</param>
        public ForEachLoop(string keyName, string valueName, Expression enumerated, Statement body)
        {
            KeyName = keyName;
            ValueName = valueName;
            Enumerated = enumerated;
            Body = body;
        }

        /// <summary>
        /// The variable used to iterate through keys.
        /// </summary>
        public string KeyName { get; private set; }

        /// <summary>
        /// The variable used to iterate through values.
        /// </summary>
        public string ValueName { get; private set; }

        /// <summary>
        /// The collection on witch enumeration is done.
        /// </summary>
        public Expression Enumerated { get; private set; }

        /// <summary>
        /// Represents the body of the loop
        /// </summary>
        public Statement Body { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileForEachLoop(this);
        }
    }
}