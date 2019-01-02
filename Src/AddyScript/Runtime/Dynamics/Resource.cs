using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using AddyScript.Properties;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Resource : Dynamic
    {
        private readonly object handle;

        public Resource(object handle)
        {
            this.handle = handle;
        }

        public Type NativeType
        {
            get { return handle.GetType(); }
        }

        public override Class Class
        {
            get { return Class.Resource; }
        }

        public override object AsNativeObject
        {
            get { return handle; }
        }

        public override object Clone()
        {
            object handleClone = handle is ICloneable
                               ? ((ICloneable) handle).Clone()
                               : handle;

            return new Resource(handleClone);
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return handle is IFormattable
                ? ((IFormattable) handle).ToString(format, formatProvider)
                : handle.ToString();
        }

        protected override bool UnsafeEquals(Dynamic other)
        {
            return handle.Equals(other.AsNativeObject);
        }

        public override int GetHashCode()
        {
            return handle.GetHashCode();
        }

        protected override int UnsafeCompareTo(Dynamic other)
        {
            return handle is IComparable
                ? ((IComparable) handle).CompareTo(other)
                : base.UnsafeCompareTo(other);
        }

        public override void Dispose()
        {
            if (handle is IDisposable)
                ((IDisposable) handle).Dispose();
            else
                base.Dispose();
        }

        public override object ConvertTo(Type targetType)
        {
            return NativeType == targetType || NativeType.IsSubclassOf(targetType)
                 ? handle
                 : handle is IConvertible
                 ? ((IConvertible) handle).ToType(targetType, CultureInfo.InvariantCulture)
                 : base.ConvertTo(targetType);
        }

        public override Dynamic GetProperty(string propertyName)
        {
            return Reflector.GetValue(NativeType, propertyName, handle);
        }

        public override void SetProperty(string propertyName, Dynamic value)
        {
            Reflector.SetValue(NativeType, propertyName, handle, value);
        }

        public override Dynamic GetItem(Dynamic index)
        {
            const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

            object obj = NativeType.IsCOMObject
                       ? NativeType.InvokeMember("Item", flags, null, handle, new[] { index.AsNativeObject })
                       : NativeType.InvokeMember("get_Item", flags, new DynamicBinder(), handle, new[] { index });

            return DynamicFactory.CreateDynamic(obj);
        }

        public override void SetItem(Dynamic index, Dynamic value)
        {
            const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;

            if (NativeType.IsCOMObject)
                NativeType.InvokeMember("Item", flags, null, handle, new[] { index.AsNativeObject, value.AsNativeObject });
            else
                NativeType.InvokeMember("set_Item", flags, new DynamicBinder(), handle, new[] { index, value });
        }

        public override IEnumerable<KeyValuePair<Dynamic, Dynamic>> GetEnumerable()
        {
            if (handle is IDictionary)
            {
                foreach (DictionaryEntry entry in (IDictionary) handle)
                    yield return new KeyValuePair<Dynamic, Dynamic>(
                        DynamicFactory.CreateDynamic(entry.Key),
                        DynamicFactory.CreateDynamic(entry.Value));
            }
            else if (handle is IEnumerable)
            {
                int index = 0;
                foreach (object item in (IEnumerable) handle)
                    yield return new KeyValuePair<Dynamic, Dynamic>(
                        new Integer(index++),
                        DynamicFactory.CreateDynamic(item));
            }
            else
                throw new InvalidOperationException(string.Format(Resources.IterationNotSupported, NativeType.FullName));
        }
    }
}
