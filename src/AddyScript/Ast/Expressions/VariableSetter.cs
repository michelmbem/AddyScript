namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represent any sequence in the form <em>name = value</em> in the code.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of VariableSetter
    /// </remarks>
    /// <param name="name">The name of the variable or member to set</param>
    /// <param name="value">The value that should be set to the variable or member</param>
    public class VariableSetter(string name, Expression value) : ScriptElement
    {

        /// <summary>
        /// The name of the variable or member to set.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// The value that should be set to the variable or member.
        /// </summary>
        public Expression Value => value;

        #region Overrides

        public override int GetHashCode() => Name.GetHashCode();

        public override bool Equals(object obj) => obj is VariableSetter vs && Name == vs.Name;

        #endregion
    }
}