using System;

using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime
{
    /// <summary>
    /// An abstraction of a static type's member managed by reflection.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of StaticTypeMember
    /// </remarks>
    /// <param name="type">A CLR type</param>
    /// <param name="memberName">The name of a type's member</param>
    public class StaticTypeMember(Type type, string memberName)
    {

        /// <summary>
        /// A CLR type.
        /// </summary>
        public Type Type { get; private set; } = type;

        /// <summary>
        /// The name of a type's member.
        /// </summary>
        public string MemberName { get; private set; } = memberName;

        /// <summary>
        /// Gets the value of a field or property.
        /// </summary>
        /// <returns>A <see cref="DataItem"/></returns>
        public DataItem GetValue()
        {
            return Reflector.GetValue(Type, MemberName, null);
        }

        /// <summary>
        /// Sets the value of a field or property.
        /// </summary>
        /// <param name="value">The value to set</param>
        public void SetValue(DataItem value)
        {
            Reflector.SetValue(Type, MemberName, null, value);
        }
    }
}
