namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represent any name and value pair.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of VariableSetter
    /// </remarks>
    /// <param name="name">The name part</param>
    /// <param name="expr">The value part</param>
    public class VariableSetter(string name, Expression expr) : ScriptElement
    {

        /// <summary>
        /// The name part.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// The value part.
        /// </summary>
        public Expression Expression => expr;

        #region Overrides

        public override int GetHashCode() => Name.GetHashCode();

        public override bool Equals(object obj) => obj is VariableSetter vs && Name == vs.Name;

        #endregion
    }
}