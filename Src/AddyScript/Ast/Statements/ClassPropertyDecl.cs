using AddyScript.Runtime;
using AddyScript.Runtime.OOP;


namespace AddyScript.Ast.Statements
{
    /// <summary>
    /// Represents the declaration of class property.
    /// </summary>
    public class ClassPropertyDecl : ClassMemberDecl
    {
        /// <summary>
        /// Initializes a new instance of ClassPropertyDecl.
        /// </summary>
        /// <param name="name">The property's name</param>
        /// <param name="scope">The scope of this property</param>
        /// <param name="modifier">Determines whether this property is abstract, final, static or not</param>
        /// <param name="readerBody">The property's read accessor's body</param>
        /// <param name="writerBody">The property's write accessor's body</param>
        public ClassPropertyDecl(string name, Scope scope, Modifier modifier, Block readerBody, Block writerBody)
            : base(name, scope, modifier)
        {
            ReaderBody = readerBody;
            WriterBody = writerBody;
        }

        /// <summary>
        /// Initializes a new instance of ClassPropertyDecl.
        /// </summary>
        /// <param name="name">The property's name</param>
        /// <param name="scope">The property's scope; it may be <b>private</b>, <b>protected</b> or <b>public</b></param>
        /// <param name="modifier">property's modifier; it may be <b>static</b>, <b>final</b>, <b>abstract</b> or nothing</param>
        /// <param name="access">Determines which accessors to generate</param>
        public ClassPropertyDecl(string name, Scope scope, Modifier modifier, PropertyAccess access)
            : base(name, scope, modifier)
        {
            IsAuto = true;
            Access = access;
        }

        /// <summary>
        /// The property's read accessor.
        /// </summary>
        public Block ReaderBody { get; private set; }

        /// <summary>
        /// The property's write accessor.
        /// </summary>
        public Block WriterBody { get; private set; }

        /// <summary>
        /// Gets if the property has automatically generated accessors and backing field or not.
        /// </summary>
        public bool IsAuto { get; private set; }

        /// <summary>
        /// Determines which accessors to generate when they are not explicitly provided.
        /// </summary>
        public PropertyAccess Access { get; private set; }

        /// <summary>
        /// The scope of an eventually generated read aacessor.
        /// </summary>
        public Scope ReaderScope { get; set; }

        /// <summary>
        /// The scope of an eventually generated write aacessor.
        /// </summary>
        public Scope WriterScope { get; set; }

        /// <summary>
        /// Gets if this property has a read accessor or not.
        /// </summary>
        public bool CanRead => ReaderBody != null;

        /// <summary>
        /// Gets if this property has a write accessor or not.
        /// </summary>
        public bool CanWrite => WriterBody != null;

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
        public override ClassMember ToClassMember()
        {
            ClassProperty property;

            if (IsAuto || Modifier == Modifier.Abstract)
                property = new ClassProperty(Name, Scope, Modifier, Access, ReaderScope, WriterScope);
            else
            {
                ClassMethod reader = null, writer = null;

                if (ReaderBody != null)
                {
                    Parameter[] readerParameters = IsIndexer
                                                 ? [new Parameter(ForEachLoop.DEFAULT_KEY_NAME)]
                                                 : [];

                    reader = new ClassMethod(ClassProperty.GetReaderName(Name), Scope, Modifier,
                                             new Function(readerParameters, ReaderBody));
                }

                if (WriterBody != null)
                {
                    Parameter[] writerParameters = IsIndexer
                                                 ? [new Parameter(ForEachLoop.DEFAULT_KEY_NAME),
                                                    new Parameter(ClassProperty.WRITER_PARAMETER_NAME)]
                                                 : [new Parameter(ClassProperty.WRITER_PARAMETER_NAME)];

                    writer = new ClassMethod(ClassProperty.GetWriterName(Name), Scope, Modifier,
                                             new Function(writerParameters, WriterBody));
                }

                property = new ClassProperty(Name, Scope, Modifier, reader, writer);
            }

            return property;
        }
    }
}
