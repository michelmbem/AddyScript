using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using AddyScript.Properties;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.DataItems;


public sealed class Resource(object handle) : DataItem
{
    public Type NativeType => handle.GetType();

    public override Class Class => Class.Resource;

    public override object AsNativeObject => handle;

    public override object Clone()
    {
        object handleClone = handle is ICloneable cloneable
                           ? cloneable.Clone()
                           : handle;

        return new Resource(handleClone);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        return handle is IFormattable formattable
             ? formattable.ToString(format, formatProvider)
             : handle.ToString();
    }

    protected override bool UnsafeEquals(DataItem other) => handle.Equals(other.AsNativeObject);

    public override int GetHashCode() => handle.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other)
    {
        return handle is IComparable comparable
             ? comparable.CompareTo(other)
             : base.UnsafeCompareTo(other);
    }

    public override void Dispose()
    {
        if (handle is IDisposable disposable)
            disposable.Dispose();

        base.Dispose();
    }

    public override object ConvertTo(Type targetType)
    {
        return NativeType == targetType || NativeType.IsSubclassOf(targetType)
             ? handle
             : handle is IConvertible convertible
             ? convertible.ToType(targetType, CultureInfo.InvariantCulture)
             : base.ConvertTo(targetType);
    }

    public override DataItem GetProperty(string propertyName)
        => Reflector.GetValue(NativeType, propertyName, handle);

    public override void SetProperty(string propertyName, DataItem value)
        => Reflector.SetValue(NativeType, propertyName, handle, value);

    public override DataItem GetItem(DataItem index)
    {
        const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

        object obj = NativeType.IsCOMObject
                   ? NativeType.InvokeMember("Item", flags, null, handle, [index.AsNativeObject])
                   : NativeType.InvokeMember("get_Item", flags, new DataItemBinder(), handle, [index]);

        return DataItemFactory.CreateDataItem(obj);
    }

    public override void SetItem(DataItem index, DataItem value)
    {
        const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

        if (NativeType.IsCOMObject)
            NativeType.InvokeMember("Item", flags, null, handle, [index.AsNativeObject, value.AsNativeObject]);
        else
            NativeType.InvokeMember("set_Item", flags, new DataItemBinder(), handle, [index, value]);
    }

    public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
    {
        if (handle is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
                yield return new KeyValuePair<DataItem, DataItem>(
                    DataItemFactory.CreateDataItem(entry.Key),
                    DataItemFactory.CreateDataItem(entry.Value));
        }
        else if (handle is IEnumerable enumerable)
        {
            int index = 0;

            foreach (object item in enumerable)
                yield return new KeyValuePair<DataItem, DataItem>(
                    new Integer(index++),
                    DataItemFactory.CreateDataItem(item));
        }
        else
            throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, NativeType.FullName));
    }
}
