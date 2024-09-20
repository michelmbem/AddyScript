using AddyScript.Runtime.DataItems;


namespace AddyScript.Runtime
{
    /// <summary>
    /// Represents a function's parameter.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of Parameter.
    /// </remarks>
    /// <param name="name">The parameter's name.</param>
    /// <param name="byRef">Determines if the parameter is passed by reference or not</param>
    /// <param name="vaArgs">Determines if the parameter is a variably sized arguments list or not</param>
    /// <param name="defaultValue">The default value for this parameter if any</param>
    public class Parameter(string name, bool byRef, bool vaArgs, DataItem defaultValue)
    {
        /// <summary>
        /// Initializes a new instance of Parameter.
        /// </summary>
        /// <param name="name">The parameter's name.</param>
        public Parameter(string name)
            : this(name, false, false, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of Parameter.
        /// </summary>
        /// <param name="name">The parameter's name.</param>
        /// <param name="defaultValue">The default value for this parameter if any</param>
        public Parameter(string name, DataItem defaultValue)
            : this(name, false, false, defaultValue)
        {
        }

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
        /// Gets if this parameter is optional or not.
        /// </summary>
        public bool Optional => VaArgs || DefaultValue != null;

        /// <summary>
        /// The parameter's attributes.
        /// </summary>
        public DataItem[] Attributes { get; set; }
    }
}