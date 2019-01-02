using AddyScript.Runtime.Dynamics;


namespace AddyScript.Runtime.Frames
{
    /// <summary>
    /// Represents the context under which a <see cref="MethodFrame"/> is created.
    /// </summary>
    public class CallContext
    {
        /// <summary>
        /// Initializes an instance of CallContext.
        /// </summary>
        /// <param name="self">The called method's defining class</param>
        /// <param name="_this">The calling instance</param>
        /// <param name="name">The called method's name</param>
        public CallContext(Class self, Dynamic _this, string name)
        {
            Self = self;
            This = _this;
            Name = name;
        }

        /// <summary>
        /// The called method's defining class.
        /// </summary>
        public Class Self { get; private set; }

        /// <summary>
        /// The calling instance.
        /// </summary>
        public Dynamic This { get; private set; }

        /// <summary>
        /// The called method's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets if the called method is a constructor or not.
        /// </summary>
        /// <returns><b>true</b> if the called method is homonymous to its defining class;<b>false</b> otherwise.</returns>
        public bool IsConstructor()
        {
            return Self != null && Self.Name.Equals(Name);
        }
    }
}
