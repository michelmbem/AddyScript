using System;
using System.Text;
using System.Text.RegularExpressions;

using AddyScript.Runtime.DataItems;


namespace AddyScript.Ast.Expressions;


/// <summary>
/// Represents a part in a name like A::B::C i.e.: one of A, B, or C.
/// </summary>
/// <remarks>
/// Initializes an instance of NamePart.
/// </remarks>
/// <param name="value">The value of this part</param>
/// <param name="paramCount">The number of eventual type parameters</param>
public partial class NamePart(string value, int paramCount = 0)
    : IComparable, IComparable<NamePart>, IEquatable<NamePart>
{
    private static readonly string GenericTypeArgument = typeof(DataItem).AssemblyQualifiedName;

    private readonly string value = value;
    private readonly int paramCount = paramCount;

    /// <summary>
    /// Gets the value of this part.
    /// </summary>
    public string Value => value;

    /// <summary>
    /// Gets the value of this part.
    /// </summary>
    public int ParamCount => paramCount;

    [GeneratedRegex(@"^(?<VALUE>\w+)(\s*\{\s*(?<PARAMCOUNT>\d+)\s*\}\s*)?$", RegexOptions.Compiled)]
    private static partial Regex GetASNamePartRegex();

    [GeneratedRegex(@"^(?<VALUE>\w+)(`(?<PARAMCOUNT>\d+))?$", RegexOptions.Compiled)]
    private static partial Regex GetNativeNamePartRegex();

    /// <summary>
    /// A factory method that creates qualified name parts from strings.
    /// </summary>
    /// <param name="str">The input string</param>
    /// <param name="useNativeSyntax">Tells if <paramref name="str"/> uses the dotnet native syntax or not</param>
    /// <returns>A <see cref="NamePart"/></returns>
    public static NamePart Parse(string str, bool useNativeSyntax)
    {
        Regex namePartRegex = useNativeSyntax ? GetNativeNamePartRegex() : GetASNamePartRegex();
        Match match = namePartRegex.Match(str);
        if (!match.Success) throw new FormatException();

        Group valueGroup = match.Groups[namePartRegex.GroupNumberFromName("VALUE")];
        Group paramCountGroup = match.Groups[namePartRegex.GroupNumberFromName("PARAMCOUNT")];

        return paramCountGroup.Success
            ? new NamePart(valueGroup.Value, int.Parse(paramCountGroup.Value))
            : new NamePart(valueGroup.Value);
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
    public int CompareTo(NamePart other)
    {
        int valueCmp = string.CompareOrdinal(value, other.value);
        return valueCmp == 0 ? paramCount.CompareTo(other.ParamCount) : valueCmp;
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
    /// <exception cref="ArgumentException"><paramref name="obj" /> is different type as this instance.</exception>
    /// <filterpriority>2</filterpriority>
    public int CompareTo(object obj)
    {
        if (obj is NamePart namePart) return CompareTo(namePart);
        throw new ArgumentException("obj must be a NamePart");
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
    /// <param name="other">An object to compare with this object.</param>
    public bool Equals(NamePart other)
    {
        if (other == null) return false;
        return Equals(value, other.value) && paramCount == other.paramCount;
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>true if the current object is equal to the <paramref name="obj" /> parameter; otherwise, false.</returns>
    /// <param name="obj">An object to compare with this object.</param>
    public override bool Equals(object obj) => obj is NamePart other && Equals(other);

    /// <summary>
    /// Serves as a hash function for a particular type. 
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() => HashCode.Combine(value, paramCount);

    /// <summary>
    /// Returns a string that represents the current <see cref="NamePart" />.
    /// </summary>
    /// <returns>A <see cref="System.String" /></returns>
    /// <param name="useNativeSyntax">Should the dotnet native syntax be used to represent generic type parameters?</param>
    /// <param name="expandTypeArguments">Should type parameters be expanded if the dotnet native syntax is used?</param>
    public string ToString(bool useNativeSyntax, bool expandTypeArguments)
    {
        StringBuilder sb = new (value);

        if (paramCount > 0)
        {
            if (useNativeSyntax)
            {
                sb.Append('`').Append(paramCount);

                if (expandTypeArguments)
                {
                    sb.Append('[');
                    for (int i = 0; i < paramCount; ++i)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append('[').Append(GenericTypeArgument).Append(']');
                    }
                    sb.Append(']');
                }
            }
            else
                sb.Append('{').Append(paramCount).Append('}');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a string that represents the current <see cref="NamePart" />.
    /// </summary>
    /// <returns>A <see cref="System.String" /></returns>
    public override string ToString() => ToString(false, false);
    
    #region Operators

    public static bool operator ==(NamePart a, NamePart b) => Equals(a, b);

    public static bool operator !=(NamePart a, NamePart b) => !Equals(a, b);

    public static bool operator <(NamePart a, NamePart b) => a.CompareTo(b) < 0;

    public static bool operator >(NamePart a, NamePart b) => a.CompareTo(b) > 0;

    public static bool operator <=(NamePart a, NamePart b) => a.CompareTo(b) <= 0;

    public static bool operator >=(NamePart a, NamePart b) => a.CompareTo(b) >= 0;
    
    #endregion
}