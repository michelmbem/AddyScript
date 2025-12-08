using AddyScript.Runtime.OOP;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of class property.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ClassPropertyDecl.
    /// </remarks>
    /// <param name="name">The property's name</param>
    /// <param name="scope">The property's scope; it may be <b>private</b>, <b>protected</b> or <b>public</b></param>
    /// <param name="modifier">property's modifier; it may be <b>static</b>, <b>final</b>, <b>abstract</b> or nothing</param>
    /// <param name="access">The desired property access mode. Used for automatic accessors generation</param>
    /// <param name="readerScope">The read accessor scope</param>
    /// <param name="readerBody">The read accessor body</param>
    /// <param name="writerScope">The write accessor scope</param>
    /// <param name="writerBody">The write accessor body</param>
    public class ClassPropertyDecl(string name, Scope scope, Modifier modifier, PropertyAccess access,
                                   Scope readerScope, Block readerBody, Scope writerScope, Block writerBody) :
        ClassMemberDecl(name, scope, modifier)
    {
        /// <summary>
        /// Determines which accessors to generate when they are not explicitly provided.
        /// </summary>
        public PropertyAccess Access => access;

        /// <summary>
        /// The scope of an eventually generated read accessor.
        /// </summary>
        public Scope ReaderScope => readerScope;

        /// <summary>
        /// The property's read accessor.
        /// </summary>
        public Block ReaderBody => readerBody;

        /// <summary>
        /// The scope of an eventually generated write accessor.
        /// </summary>
        public Scope WriterScope => writerScope;

        /// <summary>
        /// The property's write accessor.
        /// </summary>
        public Block WriterBody => writerBody;

        /// <summary>
        /// Gets if this property has a read accessor or not.
        /// </summary>
        public bool CanRead =>
            ReaderBody != null || (access & PropertyAccess.Read) != PropertyAccess.None;

        /// <summary>
        /// Gets if this property has a write accessor or not.
        /// </summary>
        public bool CanWrite =>
            WriterBody != null || (access & PropertyAccess.Write) != PropertyAccess.None;

        /// <summary>
        /// Gets if this property is an indexer or not.
        /// </summary>
        public bool IsIndexer => Name == ClassProperty.INDEXER_NAME;

        /// <summary>
        /// Gets if the property being declared has the same signature than an existing one.
        /// </summary>
        /// <param name="property">The existing property</param>
        /// <returns><b>true</b> if both property have the same scope and prototype. <b>false</b> otherwise</returns>
        public bool MatchesSignature(ClassProperty property)
        {
            return Name == property.Name &&
                   Scope == property.Scope &&
                   CanRead == property.CanRead &&
                   CanWrite == property.CanWrite;
        }

        /// <summary>
        /// Creates a <see cref="ClassMember"/> from this instance.
        /// </summary>
        public override ClassMember ToClassMember() =>
            new ClassProperty(Name, Scope, Modifier, Access, ReaderScope, ReaderBody, WriterScope, WriterBody);
    }
}
