using AddyScript.Runtime;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of a function's parameter.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ParameterDecl.
    /// </remarks>
    /// <param name="name">The parameter's name.</param>
    /// <param name="byRef">Determines if the parameter is passed by reference or not</param>
    /// <param name="vaArgs">Determines if the parameter is a variably sized arguments list or not</param>
    /// <param name="defaultValue">The default value for this parameter if any</param>
    public class ParameterDecl(string name, bool byRef, bool vaArgs, DataItem defaultValue) : SymbolWithAttributes
    {

        /// <summary>
        /// The parameter's name.
        /// </summary>
        public string Name { get; private set; } = name;

        /// <summary>
        /// Determines if the parameter is passed by reference or not.
        /// </summary>
        public bool ByRef { get; private set; } = byRef;

        /// <summary>
        /// Determines if the parameter is a variably sized arguments list or not.
        /// </summary>
        public bool VaArgs { get; private set; } = vaArgs;

        /// <summary>
        /// The parameter's default value if any.
        /// </summary>
        public DataItem DefaultValue { get; private set; } = defaultValue;

        /// <summary>
        /// Create a <see cref="Parameter"/> from this instance.
        /// </summary>
        /// <returns>A <see cref="Parameter"/></returns>
        public Parameter ToParameter()
        {
            return new Parameter(Name, ByRef, VaArgs, DefaultValue);
        }

        #region Overrides

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ParameterDecl paramDecl && Name == paramDecl.Name;
        }

        #endregion
    }
}
