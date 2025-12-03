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
    /// <param name="reader">The property's read accessor</param>
    /// <param name="writer">The property's write accessor</param>
    public ClassProperty(string name, Scope scope, Modifier modifier, ClassMethod reader, ClassMethod writer)
        : base(name, scope, modifier)
    {
        Reader = reader;
        Writer = writer;
    }

    /// <summary>
    /// Initializes an instance of ClassProperty that encapsulates a field.
    /// </summary>
    /// <param name="name">The property's name</param>
    /// <param name="scope">The property's scope; it may be <b>private</b>, <b>protected</b> or <b>public</b></param>
    /// <param name="modifier">property's modifier; it may be <b>static</b>, <b>final</b>, <b>abstract</b> or nothing</param>
    /// <param name="fieldName">The backing field's name</param>
    /// <param name="access">Determines which accessors to generate</param>
    /// <param name="readerScope">The read accessor's scope</param>
    /// <param name="writerScope">The write accessor's scope</param>
    public ClassProperty(string name, Scope scope, Modifier modifier, string fieldName,
                         PropertyAccess access, Scope readerScope, Scope writerScope)
        : base(name, scope, modifier)
    {
        BackingFieldName = fieldName;
        Access = access;
        ReaderScope = readerScope;
        WriterScope = writerScope;
    }

    /// <summary>
    /// Initializes an instance of ClassProperty that encapsulates a field.
    /// </summary>
    /// <param name="name">The property's name</param>
    /// <param name="scope">The property's scope; it may be <b>private</b>, <b>protected</b> or <b>public</b></param>
    /// <param name="modifier">property's modifier; it may be <b>static</b>, <b>final</b>, <b>abstract</b> or nothing</param>
    /// <param name="fieldName">The backing field's name</param>
    /// <param name="access">Determines which accessors to generate</param>
    public ClassProperty(string name, Scope scope, Modifier modifier, string fieldName, PropertyAccess access)
        : this(name, scope, modifier, fieldName, access, scope, scope)
    {
    }

    /// <summary>
    /// Initializes an instance of ClassProperty that encapsulates a field.
    /// </summary>
    /// <param name="name">The property's name</param>
    /// <param name="scope">The property's scope; it may be <b>private</b>, <b>protected</b> or <b>public</b></param>
    /// <param name="modifier">property's modifier; it may be <b>static</b>, <b>final</b>, <b>abstract</b> or nothing</param>
    /// <param name="access">Determines which accessors to generate</param>
    /// <param name="readerScope">The read accessor's scope</param>
    /// <param name="writerScope">The write accessor's scope</param>
    public ClassProperty(string name, Scope scope, Modifier modifier,
                         PropertyAccess access, Scope readerScope, Scope writerScope)
        : this(name, scope, modifier, "__" + name, access, readerScope, writerScope)
    {
        IsAuto = true;
    }

    /// <summary>
    /// The property's read accessor.
    /// </summary>
    public ClassMethod Reader { get; private set; }

    /// <summary>
    /// The property's write accessor.
    /// </summary>
    public ClassMethod Writer { get; private set; }

    /// <summary>
    /// Gets if the property has automatically generated accessors and backing field or not.
    /// </summary>
    public bool IsAuto { get; private set; }

    /// <summary>
    /// Gets the name that should be assigned to an eventually generated backing field.
    /// </summary>
    public string BackingFieldName { get; private set; }

    /// <summary>
    /// Determines which accessors to generate when they are not explicitly provided.
    /// </summary>
    public PropertyAccess Access { get; private set; }

    /// <summary>
    /// The scope of an eventually generated read aacessor.
    /// </summary>
    public Scope ReaderScope { get; private set; }

    /// <summary>
    /// The scope of an eventually generated write aacessor.
    /// </summary>
    public Scope WriterScope { get; private set; }

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
    public static string GetReaderName(string name)
    {
        return "__read_" + name;
    }

    /// <summary>
    /// Creates a suitable name for a write-accessor, given the property's name.
    /// </summary>
    /// <param name="name">The property's name</param>
    /// <returns>A <see cref="string"/></returns>
    public static string GetWriterName(string name)
    {
        return "__write_" + name;
    }

    /// <summary>
    /// Generates accessors for an auto property.
    /// </summary>
    public void GenerateAccessors()
    {
        if ((Access & PropertyAccess.Read) != PropertyAccess.None)
            Reader = CreateReader();

        if ((Access & PropertyAccess.Write) != PropertyAccess.None)
            Writer = CreateWriter();
    }

    /// <summary>
    /// Automatically creates a reader for a field
    /// </summary>
    /// <returns>A <see cref="ClassMethod"/></returns>
    private ClassMethod CreateReader()
    {
        Block readerBody = null;
        switch (Modifier)
        {
            case Modifier.Abstract:
                break;
            case Modifier.Static:
                var qName = new QualifiedName(Holder.Name, BackingFieldName);
                readerBody = Block.WithReturn(new StaticPropertyRef(qName));
                break;
            default:
                readerBody = Block.WithReturn(PropertyRef.This(BackingFieldName));
                break;
        }

        var function = new Function([], readerBody);

        return new ClassMethod(GetReaderName(Name), ReaderScope, Modifier, function);
    }

    /// <summary>
    /// Automatically creates a writer for a field
    /// </summary>
    /// <returns>A <see cref="ClassMethod"/></returns>
    private ClassMethod CreateWriter()
    {
        Block writerBody = null;
        switch (Modifier)
        {
            case Modifier.Abstract:
                break;
            case Modifier.Static:
                var qName = new QualifiedName(Holder.Name, BackingFieldName);
                writerBody = new Block(new Assignment(new StaticPropertyRef(qName),
                                                      new VariableRef(WRITER_PARAMETER_NAME)),
                                       new Return());
                break;
            default:
                writerBody = new Block(new Assignment(PropertyRef.This(BackingFieldName),
                                                      new VariableRef(WRITER_PARAMETER_NAME)),
                                       new Return());
                break;
        }

        var function = new Function([new Parameter(WRITER_PARAMETER_NAME)], writerBody);

        return new ClassMethod(GetWriterName(Name), WriterScope, Modifier, function);
    }
}
