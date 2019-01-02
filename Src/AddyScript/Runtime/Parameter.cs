using AddyScript.Runtime.Dynamics;


namespace AddyScript.Runtime
{
    /// <summary>
    /// Represents a function's parameter.
    /// </summary>
    public class Parameter : ScriptElement
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
        public Parameter(string name, Dynamic defaultValue)
            : this(name, false, false, defaultValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of Parameter.
        /// </summary>
        /// <param name="name">The parameter's name.</param>
        /// <param name="byRef">Determines if the parameter is passed by reference or not</param>
        /// <param name="vaArgs">Determines if the parameter is a variably sized arguments list or not</param>
        /// <param name="defaultValue">The default value for this parameter if any</param>
        public Parameter(string name, bool byRef, bool vaArgs, Dynamic defaultValue)
        {
            Name = name;
            ByRef = byRef;
            VaArgs = vaArgs;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// The parameter's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Determines if the parameter is passed by reference or not.
        /// </summary>
        public bool ByRef { get; private set; }

        /// <summary>
        /// Determines if the parameter is a variably sized arguments list or not.
        /// </summary>
        public bool VaArgs { get; private set; }

        /// <summary>
        /// The parameter's default value if any.
        /// </summary>
        public Dynamic DefaultValue { get; private set; }

        /// <summary>
        /// Returns an empty array of parameters.
        /// </summary>
        public static Parameter[] EmptyArray
        {
            get { return new Parameter[] {}; }
        }

        /// <summary>
        /// The parameter's annotations.
        /// </summary>
        public Attribute[] Attributes { get; set; }

        /// <summary>
        /// Gets an annotation in the list by its name.
        /// </summary>
        /// <param name="name">The name of an attribute</param>
        /// <returns><see cref="Attribute"/></returns>
        public Attribute GetAttribute(string name)
        {
            if (Attributes != null)
                foreach (Attribute attribute in Attributes)
                    if (attribute.Name == name)
                        return attribute;

            return null;
        }

        #region Overrides

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is Parameter) && Name == ((Parameter) obj).Name;
        }

        #endregion
    }
}