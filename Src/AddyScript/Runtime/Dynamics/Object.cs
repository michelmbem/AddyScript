using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;


namespace AddyScript.Runtime.Dynamics
{
    public sealed class Object : Dynamic
    {
        private readonly Class klass;
        private readonly Dictionary<string, Dynamic> fields;

        public Object(Class klass, Dictionary<string, Dynamic> fields)
        {
            this.klass = klass;
            this.fields = fields;
        }

        public Object(Class klass)
            : this(klass, new Dictionary<string, Dynamic>())
        {
        }

        public Object(Dictionary<string, Dynamic> fields)
            : this(Class.Object, fields)
        {
        }

        public Object()
            : this(Class.Object, new Dictionary<string, Dynamic>())
        {
        }

        public override Class Class
        {
            get { return klass; }
        }

        public override Dictionary<Dynamic, Dynamic> AsDictionary
        {
            get
            {
                var dict = new Dictionary<Dynamic, Dynamic>();

                foreach (KeyValuePair<string, Dynamic> pair in fields)
                    dict.Add(new String(pair.Key), pair.Value);

                return dict;
            }
        }

        public override Dictionary<string, Dynamic> AsDynamicObject
        {
            get { return fields; }
        }

        public override object AsNativeObject
        {
            get { return new Tuple<Class, Dictionary<string, Dynamic>>(klass, fields); }
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<{0}:0x{1:x8} {{", Class.Name, SerialID);

            bool trimEnd = false;
            foreach (KeyValuePair<string, Dynamic> pair in fields)
            {
                ClassField field = klass.GetField(pair.Key);
                if (field != null && field.Scope != Scope.Public) continue;
                sb.AppendFormat("{0} = {1}, ", pair.Key, pair.Value);
                trimEnd = true;
            }

            if (trimEnd)
                sb.Remove(sb.Length - 2, 2);

            return sb.Append("}>").ToString();
        }

        public override object ConvertTo(Type targetType)
        {
            if (targetType != typeof(Dynamic) && targetType != typeof(object))
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static |
                                           BindingFlags.Instance | BindingFlags.SetField |
                                           BindingFlags.SetProperty | BindingFlags.OptionalParamBinding;

                var binder = new DynamicBinder();
                object instance = Activator.CreateInstance(targetType);
                foreach (KeyValuePair<string, Dynamic> pair in fields)
                    targetType.InvokeMember(pair.Key, flags, binder, instance, new[] { pair.Value });

                return instance;
            }

            return base.ConvertTo(targetType);
        }

        public override Dynamic GetProperty(string propertyName)
        {
            Dynamic value;
            fields.TryGetValue(propertyName, out value);
            return value;
        }

        public override void SetProperty(string propertyName, Dynamic value)
        {
            fields[propertyName] = value;
        }
    }
}
