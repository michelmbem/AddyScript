﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using AddyScript.Runtime.OOP;


namespace AddyScript.Runtime.DataItems
{
    public sealed class Object(Class klass, Dictionary<string, DataItem> fields) : DataItem
    {
        public Object(Class klass) : this(klass, [])
        {
        }

        public Object(Dictionary<string, DataItem> fields) : this(Class.Object, fields)
        {
        }

        public Object() : this(Class.Object, [])
        {
        }

        public override Class Class => klass;

        public override Dictionary<DataItem, DataItem> AsDictionary
        {
            get
            {
                var dict = new Dictionary<DataItem, DataItem>();

                foreach (var pair in fields)
                    dict.Add(new String(pair.Key), pair.Value);

                return dict;
            }
        }

        public override Dictionary<string, DataItem> AsDynamicObject => fields;

        public override object AsNativeObject
        {
            get { return new Tuple<Class, Dictionary<string, DataItem>>(klass, fields); }
        }

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<{0} {{", Class.Name);

            bool trimEnd = false;

            foreach (var pair in fields)
            {
                ClassField field = klass.GetField(pair.Key);
                if (!(field == null || field.Scope == Scope.Public)) continue;
                sb.AppendFormat("{0} = {1}, ", pair.Key, pair.Value);
                trimEnd = true;
            }

            if (trimEnd) sb.Remove(sb.Length - 2, 2);

            return sb.Append("}>").ToString();
        }

        public override object ConvertTo(Type targetType)
        {
            if (!(targetType == typeof(DataItem) || targetType == typeof(object)))
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static |
                                           BindingFlags.Instance | BindingFlags.SetField |
                                           BindingFlags.SetProperty | BindingFlags.OptionalParamBinding;

                var binder = new DataItemBinder();
                object instance = Activator.CreateInstance(targetType);

                foreach (var pair in fields)
                    targetType.InvokeMember(pair.Key, flags, binder, instance, [pair.Value]);

                return instance;
            }

            return base.ConvertTo(targetType);
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
    }
}