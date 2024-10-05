using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using AddyScript.Properties;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.DataItems;


public sealed class Resource(object handle) : DataItem
{
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
        return handle.GetType().IsAssignableTo(targetType)
             ? handle
             : handle is IConvertible convertible
             ? convertible.ToType(targetType, CultureInfo.CurrentUICulture)
             : base.ConvertTo(targetType);
    }

    public override DataItem GetProperty(string propertyName)
        => Reflector.GetValue(handle.GetType(), propertyName, handle);

    public override void SetProperty(string propertyName, DataItem value)
        => Reflector.SetValue(handle.GetType(), propertyName, handle, value);

    public override DataItem GetItem(DataItem index) => Reflector.GetItem(handle, index);

    public override void SetItem(DataItem index, DataItem value) => Reflector.SetItem(handle, index, value);

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
            throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, handle.GetType().FullName));
    }
}
