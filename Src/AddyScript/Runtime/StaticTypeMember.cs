using System;

using AddyScript.Runtime.Dynamics;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime
{
    /// <summary>
    /// An abstraction of a static type's member managed by reflection.
    /// </summary>
    public class StaticTypeMember
    {
        /// <summary>
        /// Initializes a new instance of StaticTypeMember
        /// </summary>
        /// <param name="type">A CLR type</param>
        /// <param name="memberName">The name of a type's member</param>
        public StaticTypeMember(Type type, string memberName)
        {
            Type = type;
            MemberName = memberName;
        }

        /// <summary>
        /// A CLR type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The name of a type's member.
        /// </summary>
        public string MemberName { get; private set; }

        /// <summary>
        /// Gets the value of a field or property.
        /// </summary>
        /// <returns>A <see cref="Dynamic"/></returns>
        public Dynamic GetValue()
        {
            return Reflector.GetValue(Type, MemberName, null);
        }

        /// <summary>
        /// Sets the value of a field or property.
        /// </summary>
        /// <param name="value">The value to set</param>
        public void SetValue(Dynamic value)
        {
            Reflector.SetValue(Type, MemberName, null, value);
        }
    }
}
