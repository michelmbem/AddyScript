using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;


namespace AddyScript.Runtime.OOP;


/// <summary>
/// Represents a property in a class.
/// </summary>
public class ClassProperty : ClassMember
{
    public const string INDEXER_NAME = "[item]";
    public const string WRITER_PARAMETER_NAME = "__value";

    /// <summary>
    /// Initializes a new instance of ClassProperty.
    /// </summary>
    /// <param name="name">The property's name</param>
    /// <param name="scope">The property's scope; it may be <b>private</b>, <b>protected</b> or <b>public</b></param>
    /// <param name="modifier">property's modifier; it may be <b>static</b>, <b>final</b>, <b>abstract</b> or nothing</param>
    /// <param name="access">The desired property access mode. Used for automatic accessors generation</param>
    /// <param name="readerScope">The read accessor scope</param>
    /// <param name="readerBody">The read accessor body</param>
    /// <param name="writerScope">The write accessor scope</param>
    /// <param name="writerBody">The write accessor body</param>
    public ClassProperty(string name, Scope scope, Modifier modifier, PropertyAccess access,
                         Scope readerScope, Block readerBody, Scope writerScope, Block writerBody) :
        base(name, scope, modifier)
    {
        if (readerBody != null || (access & PropertyAccess.Read) != PropertyAccess.None)
        {
            Parameter[] readerParameters = IsIndexer ? [new(ForEachLoop.DEFAULT_KEY_NAME)] : [];
            Reader = new ClassMethod(GetReaderName(name), readerScope, modifier, new (readerParameters, readerBody));
        }

        if (writerBody == null && (access & PropertyAccess.Write) == PropertyAccess.None) return;
        
        Parameter[] writerParameters = IsIndexer
            ? [new(ForEachLoop.DEFAULT_KEY_NAME), new(WRITER_PARAMETER_NAME)]
            : [new(WRITER_PARAMETER_NAME)];

        Writer = new ClassMethod(GetWriterName(name), writerScope, modifier, new(writerParameters, writerBody));
    }

    /// <summary>
    /// Initializes a new instance of ClassProperty.
    /// </summary>
    /// <param name="name">The property's name</param>
    /// <param name="scope">The property's scope; it may be <b>private</b>, <b>protected</b> or <b>public</b></param>
    /// <param name="modifier">property's modifier; it may be <b>static</b>, <b>final</b>, <b>abstract</b> or nothing</param>
    /// <param name="readerBody">The read accessor body</param>
    /// <param name="writerBody">The write accessor body</param>
    public ClassProperty(string name, Scope scope, Modifier modifier, Block readerBody, Block writerBody = null) :
        this(name, scope, modifier, PropertyAccess.None, scope, readerBody, scope, writerBody)
    {
    }

    /// <summary>
    /// Initializes a new instance of ClassProperty.
    /// </summary>
    /// <param name="name">The property's name</param>
    /// <param name="scope">The property's scope; it may be <b>private</b>, <b>protected</b> or <b>public</b></param>
    /// <param name="modifier">property's modifier; it may be <b>static</b>, <b>final</b>, <b>abstract</b> or nothing</param>
    /// <param name="access">The desired property access mode. Used for automatic accessors generation</param>
    public ClassProperty(string name, Scope scope, Modifier modifier, PropertyAccess access = PropertyAccess.ReadWrite) :
        this(name, scope, modifier, access, scope, null, scope, null)
    {
    }

    /// <summary>
    /// The property's read accessor.
    /// </summary>
    public ClassMethod Reader { get;  }

    /// <summary>
    /// The property's write accessor.
    /// </summary>
    public ClassMethod Writer { get; }

    /// <summary>
    /// Gets if this property has a read accessor or not.
    /// </summary>
    public bool CanRead => Reader != null;

    /// <summary>
    /// Gets if this property has a write accessor or not.
    /// </summary>
    public bool CanWrite => Writer != null;

    /// <summary>
    /// Gets if this property is an indexer or not.
    /// </summary>
    public bool IsIndexer => Name == INDEXER_NAME;

    /// <summary>
    /// Creates a suitable name for a read-accessor, given the property's name.
    /// </summary>
    /// <param name="name">The property's name</param>
    /// <returns>A <see cref="string"/></returns>
    public static string GetReaderName(string name) => $"__read_{name}";

    /// <summary>
    /// Creates a suitable name for a write-accessor, given the property's name.
    /// </summary>
    /// <param name="name">The property's name</param>
    /// <returns>A <see cref="string"/></returns>
    public static string GetWriterName(string name) => $"__write_{name}";

    /// <summary>
    /// Handles automatic accessors logic generation.
    /// </summary>
    public bool GenerateAccessors(out string backingFieldName)
    {
        bool generated = false;
        backingFieldName = $"__{Name}";

        if (Reader != null && Reader.Function.Body == null)
        {
            Reader.Function.Body = GenerateReaderBody(backingFieldName);
            generated = true;
        }

        if (Writer != null && Writer.Function.Body == null)
        {
            Writer.Function.Body = GenerateWriterBody(backingFieldName);
            generated = true;
        }

        return generated;
    }

    /// <summary>
    /// Generates the read accessor's body.
    /// </summary>
    /// <param name="backingFieldName">The backing field name</param>
    /// <returns>A <see cref="Block"/></returns>
    private Block GenerateReaderBody(string backingFieldName)
    {
        switch (Modifier)
        {
            case Modifier.Abstract:
                return null;
            case Modifier.Static:
            {
                var qName = new QualifiedName(Holder.Name, backingFieldName);
                return Block.WithReturn(new StaticPropertyRef(qName));
            }
            default:
                return Block.WithReturn(PropertyRef.OfSelf(backingFieldName));
        }
    }

    /// <summary>
    /// Generates the write accessor's body.
    /// </summary>
    /// <param name="backingFieldName">The backing field name</param>
    /// <returns>A <see cref="Block"/></returns>
    private Block GenerateWriterBody(string backingFieldName)
    {
        switch (Modifier)
        {
            case Modifier.Abstract:
                return null;
            case Modifier.Static:
            {
                var qName = new QualifiedName(Holder.Name, backingFieldName);
                return new Block(new Assignment(new StaticPropertyRef(qName),
                                                new VariableRef(WRITER_PARAMETER_NAME)),
                                 new Return());
            }
            default:
                return new Block(new Assignment(PropertyRef.OfSelf(backingFieldName),
                                                new VariableRef(WRITER_PARAMETER_NAME)),
                                 new Return());
        }
    }
}
