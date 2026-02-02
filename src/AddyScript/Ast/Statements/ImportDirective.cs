using AddyScript.Ast.Expressions;
using AddyScript.Translators;


namespace AddyScript.Ast.Statements;


/// <summary>
/// Represents an <b>import</b> directive.
/// </summary>
public class ImportDirective : Statement
{ 
    /// <summary>
    /// Initializes a new instance of ImportDirective with an alias.
    /// </summary>
    /// <param name="moduleName">The name of the module to be imported</param>
    /// <param name="alias">An eventually shorter name given to the imported namespace</param>
    public ImportDirective(QualifiedName moduleName, string alias = null)
    {
        ModuleName = moduleName;
        Alias = alias;
    }

    /// <summary>
    /// The name of the module to be imported.<br/>
    /// Should be a file name without the extension, with a dot
    /// replacing the directory separator, or a fully .Net type's
    /// qualified name or a .Net's namespace.
    /// </summary>
    public QualifiedName ModuleName { get; }

    /// <summary>
    /// An eventually shorter name given to the imported type or namespace.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets a value indicating whether an alias is defined.
    /// </summary>
    public bool HasAlias => !string.IsNullOrEmpty(Alias);

    /// <summary>
    /// Translates this node.
    /// </summary>
    /// <param name="translator">The translator to use</param>
    public override void AcceptTranslator(ITranslator translator)
    {
        translator.TranslateImportDirective(this);
    }
}