using AddyScript.Ast.Expressions;
using AddyScript.Compilers;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents an <b>import</b> directive.
    /// </summary>
    public class ImportDirective : Statement
    {
        /// <summary>
        /// Initializes a new instance of ImportDirective.
        /// </summary>
        /// <param name="moduleName">The name of the module to be imported</param>
        public ImportDirective(QualifiedName moduleName)
        {
            ModuleName = moduleName;
        }
        
        /// <summary>
        /// Initializes a new instance of ImportDirective with an alias.
        /// </summary>
        /// <param name="moduleName">The name of the module to be imported</param>
        /// <param name="alias">An eventually shorter name given to the imported namespace</param>
        public ImportDirective(QualifiedName moduleName, string alias)
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
        public QualifiedName ModuleName { get; private set; }

        /// <summary>
        /// An eventually shorter name given to the imported type or namespace.
        /// </summary>
        public string Alias { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileImportDirective(this);
        }
    }
}