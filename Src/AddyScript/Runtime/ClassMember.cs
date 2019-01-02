namespace AddyScript.Runtime
{
    /// <summary>
    /// The base class of all class members.
    /// </summary>
    public abstract class ClassMember : ScriptElement
    {
        /// <summary>
        /// Initializes a new instance of ClassMember.
        /// </summary>
        /// <param name="name">The member's name</param>
        /// <param name="scope">The scope of this member</param>
        /// <param name="modifier">Determines whether this member is abstract, final, static or none</param>
        protected ClassMember(string name, Scope scope, Modifier modifier)
        {
            Name = name;
            Scope = scope;
            Modifier = modifier;
        }

        /// <summary>
        /// The member's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The scope of this member.
        /// </summary>
        public Scope Scope { get; private set; }

        /// <summary>
        /// Determines whether this member is abstract, final, static or none.
        /// </summary>
        public Modifier Modifier { get; private set; }

        /// <summary>
        /// Represents the class in which this member is declared
        /// </summary>
        public Class Definer { get; set; }

        /// <summary>
        /// The name of this member prefixed by the class name.
        /// </summary>
        public string FullName
        {
            get { return Definer == null ? Name : Definer.Name + "." + Name; }
        }

        /// <summary>
        /// The member's attributes.
        /// </summary>
        public Attribute[] Attributes { get; set; }

        /// <summary>
        /// Gets an attribute by its name.
        /// </summary>
        /// <param name="name">The name of an attribute</param>
        /// <returns><see cref="Attribute"/></returns>
        public Attribute GetAttribute(string name)
        {
            if (Attributes == null)
                return null;

            foreach (Attribute attribute in Attributes)
                if (attribute.Name == name)
                    return attribute;

            return null;
        }
    }
}