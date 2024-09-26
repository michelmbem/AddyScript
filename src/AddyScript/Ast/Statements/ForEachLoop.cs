using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents a <b>for-each</b> statement
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ForEachLoop
    /// </remarks>
    /// <param name="keyName">The variable used to iterate through keys</param>
    /// <param name="valueName">The variable used to iterate through values</param>
    /// <param name="enumerated">The collection on which enumeration is done</param>
    /// <param name="action">The body of the loop</param>
    public class ForEachLoop(string keyName, string valueName, Expression enumerated, Statement action) : Statement
    {
        /// <summary>
        /// The default value of <see cref="KeyName"/>
        /// </summary>
        public const string DEFAULT_KEY_NAME = "__key";

        /// <summary>
        /// The variable used to iterate through keys.
        /// </summary>
        public string KeyName { get; private set; } = keyName;

        /// <summary>
        /// The variable used to iterate through values.
        /// </summary>
        public string ValueName { get; private set; } = valueName;

        /// <summary>
        /// The collection on witch enumeration is done.
        /// </summary>
        public Expression Enumerated { get; private set; } = enumerated;

        /// <summary>
        /// Represents the body of the loop
        /// </summary>
        public Statement Action { get; private set; } = action;

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateForEachLoop(this);
        }
    }
}