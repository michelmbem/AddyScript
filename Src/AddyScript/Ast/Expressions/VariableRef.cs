using AddyScript.Compilers;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents the reference to a variable.
    /// </summary>
    public class VariableRef : Expression
    {
        /// <summary>
        /// Initializes a new instance of VariableRef
        /// </summary>
        /// <param name="name">The name of the referred variable</param>
        public VariableRef(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of the referred variable
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Compiles or interprets this statement.
        /// </summary>
        /// <param name="compiler">The compiler or interpreter</param>
        public override void AcceptCompiler(ICompiler compiler)
        {
            compiler.CompileVariableRef(this);
        }
    }
}