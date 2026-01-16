using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents an expression using the <b>with</b> operator.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="MutableCopy"/>
/// </remarks>
/// <param name="original">The object that's being copied</param>
/// <param name="mutators">A list of mutators for some of the properties</param>
public class MutableCopy(Expression original, params VariableSetter[] mutators)
    : ObjectInitializer(mutators)
{
    /// <summary>
    /// Represents the object that's being copied.
    /// </summary>
    public Expression Original => original;

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateMutableCopy(this);
    }
}