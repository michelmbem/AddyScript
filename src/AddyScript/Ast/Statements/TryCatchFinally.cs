using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents a <b>try-catch-finally</b> statement.
/// </summary>
/// <remarks>
/// Initializes a new instance of TryCatchFinally.
/// </remarks>
/// <param name="tryBlock">The try block</param>
/// <param name="exceptionName">The name of the caught exception</param>
/// <param name="catchBlock">The catch block</param>
/// <param name="finallyBlock">The finally block</param>
public class TryCatchFinally(Block tryBlock, string exceptionName, Block catchBlock, Block finallyBlock)
    : Statement
{
    /// <summary>
    /// The try block.
    /// </summary>
    public Block TryBlock => tryBlock;

    /// <summary>
    /// The name of the caught exception.
    /// </summary>
    public string ExceptionName => exceptionName;

    /// <summary>
    /// The catch block. At most one is allowed.
    /// </summary>
    public Block CatchBlock => catchBlock;

    /// <summary>
    /// The optional finally block.
    /// </summary>
    public Block FinallyBlock => finallyBlock;

    /// <summary>
    /// The optional resource that should be disposed upon completion.
    /// </summary>
    public Expression Resource { get; set; }

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateTryCatchFinally(this);
    }
}