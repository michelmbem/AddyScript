using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems;


public sealed class Object(Class klass, Dictionary<string, DataItem> fields) : DataItem
{
    public Object(Class klass) : this(klass, []) { }

    public Object(Dictionary<string, DataItem> fields) : this(Class.Object, fields) { }

    public Object() : this(Class.Object, []) { }

    public override Class Class => klass;

    public override Dictionary<DataItem, DataItem> AsDictionary
    {
        get
        {
            Dictionary<DataItem, DataItem> dict = [];

            foreach (var pair in fields)
                dict.Add(new String(pair.Key), pair.Value);

            return dict;
        }
    }

    public override Dictionary<string, DataItem> AsDynamicObject => fields;

    public override object AsNativeObject => (klass, fields);

    public override object Clone()
    {
        if (IsOverridden("clone"))
            return RuntimeServices.Clone(this);

        Dictionary<string, DataItem> cloneFields = [];

        foreach (var pair in fields)
            cloneFields.Add(pair.Key, (DataItem)pair.Value.Clone());

        return new Object(klass, cloneFields);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        if (IsOverridden("toString"))
            return RuntimeServices.ToString(this, format);

        StringBuilder sb = new ($"<{Class.Name} {{");
        bool stripEnd = false;
        var publicFields = from pair in fields
                           let field = klass.GetField(pair.Key)
                           where field == null || field.Scope == Scope.Public
                           select pair;

        foreach (var (name, value) in publicFields)
        {
            sb.Append($"{name} = {value.ToString(format, formatProvider)}, ");
            stripEnd = true;
        }

        if (stripEnd) sb.Length -= 2;
        sb.Append("}>");

        return sb.ToString();
    }

    protected override bool UnsafeEquals(DataItem other)
    {
        return IsOverridden("equals")
             ? RuntimeServices.Equals(this, other)
             : base.UnsafeEquals(other);
    }

    public override int GetHashCode()
    {
        return IsOverridden("hashCode")
             ? RuntimeServices.HashCode(this)
             : base.GetHashCode();
    }

    protected override int UnsafeCompareTo(DataItem other)
    {
        return IsOverridden("compareTo")
             ? RuntimeServices.CompareTo(this, other)
             : base.UnsafeCompareTo(other);
    }

    public override void Dispose()
    {
        if (IsOverridden("dispose"))
            RuntimeServices.Dispose(this);
        else
            base.Dispose();
    }

    public override object ConvertTo(Type targetType)
    {
        if (targetType == typeof(DataItem) || targetType == typeof(object))
            return base.ConvertTo(targetType);

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static |
                                   BindingFlags.Instance | BindingFlags.SetField |
                                   BindingFlags.SetProperty | BindingFlags.OptionalParamBinding;

        var binder = new DataItemBinder();
        object instance = Activator.CreateInstance(targetType);

        foreach (var (name, value) in fields)
            targetType.InvokeMember(name, flags, binder, instance, [value]);

        return instance;

    }

    public override DataItem GetProperty(string propertyName)
    {
        fields.TryGetValue(propertyName, out DataItem value);
        return value;
    }

    public override void SetProperty(string propertyName, DataItem value)
    {
        fields[propertyName] = value;
    }

    private bool IsOverridden(string methodName)
    {
        var method = klass.GetMember(methodName, MemberKind.Method);
        return method != null && method.Holder != Class.Object;
    }
}
