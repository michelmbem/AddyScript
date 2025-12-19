using AddyScript.Translators;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents a <b>goto</b> statement.
/// </summary>
/// <remarks>
/// Initializes a new instance of Goto.
/// </remarks>
/// <param name="labelName">The label following the goto</param>
public class Goto(string labelName) : Statement
{
    /// <summary>
    /// The label following the goto.
    /// </summary>
    public string LabelName => labelName;

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateGoto(this);
    }
}