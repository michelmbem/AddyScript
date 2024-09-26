using System.Linq;

using AddyScript.Ast.Statements;
using AddyScript.Translators;
using AddyScript.Runtime;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an inline function's declaration or a lambda expression.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of InlineFunction
    /// </remarks>
    /// <param name="parameters">The function's parameters</param>
    /// <param name="body">The function's body</param>
    public class InlineFunction(ParameterDecl[] parameters, Block body) : Expression
    {

        /// <summary>
        /// The parameters of this function.
        /// </summary>
        public ParameterDecl[] Parameters { get; private set; } = parameters;

        /// <summary>
        /// The body of this function.
        /// </summary>
        public Block Body { get; private set; } = body;

        /// <summary>
        /// Verifies that an inline function is a lambda expression or not.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the first statement is a <b>return</b> with an expression.
        /// <b>false</b> otherwise
        /// </returns>
        public bool IsLambda()
        {
            Statement[] statements = Body.Statements;
            return statements.Length > 0 && statements[0] is Return ret && ret.Expression != null;
        }

        /// <summary>
        /// Create a <see cref="Function"/> from this instance.
        /// </summary>
        /// <returns>A <see cref="Function"/></returns>
        public Function ToFunction()
        {
            return new Function(Parameters.Select(p => p.ToParameter()).ToArray(), Body);
        }

        /// <summary>
        /// Translates this node.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateInlineFunction(this);
        }
    }
}