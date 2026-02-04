using System;
using System.IO;
using System.Linq;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a composite name like A::B::C
/// </summary>
public sealed class QualifiedName : IComparable, IComparable<QualifiedName>, IEquatable<QualifiedName>
{
    private readonly NamePart[] parts;
    private readonly int startIndex;
    private readonly int length;

    /// <summary>
    /// Initializes an instance of QualifiedName.
    /// </summary>
    /// <param name="parts">The parts of the name</param>
    /// <param name="startIndex">The index of the initial part of the name</param>
    /// <param name="length">The length of the name</param>
    private QualifiedName(NamePart[] parts, int startIndex, int length)
    {
        this.parts = parts;
        this.startIndex = startIndex;
        this.length = length;
    }

    /// <summary>
    /// Initializes a new instance of QualifiedName
    /// </summary>
    /// <param name="parts">The parts of the name</param>
    public QualifiedName(params NamePart[] parts) : this(parts, 0, parts.Length) { }

    /// <summary>
    /// Initializes a new instance of QualifiedName
    /// </summary>
    /// <param name="parts">The parts of the name given as strings</param>
    public QualifiedName(params string[] parts)
        : this([..parts.Select(s => new NamePart(s))]) { }

    /// <summary>
    /// Gets the part at the specified index.
    /// </summary>
    public NamePart this[int index]
    {
        get
        {
            while (index < 0) index += length;
            return parts[startIndex + index];
        }
    }

    /// <summary>
    /// Gets the number of parts that the name is made of.
    /// </summary>
    public int Length => length;

    /// <summary>
    /// Gets if this name is a simple identifier.
    /// </summary>
    public bool IsIdentifier => length == 1 && parts[startIndex].ParamCount <= 0;

    /// <summary>
    /// A factory method that creates qualified names from a string which may contain a separator.
    /// </summary>
    /// <param name="str">The input string</param>
    /// <param name="separator">The separator</param>
    /// <param name="useNativeSyntax">Tells if <paramref name="str"/> uses the dotnet native syntax or not</param>
    /// <returns>A <see cref="QualifiedName"/></returns>
    public static QualifiedName Parse(string str, string separator = "::", bool useNativeSyntax = false)
    {
        var strings = str.Split([separator], StringSplitOptions.RemoveEmptyEntries);
        return new QualifiedName(strings.Select(s => NamePart.Parse(s, useNativeSyntax)).ToArray());
    }

    /// <summary>
    /// A factory method that creates qualified names from dotted name.
    /// </summary>
    /// <param name="dottedName">The input string. It's supposed to represent a generic .Net type's name</param>
    /// <returns>A <see cref="QualifiedName"/></returns>
    public static QualifiedName ParseDottedName(string dottedName)
    {
        // Remove any type argument expansion from the given name
        if (dottedName.EndsWith(']'))
            dottedName = dottedName[..dottedName.IndexOf('[')];

        return Parse(dottedName.Replace('+', '.'), ".", true);
    }

    /// <summary>
    /// A factory method that creates qualified names from file names.
    /// </summary>
    /// <param name="path">The input string</param>
    /// <returns>A <see cref="QualifiedName"/></returns>
    public static QualifiedName ParsePath(string path) => Parse(path, Path.DirectorySeparatorChar.ToString(), false);

    /// <summary>
    /// Extracts a part of a qualified name.
    /// </summary>
    /// <param name="start">The starting index of the part to be extracted</param>
    /// <param name="cnt">The length of the part to be extracted</param>
    /// <returns>A <see cref="QualifiedName"/></returns>
    public QualifiedName Subname(int start, int cnt)
    {
        while (start < 0) start += length;
        while (cnt < 0) cnt += length;

        return new QualifiedName(parts, startIndex + start, cnt);
    }

    /// <summary>
    /// Extracts a part of a qualified name.
    /// </summary>
    /// <param name="start">The starting index of the part to be extracted</param>
    /// <returns>A <see cref="QualifiedName"/></returns>
    public QualifiedName Subname(int start)
    {
        while (start < 0) start += length;
        return new QualifiedName(parts, startIndex + start, length - startIndex - start);
    }

    /// <summary>
    /// Prepends a part to a qualified name.
    /// </summary>
    /// <param name="part">The part to prepend</param>
    /// <returns>A <see cref="QualifiedName"/></returns>
    public QualifiedName Prepend(NamePart part)
    {
        var newParts = new NamePart[length + 1];
        newParts[0] = part;
        Array.Copy(parts, startIndex, newParts, 1, length);

        return new QualifiedName(newParts);
    }

    /// <summary>
    /// Apppends a part to a qualified name.
    /// </summary>
    /// <param name="part">The part to append</param>
    /// <returns>A <see cref="QualifiedName"/></returns>
    public QualifiedName Apppend(NamePart part)
    {
        var newParts = new NamePart[length + 1];
        Array.Copy(parts, startIndex, newParts, 0, length);
        newParts[length] = part;

        return new QualifiedName(newParts);
    }

    /// <summary>
    /// Inserts a part to a qualified name.
    /// </summary>
    /// <param name="index">The index where to insert</param>
    /// <param name="part">The part to prepend</param>
    /// <returns>A <see cref="QualifiedName"/></returns>
    public QualifiedName Insert(int index, NamePart part)
    {
        while (index < 0) index += length;

        var newParts = new NamePart[length + 1];
        Array.Copy(parts, startIndex, newParts, 0, index);
        newParts[index] = part;
        Array.Copy(parts, startIndex + index + 1, newParts, index + 1, length - index);

        return new QualifiedName(newParts);
    }

    /// <summary>
    /// Removes some parts from a qualified name.
    /// </summary>
    /// <param name="index">The index from which to remove</param>
    /// <param name="count">The number of parts to be removed</param>
    /// <returns>A <see cref="QualifiedName"/></returns>
    public QualifiedName Remove(int index, int count)
    {
        while (index < 0) index += length;
        while (count < 0) count += length;

        var newParts = new NamePart[length - count];
        Array.Copy(parts, startIndex, newParts, 0, index);
        Array.Copy(parts, startIndex + index + count, newParts, index, length - index - count);

        return new QualifiedName(newParts);
    }

    /// <summary>
    /// Compares the current object with another object of the same type.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that indicates the relative order of the objects being compared.
    /// The return value has these meanings:
    /// Less than zero: this instance is less than <paramref name="other" />.
    /// Zero: this instance is equal to <paramref name="other" />.
    /// Greater than zero: this instance is greater than <paramref name="other" />.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public int CompareTo(QualifiedName other)
    {
        int minCount = Math.Min(length, other.length);

        for (int i = 0; i < minCount; ++i)
        {
            int tmp = this[i].CompareTo(other[i]);
            if (tmp != 0) return tmp;
        }

        return length - other.length;
    }

    /// <summary>
    /// Compares the current object with another object of the same type.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that indicates the relative order of the objects being compared.
    /// The return value has these meanings:
    /// Less than zero: this instance is less than <paramref name="obj" />.
    /// Zero: this instance is equal to <paramref name="obj" />.
    /// Greater than zero: this instance is greater than <paramref name="obj" />.
    /// </returns>
    /// <param name="obj">An object to compare with this object.</param>
    /// <exception cref="ArgumentException"><paramref name="obj" /> is not the same type as this instance.</exception>
    /// <filterpriority>2</filterpriority>
    public int CompareTo(object obj)
    {
        if (obj is QualifiedName qName) return CompareTo(qName);
        throw new ArgumentException("obj must be a QualifiedName");
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public bool Equals(QualifiedName other)
    {
        if (length != other!.length) return false;

        for (int i = 0; i < length; ++i)
            if (this[i] != other[i])
                return false;

        return true;
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>
    /// true if the current object is equal to the <paramref name="obj" /> parameter; otherwise, false.
    /// </returns>
    /// <param name="obj">An object to compare with this object.</param>
    public override bool Equals(object obj) => obj is QualifiedName qName && Equals(qName);

    /// <summary>
    /// Serves as a hash function for a particular type. 
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        int hash = 1;

        for (int i = 0; i < length; ++i)
            hash = 23 * hash + this[i].GetHashCode();

        return hash;
    }

    /// <summary>
    /// Converts a QualifiedName to a string.
    /// </summary>
    /// <param name="separator">The separator to be used</param>
    /// <param name="useNativeSyntax">Should the dotnet native syntax be used to represent generic type parameters?</param>
    /// <param name="expandTypeArguments">Should type parameters be expanded if the dotnet native syntax is used?</param>
    /// <returns>A string</returns>
    public string ToString(string separator, bool useNativeSyntax, bool expandTypeArguments)
    {
        string[] strings = parts.Skip(startIndex)
                                .Take(length)
                                .Select(p => p.ToString(useNativeSyntax, expandTypeArguments))
                                .ToArray();

        return string.Join(separator, strings);
    }

    /// <summary>
    /// Converts a QualifiedName to a string.
    /// </summary>
    /// <returns>A string</returns>
    public override string ToString() => ToString("::", false, false);

    /// <summary>
    /// Converts a QualifiedName to a string using the .Net's dotted syntax.
    /// </summary>
    /// <param name="expandTypeArguments">Should type parameters be expanded if the dotnet native syntax is used?</param>
    /// <returns>A string</returns>
    public string ToDottedName(bool expandTypeArguments) => ToString(".", true, expandTypeArguments);

    /// <summary>
    /// Converts a QualifiedName to a string representing a file path.
    /// </summary>
    /// <returns>A string</returns>
    public string ToFilePath() => ToString(Path.DirectorySeparatorChar.ToString(), false, false);

    #region Operators

    public static bool operator ==(QualifiedName a, QualifiedName b) => Equals(a, b);

    public static bool operator !=(QualifiedName a, QualifiedName b) => !Equals(a, b);

    public static bool operator <(QualifiedName a, QualifiedName b) => a.CompareTo(b) < 0;

    public static bool operator >(QualifiedName a, QualifiedName b) => a.CompareTo(b) > 0;

    public static bool operator <=(QualifiedName a, QualifiedName b) => a.CompareTo(b) <= 0;

    public static bool operator >=(QualifiedName a, QualifiedName b) => a.CompareTo(b) >= 0;

    #endregion
}