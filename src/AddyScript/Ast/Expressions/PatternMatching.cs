using AddyScript.Runtime.DataItems;
using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents an expression with the <b>switch</b> operator.
/// </summary>
/// <remarks>
/// Initializes a new instance of <cee cref="PatternMatching"/>.
/// </remarks>
/// <param name="expression">The expression against which to match patterns</param>
/// <param name="cases">The patterns to match with associated expressions</param>
public class PatternMatching(Expression expression, params MatchCase[] cases) : Expression
{
    /// <summary>
    /// The expression against which to match patterns.
    /// </summary>
    public Expression Expression => expression;

    /// <summary>
    /// The patterns to match with associated expressions.
    /// </summary>
    public MatchCase[] MatchCases => cases;

    /// <summary>
    /// Determines whether this is a simple boolean pattern matching.
    /// </summary>
    public bool IsSimple => cases.Length == 2 &&
                            cases[0] is { Guard: null, Expression: Literal literal1 } &&
                            cases[1] is { Pattern: AlwaysTruePattern,  Guard: null, Expression: Literal literal2 } &&
                            Equals(literal1.Value, Boolean.True) &&
                            Equals(literal2.Value, Boolean.False);

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslatePatternMatching(this);
    }
}