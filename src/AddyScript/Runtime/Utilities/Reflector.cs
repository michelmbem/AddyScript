using System;
using System.Reflection;

using AddyScript.Properties;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Runtime.Utilities
{
    public static class Reflector
    {
        public static DataItem GetValue(Type type, string memberName, object target)
        {
            object result = null;

            if (type.IsCOMObject)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static |
                                           BindingFlags.Instance | BindingFlags.GetField |
                                           BindingFlags.GetProperty | BindingFlags.OptionalParamBinding;

                result = type.InvokeMember(memberName, flags, null, target, null);
            }
            else
            {
                MemberInfo member = GetValueMember(type, memberName);
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        var field = (FieldInfo)member;
                        result = field.GetValue(target);
                        break;
                    case MemberTypes.Property:
                        var property = (PropertyInfo)member;
                        if (!property.CanRead)
                            throw new InvalidOperationException(Resources.CannotReadProperty);
                        result = property.GetValue(target, null);
                        break;
                }
            }

            return DataItemFactory.CreateDataItem(result);
        }

        public static void SetValue(Type type, string memberName, object target, DataItem value)
        {
            if (type.IsCOMObject)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static |
                                           BindingFlags.Instance | BindingFlags.SetField |
                                           BindingFlags.SetProperty | BindingFlags.OptionalParamBinding;

                type.InvokeMember(memberName, flags, null, target, [value.AsNativeObject]);
            }
            else
            {
                MemberInfo member = GetValueMember(type, memberName);
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        var field = (FieldInfo) member;
                        field.SetValue(target, value.ConvertTo(field.FieldType));
                        break;
                    case MemberTypes.Property:
                        var property = (PropertyInfo) member;
                        if (!property.CanWrite)
                            throw new InvalidOperationException(Resources.CannotWriteProperty);
                        property.SetValue(target, value.ConvertTo(property.PropertyType), null);
                        break;
                }
            }
        }

        private static MemberInfo GetValueMember(Type type, string memberName)
        {
            const MemberTypes types = MemberTypes.Field | MemberTypes.Property;

            BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            MemberInfo[] members = type.GetMember(memberName, types, flags);
            if (members.Length > 1)
            {
                flags |= BindingFlags.DeclaredOnly;
                members = type.GetMember(memberName, types, flags);
            }

            foreach (MemberInfo member in members)
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        return member;
                    case MemberTypes.Property:
                        if (((PropertyInfo)member).GetIndexParameters().Length <= 0)
                            return member;
                        break;
                }

            throw new InvalidOperationException(string.Format(Resources.NoMatchingProperty, type.FullName));
        }
    }
}
