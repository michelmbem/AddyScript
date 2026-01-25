using System;
using System.Reflection;

using AddyScript.Properties;
using AddyScript.Runtime.DataItems;


namespace AddyScript.Runtime.Utilities;


public static class Reflector
{
    public static object CreateInstance(Type type, DataItem[] constructorArgs)
    {
        const BindingFlags flags = BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;
        return type.InvokeMember(null, flags, new DataItemBinder(), null, constructorArgs);
    }

    public static DataItem GetValue(Type type, string memberName, object target)
    {
        object result;

        if (type.IsCOMObject)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.OptionalParamBinding |
                                       BindingFlags.Static | BindingFlags.Instance |
                                       BindingFlags.GetField | BindingFlags.GetProperty;

            result = type.InvokeMember(memberName, flags, null, target, null);
        }
        else
            result = GetDataMember(type, memberName) switch
            {
                FieldInfo field => field.GetValue(target),
                PropertyInfo { CanRead: true } property => property.GetValue(target, null),
                _ => throw new InvalidOperationException(Resources.CannotReadProperty)
            };

        return DataItemFactory.CreateDataItem(result);
    }

    public static void SetValue(Type type, string memberName, object target, DataItem value)
    {
        if (type.IsCOMObject)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.OptionalParamBinding |
                                       BindingFlags.Static | BindingFlags.Instance |
                                       BindingFlags.SetField | BindingFlags.SetProperty;

            type.InvokeMember(memberName, flags, null, target, [value.AsNativeObject]);
        }
        else
            switch (GetDataMember(type, memberName))
            {
                case FieldInfo { IsInitOnly: false } field:
                    field.SetValue(target, value.ConvertTo(field.FieldType));
                    break;
                case PropertyInfo { CanWrite: true } property:
                    property.SetValue(target, value.ConvertTo(property.PropertyType), null);
                    break;
                default:
                    throw new InvalidOperationException(Resources.CannotWriteProperty);
            }
    }

    public static DataItem GetItem(object target, DataItem index)
    {
        const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

        Type targetType = target.GetType();

        object obj = targetType.IsCOMObject
                   ? targetType.InvokeMember("Item", flags, null, target, [index.AsNativeObject])
                   : targetType.InvokeMember("get_Item", flags, new DataItemBinder(), target, [index]);

        return DataItemFactory.CreateDataItem(obj);
    }

    public static void SetItem(object target, DataItem index, DataItem value)
    {
        const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

        Type targetType = target.GetType();

        if (targetType.IsCOMObject)
            targetType.InvokeMember("Item", flags, null, target, [index.AsNativeObject, value.AsNativeObject]);
        else
            targetType.InvokeMember("set_Item", flags, new DataItemBinder(), target, [index, value]);
    }

    private static MemberInfo GetDataMember(Type type, string memberName)
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
            switch (member)
            {
                case FieldInfo:
                case PropertyInfo p when p.GetIndexParameters().Length == 0:
                    return member;
            }

        throw new InvalidOperationException(string.Format(Resources.NoMatchingProperty, type.FullName));
    }
}
