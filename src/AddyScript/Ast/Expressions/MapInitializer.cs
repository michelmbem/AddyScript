using AddyScript.Translators;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a map's initializer: a set of item initializers into braces.
/// </summary>
/// <remarks>
/// Initializes a new instance of ArrayInitializer
/// </remarks>
/// <param name="entries">The key/value pairs listed between braces</param>
public class MapInitializer(params MapEntry[] entries) : Expression
{
    /// <summary>
    /// The key/value pairs listed between braces.
    /// </summary>
    public MapEntry[] Entries => entries;

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateMapInitializer(this);
    }
}