using AddyScript.Translators;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents an expression in the form of <b>a ? b : c'</b>.
    /// </summary>
    public class TernaryExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of TernaryExpression
        /// </summary>
        /// <param name="test">The test expression</param>
        /// <param name="truePart">The Expression to be executed if the test returns true</param>
        /// <param name="falsePart">The Expression to be executed if the test returns false</param>
        public TernaryExpression(Expression test, Expression truePart, Expression falsePart)
        {
            Test = test;
            TruePart = truePart;
            FalsePart = falsePart;
            SetLocation(test.Start, falsePart.End);
        }

        /// <summary>
        /// The test expression
        /// </summary>
        public Expression Test { get; private set; }

        /// <summary>
        /// The Expression to be evaluated if the test returns true
        /// </summary>
        public Expression TruePart { get; private set; }

        /// <summary>
        /// The Expression to be evaluated if the test returns false
        /// </summary>
        public Expression FalsePart { get; private set; }

        /// <summary>
        /// Translates this statement.
        /// </summary>
        /// <param name="translator">The translator to use</param>
        public override void AcceptTranslator(ITranslator translator)
        {
            translator.TranslateTernaryExpression(this);
        }
    }
}