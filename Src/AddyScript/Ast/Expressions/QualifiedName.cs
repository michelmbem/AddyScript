using System;
using System.IO;


namespace AddyScript.Ast.Expressions
{
    /// <summary>
    /// Represents a name like A::B::C
    /// </summary>
    public class QualifiedName : IComparable, IComparable<QualifiedName>, IEquatable<QualifiedName>
    {
        private readonly string[] parts;
        private readonly int startIndex;
        private readonly int length;

        /// <summary>
        /// Initializes an instance of QualifiedName.
        /// </summary>
        /// <param name="parts">The parts of the name</param>
        /// <param name="startIndex">The index of the initial part of the name</param>
        /// <param name="length">The length of the name</param>
        protected QualifiedName(string[] parts, int startIndex, int length)
        {
            this.parts = parts;
            this.startIndex = startIndex;
            this.length = length;
        }

        /// <summary>
        /// Initializes a new instance of QualifiedName
        /// </summary>
        /// <param name="parts">The parts of the name</param>
        public QualifiedName(params string[] parts)
        {
            this.parts = parts;
            startIndex = 0;
            length = parts.Length;
        }

        /// <summary>
        /// Gets the part at the specified index.
        /// </summary>
        public string this[int index]
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
        public int Length
        {
            get { return length; }
        }

        /// <summary>
        /// A factory method that creates qualified names from a string which may contain a separator.
        /// </summary>
        /// <param name="str">The input string</param>
        /// <param name="separator">The separator</param>
        /// <returns>A <see cref="QualifiedName"/></returns>
        public static QualifiedName Parse(string str, string separator)
        {
            return new QualifiedName(str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// A factory method that creates qualified names from .Net qualified names.
        /// </summary>
        /// <param name="value">The input string</param>
        /// <returns>A <see cref="QualifiedName"/></returns>
        public static QualifiedName Parse(string value)
        {
            return Parse(value, "::");
        }

        /// <summary>
        /// A factory method that creates qualified names from dotted name.
        /// </summary>
        /// <param name="dottedName">The input string</param>
        /// <returns>A <see cref="QualifiedName"/></returns>
        public static QualifiedName ParseDottedName(string dottedName)
        {
            return Parse(dottedName, ".");
        }

        /// <summary>
        /// A factory method that creates qualified names from file names.
        /// </summary>
        /// <param name="path">The input string</param>
        /// <returns>A <see cref="QualifiedName"/></returns>
        public static QualifiedName ParsePath(string path)
        {
            return Parse(path, Path.DirectorySeparatorChar.ToString());
        }

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
        public QualifiedName Prepend(string part)
        {
            var newParts = new string[length + 1];
            newParts[0] = part;
            Array.Copy(parts, startIndex, newParts, 1, length);

            return new QualifiedName(newParts);
        }

        /// <summary>
        /// Apppends a part to a qualified name.
        /// </summary>
        /// <param name="part">The part to append</param>
        /// <returns>A <see cref="QualifiedName"/></returns>
        public QualifiedName Apppend(string part)
        {
            var newParts = new string[length + 1];
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
        public QualifiedName Insert(int index, string part)
        {
            while (index < 0) index += length;

            var newParts = new string[length + 1];
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

            var newParts = new string[length - count];
            Array.Copy(parts, startIndex, newParts, 0, index);
            Array.Copy(parts, startIndex + index + count, newParts, index, length - index - count);

            return new QualifiedName(newParts);
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />. 
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
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj" />. Zero This instance is equal to <paramref name="obj" />. Greater than zero This instance is greater than <paramref name="obj" />. 
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param>
        /// <exception cref="T:System.ArgumentException"><paramref name="obj" /> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
        public int CompareTo(object obj)
        {
            if (obj is QualifiedName) return CompareTo((QualifiedName) obj);
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
            if (length != other.length) return false;

            for (int i = 0; i < length; ++i)
                if (this[i] != other[i])
                    return false;

            return true;
        }

        ///<summary>
        ///Determines whether the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />.
        ///</summary>
        ///
        ///<returns>
        ///true if the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />; otherwise, false.
        ///</returns>
        ///
        ///<param name="obj">The <see cref="T:System.Object" /> to compare with the current <see cref="T:System.Object" />. </param>
        ///<exception cref="T:System.NullReferenceException">The <paramref name="obj" /> parameter is null.</exception><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            return (obj is QualifiedName) && Equals((QualifiedName) obj);
        }

        ///<summary>
        ///Serves as a hash function for a particular type. 
        ///</summary>
        ///
        ///<returns>
        ///A hash code for the current <see cref="T:System.Object" />.
        ///</returns>
        ///<filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return ToString(string.Empty).GetHashCode();
        }

        /// <summary>
        /// Converts a QualifiedName to a string using the given separator
        /// </summary>
        /// <param name="separator">The separator to be used</param>
        /// <returns>A string</returns>
        public string ToString(string separator)
        {
            return string.Join(separator, parts, startIndex, length);
        }

        ///<summary>
        ///Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
        ///</returns>
        ///<filterpriority>2</filterpriority>
        public override string ToString()
        {
            return ToString("::");
        }

        /// <summary>
        /// Converts a QualifiedName to dotted name.
        /// </summary>
        /// <returns>A string</returns>
        public string ToDottedName()
        {
            return ToString(".");
        }

        /// <summary>
        /// Converts a QualifiedName to file path.
        /// </summary>
        /// <returns>A string</returns>
        public string ToFilePath()
        {
            return ToString(Path.DirectorySeparatorChar.ToString());
        }
    }
}
