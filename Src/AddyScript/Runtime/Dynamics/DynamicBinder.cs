using System;
using System.Collections;
using System.Globalization;
using System.Reflection;


namespace AddyScript.Runtime.Dynamics
{
    /// <summary>
    /// A custom binder for our calls to Type.InvokeMember.
    /// </summary>
    public class DynamicBinder : Binder
    {
        #region Public Static Methods

        public static MethodInfo FindMethod(
            Type type,
            string methodName,
            Dynamic[] args,
            BindingFlags flags)
        {
            int index = -1, minimum = int.MaxValue;
            MemberInfo[] members = type.GetMember(methodName, MemberTypes.Method, flags);

            for (int i = 0; i < members.Length; ++i)
            {
                var method = (MethodInfo) members[i];
                ParameterInfo[] parameters = method.GetParameters();
                if (args.Length > parameters.Length) continue;

                int mismatch = 0, j = 0;
                for (; j < args.Length; ++j)
                    mismatch += Mismatch(args[j], parameters[j].ParameterType);
                for (; j < parameters.Length; ++j)
                    mismatch += 4; // 4 is the maximum value of DynamicBinder.Mismatch
                if (mismatch >= minimum) continue;

                index = i;
                minimum = mismatch;
                if (minimum == 0) break;
            }

            return index < 0 ? null : (MethodInfo) members[index];
        }

        #endregion

        #region Overrides

        public override MethodBase BindToMethod(
            BindingFlags bindingAttr,
            MethodBase[] match,
            ref object[] args,
            ParameterModifier[] modifiers,
            CultureInfo culture,
            string[] names,
            out object state)
        {
            state = new BinderState { args = args };

            int index = -1, minimum = int.MaxValue;

            for (int i = 0; i < match.Length; ++i)
            {
                ParameterInfo[] parameters = match[i].GetParameters();
                if (args.Length > parameters.Length) continue;

                int mismatch = 0, j = 0;
                for (; j < args.Length; ++j)
                    mismatch += Mismatch(((Dynamic) args[j]), parameters[j].ParameterType);
                for (; j < parameters.Length; ++j)
                    mismatch += 4;
                if (mismatch >= minimum) continue;

                index = i;
                minimum = mismatch;
                if (minimum == 0) break;
            }

            if (index < 0) return null;

            ParameterInfo[] matchParams = match[index].GetParameters();
            var values = new object[args.Length];
            for (int i = 0; i < args.Length; ++i)
                values[i] = ChangeType(args[i], matchParams[i].ParameterType, culture);
            args = values;

            return match[index];
        }

        public override FieldInfo BindToField(
            BindingFlags bindingAttr,
            FieldInfo[] match,
            object value,
            CultureInfo culture)
        {
            return match.Length > 0 ? match[0] : null;
        }

        public override MethodBase SelectMethod(
            BindingFlags bindingAttr,
            MethodBase[] match,
            Type[] types,
            ParameterModifier[] modifiers)
        {
            int minimum = int.MaxValue, index = -1;

            for (int i = 0; i < match.Length; ++i)
            {
                ParameterInfo[] parameters = match[i].GetParameters();
                if (types.Length > parameters.Length) continue;

                int mismatch = 0, j = 0;
                for (; j < types.Length; ++j)
                    mismatch += Mismatch(types[j], parameters[j].ParameterType);
                for (; j < parameters.Length; ++j)
                    mismatch += 4;

                if (mismatch == 0) return match[i];
                if (mismatch >= minimum) continue;

                minimum = mismatch;
                index = i;
            }

            return match[index];
        }

        public override PropertyInfo SelectProperty(
            BindingFlags bindingAttr,
            PropertyInfo[] match,
            Type returnType,
            Type[] indexes,
            ParameterModifier[] modifiers)
        {
            int minimum = int.MaxValue, index = -1;

            for (int i = 0; i < match.Length; ++i)
            {
                ParameterInfo[] parameters = match[i].GetIndexParameters();
                if (indexes.Length > parameters.Length) continue;

                int mismatch = 0, j = 0;
                for (; j < indexes.Length; ++j)
                    mismatch += Mismatch(indexes[j], parameters[j].ParameterType);
                for (; j < parameters.Length; ++j)
                    mismatch += 4;

                if (mismatch == 0) return match[i];
                if (mismatch >= minimum) continue;

                minimum = mismatch;
                index = i;
            }

            return match[index];
        }

        public override object ChangeType(object value, Type type, CultureInfo culture)
        {
            return value is Dynamic
                ? ((Dynamic) value).ConvertTo(type)
                : Convert.ChangeType(value, type);
        }

        public override void ReorderArgumentArray(ref object[] args, object state)
        {
            args = ((BinderState) state).args;
        }

        #endregion

        #region Mismatch Method

        /// <summary>
        /// Determines the degree of incompatibility between an AddyScript's variable and a native type.
        /// </summary>
        /// <param name="v">An AddyScript's variable</param>
        /// <param name="t">A native CLR <see cref="Type"/></param>
        /// <returns>
        /// <ul>
        /// <li>0: full compatibility</li>
        /// <li>1: <paramref name="v"/> can be converted to <paramref name="t"/> without loss</li>
        /// <li>2: <paramref name="v"/> can be converted to <paramref name="t"/> with possible loss</li>
        /// <li>3: <paramref name="t"/> is the <see cref="String"/> type</li>
        /// <li>4: No possible conversion between both types</li>
        /// </ul>
        /// </returns>
        private static int Mismatch(Dynamic v, Type t)
        {
            return v is Resource
                ? Mismatch(((Resource) v).NativeType, t)
                : Mismatch(v.Class, t);
        }

        /// <summary>
        /// Determines the degree of incompatibility between an AddyScript's type and a native type.
        /// </summary>
        /// <param name="c">An AddyScript's <see cref="Class"/></param>
        /// <param name="t">A native CLR <see cref="Type"/></param>
        /// <returns>
        /// <ul>
        /// <li>0: full compatibility</li>
        /// <li>1: <paramref name="c"/> can be converted to <paramref name="t"/> without loss</li>
        /// <li>2: <paramref name="c"/> can be converted to <paramref name="t"/> with possible loss</li>
        /// <li>3: <paramref name="t"/> is the <see cref="String"/> type</li>
        /// <li>4: No possible conversion between both types</li>
        /// </ul>
        /// </returns>
        private static int Mismatch(Class c, Type t)
        {
            if (t == typeof(object)) return 1;

            switch (c.ClassID)
            {
                case ClassID.Void:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.Empty:
                            return 0;
                        case TypeCode.Object:
                        case TypeCode.String:
                        case TypeCode.DBNull:
                            return 1;
                        default:
                            return 4;
                    }
                case ClassID.Boolean:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.Boolean:
                            return 0;
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return 1;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case ClassID.Integer:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.Single:
                            return 2;
                        case TypeCode.Int32:
                            return 0;
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return 1;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case ClassID.Float:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Decimal:
                            return 2;
                        case TypeCode.Double:
                            return 0;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case ClassID.Long:
                case ClassID.Decimal:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return 2;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case ClassID.Date:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.DateTime:
                            return 0;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case ClassID.String:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                        case TypeCode.DateTime:
                        case TypeCode.Char:
                            return 2;
                        case TypeCode.String:
                            return 0;
                        case TypeCode.Object:
                            return t.IsEnum ||
                                   t == typeof(char[]) ||
                                   t == typeof(byte[]) ? 2 : 4;
                        default:
                            return 4;
                    }
                case ClassID.List:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.Object:
                            if (t.IsArray ||
                                t == typeof(IList) ||
                                t == typeof(ICollection) ||
                                t == typeof(IEnumerable)) return 1;
                            return 4;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case ClassID.Map:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.Object:
                            if (t == typeof(IDictionary) ||
                                t == typeof(ICollection) ||
                                t == typeof(IEnumerable)) return 1;
                            return 4;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case ClassID.Set:
                case ClassID.Queue:
                case ClassID.Stack:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.Object:
                            if (t == typeof(ICollection) ||
                                t == typeof(IEnumerable)) return 1;
                            return 4;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case ClassID.Object:
                case ClassID.Resource:
                    switch (Type.GetTypeCode(t))
                    {
                        case TypeCode.DBNull:
                        case TypeCode.Object:
                            return 1;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                default:
                    return 4;
            }
        }

        /// <summary>
        /// Determines the degree of incompatibility between two native types.
        /// </summary>
        /// <param name="t1">The source <see cref="Type"/></param>
        /// <param name="t2">The destination <see cref="Type"/></param>
        /// <returns>
        /// <ul>
        /// <li>0: full compatibility</li>
        /// <li>1: <paramref name="t1"/> can be converted to <paramref name="t2"/> without loss</li>
        /// <li>2: <paramref name="t1"/> can be converted to <paramref name="t2"/> with possible loss</li>
        /// <li>3: <paramref name="t2"/> is the <see cref="String"/> type</li>
        /// <li>4: No possible conversion between both types</li>
        /// </ul>
        /// </returns>
        private static int Mismatch(Type t1, Type t2)
        {
            if (t1 == t2) return 0;

            switch (Type.GetTypeCode(t1))
            {
                case TypeCode.Empty:
                    switch (Type.GetTypeCode(t2))
                    {
                        case TypeCode.Empty:
                            return 0;
                        case TypeCode.Object:
                        case TypeCode.String:
                        case TypeCode.DBNull:
                            return 1;
                        default:
                            return 4;
                    }
                case TypeCode.DBNull:
                    switch (Type.GetTypeCode(t2))
                    {
                        case TypeCode.DBNull:
                            return 0;
                        default:
                            return 1;
                    }
                case TypeCode.Boolean:
                    switch (Type.GetTypeCode(t2))
                    {
                        case TypeCode.Boolean:
                            return 0;
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return 1;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case TypeCode.Int32:
                    switch (Type.GetTypeCode(t2))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.Single:
                            return 2;
                        case TypeCode.Int32:
                            return 0;
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return 1;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case TypeCode.Double:
                    switch (Type.GetTypeCode(t2))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Decimal:
                            return 2;
                        case TypeCode.Double:
                            return 0;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case TypeCode.Decimal:
                    switch (Type.GetTypeCode(t2))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return 2;
                        case TypeCode.Decimal:
                            return 0;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case TypeCode.DateTime:
                    switch (Type.GetTypeCode(t2))
                    {
                        case TypeCode.DateTime:
                            return 0;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                case TypeCode.String:
                    switch (Type.GetTypeCode(t2))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                        case TypeCode.DateTime:
                            return 2;
                        case TypeCode.String:
                            return 0;
                        default:
                            return 4;
                    }
                case TypeCode.Object:
                    switch (Type.GetTypeCode(t2))
                    {
                        case TypeCode.Object:
                            if (t1.IsSubclassOf(t2) ||
                               (t2.IsInterface && Array.IndexOf(t1.GetInterfaces(), t2) >= 0)) return 1;
                            return 4;
                        case TypeCode.String:
                            return 3;
                        default:
                            return 4;
                    }
                default:
                    return 4;
            }
        }

        #endregion

        #region Nested class : BinderState

        /// <summary>
        /// An utility type used to store the original arguments of a method.
        /// </summary>
        public class BinderState
        {
            public object[] args;
        }

        #endregion
    }
}