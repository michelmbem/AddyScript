#region 'using' Directives

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Parsers;
using AddyScript.Properties;
using AddyScript.Runtime.Dynamics;
using AddyScript.Runtime.Utilities;
using Boolean = AddyScript.Runtime.Dynamics.Boolean;
using Complex = AddyScript.Runtime.Dynamics.Complex;
using Decimal = AddyScript.Runtime.Dynamics.Decimal;
using String = AddyScript.Runtime.Dynamics.String;
using Void = AddyScript.Runtime.Dynamics.Void;
using System.Linq;

#endregion

namespace AddyScript.Runtime
{
    /// <summary>
    /// A method that can be wrapped in an instance of <see cref="InnerFunction"/>
    /// </summary>
    /// <param name="arguments">The arguments passed to the function when it's called</param>
    /// <returns>A <see cref="Dynamic"/></returns>
    public delegate Dynamic InnerFunctionLogic(Dynamic[] arguments);

    /// <summary>
    /// An operation that is handled by the scripting engine as an atomic action.
    /// </summary>
    public class InnerFunction
    {
        #region Fields

        /// <summary>
        /// A registry for global instances of <see cref="InnerFunction"/>.
        /// </summary>
        public static readonly List<InnerFunction> Globals;

        /// <summary>
        /// The random numbers generator.
        /// </summary>
        private static readonly RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();

        #endregion

        #region Constructors

        /// <summary>
        /// Class initializer: registers global functions and attaches methods to corresponding classes.
        /// </summary>
        static InnerFunction()
        {
            Globals = new List<InnerFunction>
                          {
                              Char,
                              Order,
                              Random,
                              RandomInteger,
                              Sine,
                              Cosine,
                              Tangent,
                              ArcSine,
                              ArcCosine,
                              ArcTangent,
                              ArcTangent2,
                              SineHyperbolic,
                              CosineHyperbolic,
                              TangentHyperbolic,
                              DegreesToRadians,
                              RadiansToDegrees,
                              Logarithm,
                              LogarithmBase10,
                              LogarithmBaseN,
                              Exponential,
                              SquareRoot,
                              Sign,
                              AbsoluteValue,
                              Truncate,
                              Floor,
                              Ceiling,
                              Round,
                              Minimum,
                              Maximum,
                              Fifo,
                              Lifo,
                              Now,
                              Format,
                              ReadLine,
                              Print,
                              PrintLine,
                              Evaluate,
                              Pack,
                              UnPack
                          };

            InnerFunction[] commonFunctions = { EqualsFunction, HashCodeFunction, CompareToFunction, ToStringFunction, CloneFunction, DisposeFunction };
            InnerFunction[] dateFunctions = { DateGet, DateAdd, DateAddTicks, DateSubtract };
            InnerFunction[] stringFunctions = { StringIndexOf, StringLastIndexOf, StringToLower, StringToUpper, StringCapitalize, StringUncapitalize, StringSubstring, StringInsert, StringRemove, StringReplace, StringTrimLeft, StringTrimRight, StringTrim, StringPadLeft, StringPadRight, StringSplit };
            InnerFunction[] listFunctions = { ListJoin, ListAdd, ListInsert, ListInsertAll, ListIndexOf, ListLastIndexOf, ListBinarySearch, ListFrequencyOf, ListRemove, ListRemoveAt, ListClear, ListSort, ListShuffle, ListInverse, ListSublist, ListUnique, ListMapTo };
            InnerFunction[] mapFunctions = { MapContainsKey, MapContainsValue, MapFrequencyOf, MapKeysOf, MapInverse, MapRemove, MapRemoveAll, MapClear };
            InnerFunction[] setFunctions = { SetAdd, SetRemove, SetClear };
            InnerFunction[] queueFunctions = { QueueEnqueue, QueuePeek, QueueDequeue, QueueClear };
            InnerFunction[] stackFunctions = { StackPush, StackPeek, StackPop, StackClear };

            foreach (InnerFunction function in commonFunctions)
                foreach (Class cls in Class.Predefined)
                    if (cls.SuperClass == null)
                        cls.RegisterMethod(function.ToInstanceMethod());

            Class.Rational.RegisterProperty(RationalNum.ToInstanceProperty());
            Class.Rational.RegisterProperty(RationalDen.ToInstanceProperty());
            Class.Rational.RegisterMethod(RationalInverse.ToInstanceMethod());

            Class.Complex.RegisterProperty(ComplexReal.ToInstanceProperty());
            Class.Complex.RegisterProperty(ComplexImaginary.ToInstanceProperty());
            Class.Complex.RegisterMethod(ComplexConjugate.ToInstanceMethod());

            Class.Date.RegisterProperty(DateGetDate.ToInstanceProperty());
            Class.Date.RegisterProperty(DateGetTime.ToInstanceProperty());
            Class.Date.RegisterProperty(DateGetTicks.ToInstanceProperty());
            Class.Date.RegisterMethod(DateCreate.ToStaticMethod());
            foreach (InnerFunction function in dateFunctions)
                Class.Date.RegisterMethod(function.ToInstanceMethod());

            Class.String.RegisterProperty(StringLength.ToInstanceProperty());
            foreach (InnerFunction function in stringFunctions)
                Class.String.RegisterMethod(function.ToInstanceMethod());

            Class.List.RegisterProperty(ListCount.ToInstanceProperty());
            Class.List.RegisterMethod(ListCreate.ToStaticMethod());
            foreach (InnerFunction function in listFunctions)
                Class.List.RegisterMethod(function.ToInstanceMethod());

            Class.Map.RegisterProperty(MapCount.ToInstanceProperty());
            Class.Map.RegisterProperty(MapKeys.ToInstanceProperty());
            Class.Map.RegisterProperty(MapValues.ToInstanceProperty());
            foreach (InnerFunction function in mapFunctions)
                Class.Map.RegisterMethod(function.ToInstanceMethod());

            Class.Set.RegisterProperty(SetCount.ToInstanceProperty());
            foreach (InnerFunction function in setFunctions)
                Class.Set.RegisterMethod(function.ToInstanceMethod());

            Class.Queue.RegisterProperty(QueueCount.ToInstanceProperty());
            foreach (InnerFunction function in queueFunctions)
                Class.Queue.RegisterMethod(function.ToInstanceMethod());

            Class.Stack.RegisterProperty(StackCount.ToInstanceProperty());
            foreach (InnerFunction function in stackFunctions)
                Class.Stack.RegisterMethod(function.ToInstanceMethod());
        }

        /// <summary>
        /// Initializes a new instance of InnerFunction
        /// </summary>
        /// <param name="name">The name of this function</param>
        /// <param name="parameters">The set of parameters required for a call to this function</param>
        /// <param name="logic">The logic of this function</param>
        public InnerFunction(string name, Parameter[] parameters, InnerFunctionLogic logic)
        {
            Name = name;
            Parameters = parameters;
            Logic = logic;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The name of this function.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The set of parameters required for a call to this function.
        /// </summary>
        public Parameter[] Parameters { get; private set; }

        /// <summary>
        /// The logic of this function.
        /// </summary>
        public InnerFunctionLogic Logic { get; private set; }

        #endregion

        #region Predefined logics

        #region Global functions

        private static Dynamic CharLogic(Dynamic[] arguments)
        {
            char ch = Convert.ToChar(arguments[0].AsInt32);
            return new String(ch.ToString());
        }

        private static Dynamic OrderLogic(Dynamic[] arguments)
        {
            Dynamic arg0 = arguments[0];
            CheckArgType(arg0, Class.String, "ord", 1);

            string s = arg0.ToString();
            CheckSingleChar(s, "ord", 1);

            return new Integer(Convert.ToInt32(s[0]));
        }

        private static Dynamic RandomLogic(Dynamic[] arguments)
        {
            return new Float(NextDouble(random));
        }

        private static Dynamic RandomIntegerLogic(Dynamic[] arguments)
        {
            int min = arguments[0].AsInt32,
                max = arguments[1].AsInt32;

            if (min > max)
            {
                int tmp = min;
                min = max;
                max = tmp;
            }

            return new Integer(min + (int)(NextDouble(random) * (max - min)));
        }

        private static Dynamic SineLogic(Dynamic[] arguments)
        {
            return new Float(Math.Sin(arguments[0].AsDouble));
        }

        private static Dynamic CosineLogic(Dynamic[] arguments)
        {
            return new Float(Math.Cos(arguments[0].AsDouble));
        }

        private static Dynamic TangentLogic(Dynamic[] arguments)
        {
            return new Float(Math.Tan(arguments[0].AsDouble));
        }

        private static Dynamic ArcSineLogic(Dynamic[] arguments)
        {
            return new Float(Math.Asin(arguments[0].AsDouble));
        }

        private static Dynamic ArcCosineLogic(Dynamic[] arguments)
        {
            return new Float(Math.Acos(arguments[0].AsDouble));
        }

        private static Dynamic ArcTangentLogic(Dynamic[] arguments)
        {
            return new Float(Math.Atan(arguments[0].AsDouble));
        }

        private static Dynamic ArcTangent2Logic(Dynamic[] arguments)
        {
            return new Float(Math.Atan2(arguments[0].AsDouble, arguments[1].AsDouble));
        }

        private static Dynamic SineHyperbolicLogic(Dynamic[] arguments)
        {
            return new Float(Math.Sinh(arguments[0].AsDouble));
        }

        private static Dynamic CosineHyperbolicLogic(Dynamic[] arguments)
        {
            return new Float(Math.Cosh(arguments[0].AsDouble));
        }

        private static Dynamic TangentHyperbolicLogic(Dynamic[] arguments)
        {
            return new Float(Math.Tanh(arguments[0].AsDouble));
        }

        private static Dynamic DegreesToRadiansLogic(Dynamic[] arguments)
        {
            return new Float(arguments[0].AsDouble * Math.PI / 180.0);
        }

        private static Dynamic RadiansToDegreesLogic(Dynamic[] arguments)
        {
            return new Float(arguments[0].AsDouble * 180.0 / Math.PI);
        }

        private static Dynamic LogarithmLogic(Dynamic[] arguments)
        {
            return new Float(Math.Log(arguments[0].AsDouble));
        }

        private static Dynamic LogarithmBase10Logic(Dynamic[] arguments)
        {
            return new Float(Math.Log10(arguments[0].AsDouble));
        }

        private static Dynamic LogarithmBaseNLogic(Dynamic[] arguments)
        {
            return new Float(Math.Log(arguments[0].AsDouble, arguments[1].AsDouble));
        }

        private static Dynamic ExponentialLogic(Dynamic[] arguments)
        {
            return new Float(Math.Exp(arguments[0].AsDouble));
        }

        private static Dynamic SquareRootLogic(Dynamic[] arguments)
        {
            double x = arguments[0].AsDouble;
            return x < 0
                 ? new Complex(Complex64.ImaginaryOne * Math.Sqrt(-x))
                 : (Dynamic) new Float(Math.Sqrt(x));
        }

        private static Dynamic SignLogic(Dynamic[] arguments)
        {
            Dynamic arg = arguments[0];
            switch (arg.Class.ClassID)
            {
                case ClassID.Integer:
                    return new Integer(Math.Sign(arg.AsInt32));
                case ClassID.Long:
                    return new Integer(arg.AsBigInteger.Sign);
                case ClassID.Rational:
                    return new Integer(arg.AsRational32.Sign);
                case ClassID.Float:
                    return new Integer(Math.Sign(arg.AsDouble));
                case ClassID.Decimal:
                    return new Integer(arg.AsBigDecimal.Sign);
                default:
                    throw new InvalidOperationException(
                        string.Format(Resources.TypeDoesNotSupportFunction, "sign,", arg.Class.Name));
            }
        }

        private static Dynamic AbsoluteValueLogic(Dynamic[] arguments)
        {
            Dynamic arg = arguments[0];
            switch (arg.Class.ClassID)
            {
                case ClassID.Integer:
                    return new Integer(Math.Abs(arg.AsInt32));
                case ClassID.Long:
                    return new Long(BigInteger.Abs(arg.AsBigInteger));
                case ClassID.Rational:
                    return new Rational(arg.AsRational32.Abs());
                case ClassID.Float:
                    return new Float(Math.Abs(arg.AsDouble));
                case ClassID.Decimal:
                    return new Decimal(arg.AsBigDecimal.Abs());
                case ClassID.Complex:
                    return new Float(Complex64.Abs(arg.AsComplex64));
                default:
                    throw new InvalidOperationException(
                        string.Format(Resources.TypeDoesNotSupportFunction, "abs,", arg.Class.Name));
            }
        }

        private static Dynamic MinimumLogic(Dynamic[] arguments)
        {
            List<Dynamic> list = arguments[0].AsList;
            switch (list.Count)
            {
                case 0:
                    return Void.Value;
                case 1:
                    return list[0];
                default:
                    Dynamic minimum = list[0];

                    for (int i = 1; i < list.Count; ++i)
                        if (list[i].CompareTo(minimum) < 0)
                            minimum = list[i];

                    return minimum;
            }
        }

        private static Dynamic MaximumLogic(Dynamic[] arguments)
        {
            List<Dynamic> list = arguments[0].AsList;
            switch (list.Count)
            {
                case 0:
                    return Void.Value;
                case 1:
                    return list[0];
                default:
                    Dynamic maximum = list[0];

                    for (int i = 1; i < list.Count; ++i)
                        if (list[i].CompareTo(maximum) > 0)
                            maximum = list[i];

                    return maximum;
            }
        }

        private static Dynamic TruncateLogic(Dynamic[] arguments)
        {
            Dynamic arg = arguments[0];
            switch (arg.Class.ClassID)
            {
                case ClassID.Float:
                    return new Float(Math.Truncate(arg.AsDouble));
                case ClassID.Decimal:
                    return new Decimal(arg.AsBigDecimal.Truncate());
                default:
                    throw new InvalidOperationException(
                        string.Format(Resources.TypeDoesNotSupportFunction, "trunc,", arg.Class.Name));
            }
        }

        private static Dynamic FloorLogic(Dynamic[] arguments)
        {
            Dynamic arg = arguments[0];
            switch (arg.Class.ClassID)
            {
                case ClassID.Float:
                    return new Float(Math.Floor(arg.AsDouble));
                case ClassID.Decimal:
                    return new Decimal(arg.AsBigDecimal.Floor());
                default:
                    throw new InvalidOperationException(
                        string.Format(Resources.TypeDoesNotSupportFunction, "floor,", arg.Class.Name));
            }
        }

        private static Dynamic CeilingLogic(Dynamic[] arguments)
        {
            Dynamic arg = arguments[0];
            switch (arg.Class.ClassID)
            {
                case ClassID.Float:
                    return new Float(Math.Ceiling(arg.AsDouble));
                case ClassID.Decimal:
                    return new Decimal(arg.AsBigDecimal.Ceiling());
                default:
                    throw new InvalidOperationException(
                        string.Format(Resources.TypeDoesNotSupportFunction, "ceil,", arg.Class.Name));
            }
        }

        private static Dynamic RoundLogic(Dynamic[] arguments)
        {
            Dynamic arg1 = arguments[0], arg2 = arguments[1];
            switch (arg1.Class.ClassID)
            {
                case ClassID.Float:
                    return new Float(Math.Round(arg1.AsDouble, arg2.AsInt32));
                case ClassID.Decimal:
                    return new Decimal(arg1.AsBigDecimal.Round(arg2.AsInt32));
                default:
                    throw new InvalidOperationException(
                        string.Format(Resources.TypeDoesNotSupportFunction, "round,", arg1.Class.Name));
            }
        }

        private static Dynamic FifoLogic(Dynamic[] arguments)
        {
            return new Queue(new Queue<Dynamic>(arguments[0].AsList));
        }

        private static Dynamic LifoLogic(Dynamic[] arguments)
        {
            return new Stack(new Stack<Dynamic>(arguments[0].AsList));
        }

        private static Dynamic NowLogic(Dynamic[] arguments)
        {
            return new Date(DateTime.Now);
        }

        private static Dynamic FormatLogic(Dynamic[] arguments)
        {
            Dynamic arg0 = arguments[0];
            CheckArgType(arg0, Class.String, "format", 1);

            string mask = arg0.ToString();
            var values = arguments[1].AsList;

            return new String(FormatList(mask, values));
        }

        private static Dynamic ReadLineLogic(Dynamic[] arguments)
        {
            Dynamic arg0 = arguments[0];
            CheckArgType(arg0, Class.String, "readln", 1);
            RuntimeServices.Out.Write(arg0.ToString());

            return new String(RuntimeServices.In.ReadLine());
        }

        private static Dynamic PrintLogic(Dynamic[] arguments)
        {
            Dynamic arg0 = arguments[0];
            var values = arguments[1].AsList;

            if (values.Count <= 0)
                RuntimeServices.Out.Write(RuntimeServices.ToString(arg0));
            else
            {
                CheckArgType(arg0, Class.String, "print", 1);
                RuntimeServices.Out.Write(FormatList(arg0.ToString(), values));
            }

            return Void.Value;
        }

        private static Dynamic PrintLineLogic(Dynamic[] arguments)
        {
            Dynamic arg0 = arguments[0];
            var values = arguments[1].AsList;

            if (values.Count <= 0)
                RuntimeServices.Out.WriteLine(RuntimeServices.ToString(arg0));
            else
            {
                CheckArgType(arg0, Class.String, "println", 1);
                RuntimeServices.Out.WriteLine(FormatList(arg0.ToString(), values));
            }

            return Void.Value;
        }

        private static Dynamic EvaluateLogic(Dynamic[] arguments)
        {
            Dynamic arg0 = arguments[0];
            CheckArgType(arg0, Class.String, "eval", 1);

            string command = arg0 + ";"; // A semi-colon is appended to the input for safety
            var interpreter = RuntimeServices.Interpreter;

            try
            {
                while (command.Length > 0)
                {
                    var parser = new Parser(new Lexer(new StringReader(command)));
                    var statement = parser.RequiredStatement();
                    statement.AcceptCompiler(interpreter);
                    command = command.Substring(statement.End.Offset);
                }
            }
            catch (ParseException)
            {
                throw new ArgumentException(Resources.InvalidEvalParam);
            }

            return interpreter.ReturnedValue;
        }

        private static Dynamic PackLogic(Dynamic[] arguments)
        {
            Dynamic arg0 = arguments[0];
            var values = arguments[1].AsList;

            CheckArgType(arg0, Class.String, "pack", 1);
            var format = PackFormat.Parse(arg0.ToString());

            if (format.Length != values.Count)
                throw new ArgumentException(Resources.PackValuesDontMatchFormat);

            var ms = new MemoryStream();
            using (var bf = new BinaryFormatter(format.Endianness, ms))
            {
                int k = 0;
                foreach (PackFormatItem item in format.Items)
                    switch (item.Type)
                    {
                        case PackFormatType.Boolean:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write(values[k++].AsBoolean);
                            break;
                        case PackFormatType.SByte:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write((sbyte)values[k++].AsInt32);
                            break;
                        case PackFormatType.Byte:
                        case PackFormatType.Character:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write((byte)values[k++].AsInt32);
                            break;
                        case PackFormatType.Short:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write((short)values[k++].AsInt32);
                            break;
                        case PackFormatType.UShort:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write((ushort)values[k++].AsInt32);
                            break;
                        case PackFormatType.Integer:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write(values[k++].AsInt32);
                            break;
                        case PackFormatType.UInteger:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write((uint)values[k++].AsBigInteger);
                            break;
                        case PackFormatType.Long:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write((long)values[k++].AsBigInteger);
                            break;
                        case PackFormatType.ULong:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write((ulong)values[k++].AsBigInteger);
                            break;
                        case PackFormatType.Float:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write((float)values[k++].AsDouble);
                            break;
                        case PackFormatType.Double:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write(values[k++].AsDouble);
                            break;
                        case PackFormatType.CString:
                            {
                                var tmpString = values[k++].ToString();
                                if (tmpString.Length > item.Count)
                                    tmpString = tmpString.Substring(0, item.Count);
                                else if (tmpString.Length < item.Count)
                                    tmpString = tmpString.PadRight(item.Count, '\0');
                                bf.Write(StringUtil.String2ByteArray(tmpString));
                            }
                            break;
                        case PackFormatType.PascalString:
                            {
                                var tmpString = values[k++].ToString();
                                int count = Math.Min(item.Count - 1, 255);
                                if (tmpString.Length > count)
                                    tmpString = tmpString.Substring(0, count);
                                else if (tmpString.Length < count)
                                    tmpString = tmpString.PadRight(count, '\0');
                                bf.Write((byte)count);
                                bf.Write(StringUtil.String2ByteArray(tmpString));
                            }
                            break;
                        case PackFormatType.Pointer:
                            switch (IntPtr.Size)
                            {
                                case 4:
                                    for (int i = 0; i < item.Count; ++i)
                                        bf.Write(((IntPtr)values[k++].AsNativeObject).ToInt32());
                                    break;
                                case 8:
                                    for (int i = 0; i < item.Count; ++i)
                                        bf.Write(((IntPtr)values[k++].AsNativeObject).ToInt64());
                                    break;
                            }
                            break;
                        case PackFormatType.PaddingByte:
                            for (int i = 0; i < item.Count; ++i)
                                bf.Write((byte)0);
                            break;
                    }
                bf.Flush();
            }

            return new String(StringUtil.ByteArray2String(ms.ToArray()));
        }

        private static Dynamic UnPackLogic(Dynamic[] arguments)
        {
            CheckArgType(arguments[0], Class.String, "unpack", 1);
            CheckArgType(arguments[1], Class.String, "unpack", 2);

            var format = PackFormat.Parse(arguments[0].ToString());
            var bytes = StringUtil.String2ByteArray(arguments[1].ToString());
            var list = new List<Dynamic>();

            var ms = new MemoryStream(bytes);
            using (var br = new BinaryFormatter(format.Endianness, ms))
                foreach (PackFormatItem item in format.Items)
                    switch (item.Type)
                    {
                        case PackFormatType.Boolean:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(Boolean.FromBool(br.ReadBoolean()));
                            break;
                        case PackFormatType.SByte:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Integer(br.ReadSByte()));
                            break;
                        case PackFormatType.Byte:
                        case PackFormatType.Character:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Integer(br.ReadByte()));
                            break;
                        case PackFormatType.Short:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Integer(br.ReadInt16()));
                            break;
                        case PackFormatType.UShort:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Integer(br.ReadUInt16()));
                            break;
                        case PackFormatType.Integer:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Integer(br.ReadInt32()));
                            break;
                        case PackFormatType.UInteger:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Long(br.ReadUInt32()));
                            break;
                        case PackFormatType.Long:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Long(br.ReadInt64()));
                            break;
                        case PackFormatType.ULong:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Long(br.ReadUInt64()));
                            break;
                        case PackFormatType.Float:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Float(br.ReadSingle()));
                            break;
                        case PackFormatType.Double:
                            for (int i = 0; i < item.Count; ++i)
                                list.Add(new Float(br.ReadDouble()));
                            break;
                        case PackFormatType.CString:
                            {
                                byte[] buffer = br.ReadBytes(item.Count);
                                list.Add(new String(StringUtil.ByteArray2String(buffer).TrimEnd('\0')));
                            }
                            break;
                        case PackFormatType.PascalString:
                            {
                                byte b = br.ReadByte();
                                byte[] buffer = br.ReadBytes(b);
                                list.Add(new String(StringUtil.ByteArray2String(buffer).TrimEnd('\0')));
                            }
                            break;
                        case PackFormatType.Pointer:
                            switch (IntPtr.Size)
                            {
                                case 4:
                                    for (int i = 0; i < item.Count; ++i)
                                        list.Add(new Resource(new IntPtr(br.ReadInt32())));
                                    break;
                                case 8:
                                    for (int i = 0; i < item.Count; ++i)
                                        list.Add(new Resource(new IntPtr(br.ReadInt64())));
                                    break;
                            }
                            break;
                        case PackFormatType.PaddingByte:
                            br.ReadBytes(item.Count);
                            break;
                    }

            return new List(list);
        }

        #endregion

        #region Common methods

        private static Dynamic EqualsLogic(Dynamic[] arguments)
        {
            return Boolean.FromBool(arguments[0].Equals(arguments[1]));
        }

        private static Dynamic HashCodeLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].GetHashCode());
        }

        private static Dynamic CompareToLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].CompareTo(arguments[1]));
        }

        private static Dynamic ToStringLogic(Dynamic[] arguments)
        {
            string s = arguments[0].ToString(arguments[1].ToString(),
                                             CultureInfo.InvariantCulture);
            return new String(s);
        }

        private static Dynamic CloneLogic(Dynamic[] arguments)
        {
            return (Dynamic) arguments[0].Clone();
        }

        private static Dynamic DisposeLogic(Dynamic[] arguments)
        {
            arguments[0].Dispose();
            return Void.Value;
        }

        #endregion

        #region Rational specific methods

        private static Dynamic RationalNumLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].AsRational32.Numerator);
        }

        private static Dynamic RationalDenLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].AsRational32.Denominator);
        }

        private static Dynamic RationalInverseLogic(Dynamic[] arguments)
        {
            return new Rational(arguments[0].AsRational32.Inverse());
        }

        #endregion

        #region Complex specific methods

        private static Dynamic ComplexRealLogic(Dynamic[] arguments)
        {
            return new Float(arguments[0].AsComplex64.Real);
        }

        private static Dynamic ComplexImaginaryLogic(Dynamic[] arguments)
        {
            return new Float(arguments[0].AsComplex64.Imaginary);
        }

        private static Dynamic ComplexConjugateLogic(Dynamic[] arguments)
        {
            return new Complex(Complex64.Conjugate(arguments[0].AsComplex64));
        }

        #endregion

        #region Date specific methods

        private static Dynamic DateCreateLogic(Dynamic[] arguments)
        {
            DateTime date;
            Dynamic[] values = arguments[0].AsList.ToArray();

            switch (values.Length)
            {
                case 3: // year, month and day
                    date = new DateTime(values[0].AsInt32,
                                        values[1].AsInt32,
                                        values[2].AsInt32);
                    break;
                case 4: // hour, minute, second and millisecond
                    date = new DateTime(1, 1, 1,
                                        values[0].AsInt32,
                                        values[1].AsInt32,
                                        values[2].AsInt32,
                                        values[3].AsInt32);
                    break;
                case 6: // year, month, day, hour, minute and second
                    date = new DateTime(values[0].AsInt32,
                                        values[1].AsInt32,
                                        values[2].AsInt32,
                                        values[3].AsInt32,
                                        values[4].AsInt32,
                                        values[5].AsInt32);
                    break;
                case 7: // year, month, day, hour, minute, second and millisecond
                    date = new DateTime(values[0].AsInt32,
                                        values[1].AsInt32,
                                        values[2].AsInt32,
                                        values[3].AsInt32,
                                        values[4].AsInt32,
                                        values[5].AsInt32,
                                        values[6].AsInt32);
                    break;
                default:
                    throw new InvalidOperationException(string.Format(Resources.BadDateCreateCall, values.Length));
            }

            return new Date(date);
        }

        private static Dynamic DateGetDateLogic(Dynamic[] arguments)
        {
            return new Date(arguments[0].AsDateTime.Date);
        }

        private static Dynamic DateGetTimeLogic(Dynamic[] arguments)
        {
            long ticks = arguments[0].AsDateTime.TimeOfDay.Ticks;
            return new Date(new DateTime(ticks));
        }

        private static Dynamic DateGetLogic(Dynamic[] arguments)
        {
            Dynamic self = arguments[0], arg1 = arguments[1];
            CheckArgType(arg1, Class.String, "date::get", 1);
                
            switch (arg1.ToString())
            {
                case "year":
                    return new Integer(self.AsDateTime.Year);
                case "month":
                    return new Integer(self.AsDateTime.Month);
                case "day":
                    return new Integer(self.AsDateTime.Day);
                case "weekday":
                    return new String(self.AsDateTime.DayOfWeek.ToString());
                case "yearday":
                    return new Integer(self.AsDateTime.DayOfYear);
                case "hour":
                    return new Integer(self.AsDateTime.Hour);
                case "minute":
                    return new Integer(self.AsDateTime.Minute);
                case "second":
                    return new Integer(self.AsDateTime.Second);
                case "millisecond":
                    return new Integer(self.AsDateTime.Millisecond);
                default:
                    throw new ArgumentException(string.Format(Resources.InvalidDatePart, arg1));
            }
        }

        private static Dynamic DateGetTicksLogic(Dynamic[] arguments)
        {
            return new Long(arguments[0].AsDateTime.Ticks);
        }

        private static Dynamic DateAddLogic(Dynamic[] arguments)
        {
            Dynamic self = arguments[0], arg1 = arguments[1], arg2 = arguments[2];
            CheckArgType(arg2, Class.String, "date::add", 2);
            
            switch (arg2.ToString())
            {
                case "year":
                    return new Date(self.AsDateTime.AddYears(arg1.AsInt32));
                case "month":
                    return new Date(self.AsDateTime.AddMonths(arg1.AsInt32));
                case "day":
                    return new Date(self.AsDateTime.AddDays(arg1.AsDouble));
                case "hour":
                    return new Date(self.AsDateTime.AddHours(arg1.AsDouble));
                case "minute":
                    return new Date(self.AsDateTime.AddMinutes(arg1.AsDouble));
                case "second":
                    return new Date(self.AsDateTime.AddSeconds(arg1.AsDouble));
                case "millisecond":
                    return new Date(self.AsDateTime.AddMilliseconds(arg1.AsDouble));
                default:
                    throw new ArgumentException(string.Format(Resources.InvalidDatePart, arg2));
            }
        }

        private static Dynamic DateAddTicksLogic(Dynamic[] arguments)
        {
            var ticks = (long) arguments[1].AsBigInteger;
            return new Date(arguments[0].AsDateTime.AddTicks(ticks));
        }

        private static Dynamic DateSubtractLogic(Dynamic[] arguments)
        {
            Dynamic self = arguments[0], arg1 = arguments[1], arg2 = arguments[2];
            CheckArgType(arg2, Class.String, "date::subtract", 2);
            
            switch (arg2.ToString())
            {
                case "year":
                    return new Integer(YearDiff(self.AsDateTime, arg1.AsDateTime));
                case "month":
                    return new Integer(MonthDiff(self.AsDateTime, arg1.AsDateTime));
                case "day":
                    return new Integer((self.AsDateTime - arg1.AsDateTime).Days);
                case "hour":
                    return new Long((BigInteger)(self.AsDateTime - arg1.AsDateTime).TotalHours);
                case "minute":
                    return new Long((BigInteger)(self.AsDateTime - arg1.AsDateTime).TotalMinutes);
                case "second":
                    return new Long((BigInteger)(self.AsDateTime - arg1.AsDateTime).TotalSeconds);
                case "millisecond":
                    return new Long((BigInteger)(self.AsDateTime - arg1.AsDateTime).TotalMilliseconds);
                default:
                    throw new ArgumentException(string.Format(Resources.InvalidDatePart, arg2));
            }
        }

        #endregion

        #region String specific methods

        private static Dynamic StringLengthLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].ToString().Length);
        }

        private static Dynamic StringIndexOfLogic(Dynamic[] arguments)
        {
            var self = arguments[0].ToString();

            var arg1 = arguments[1];
            CheckArgType(arg1, Class.String, "string::indexOf", 1);
            
            var start = arguments[2].AsInt32;
            while (start < 0) start += self.Length;

            var length = arguments[3].AsInt32;
            if (length <= 0) length = self.Length - start;

            return new Integer(self.IndexOf(arg1.ToString(), start, length));
        }

        private static Dynamic StringLastIndexOfLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();

            var arg1 = arguments[1];
            CheckArgType(arg1, Class.String, "string::lastIndexOf", 1);
            
            int start = arguments[2].AsInt32;
            while (start < 0) start += self.Length;

            var length = arguments[3].AsInt32;
            if (length <= 0) length = start + 1;

            return new Integer(self.LastIndexOf(arg1.ToString(), start, length));
        }

        private static Dynamic StringToLowerLogic(Dynamic[] arguments)
        {
            return new String(arguments[0].ToString().ToLower());
        }

        private static Dynamic StringToUpperLogic(Dynamic[] arguments)
        {
            return new String(arguments[0].ToString().ToUpper());
        }

        private static Dynamic StringCapitalizeLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();
            return new String(StringUtil.Capitalize(self));
        }

        private static Dynamic StringUncapitalizeLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();
            return new String(StringUtil.Uncapitalize(self));
        }

        private static Dynamic StringSubstringLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();
            int length = arguments[2].AsInt32;

            int index = arguments[1].AsInt32;
            while (index < 0) index += self.Length;

            string substr = length > 0 ? self.Substring(index, length) : self.Substring(index);
            return new String(substr);
        }

        private static Dynamic StringInsertLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();

            int index = arguments[1].AsInt32;
            while (index < 0) index += self.Length;

            var arg2 = arguments[2];
            CheckArgType(arg2, Class.String, "string::insert", 2);

            return new String(self.Insert(index, arg2.ToString()));
        }

        private static Dynamic StringRemoveLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();
            int count = arguments[2].AsInt32;

            int index = arguments[1].AsInt32;
            while (index < 0) index += self.Length;

            string s = count <= 0 ? self.Remove(index) : self.Remove(index, count);
            return new String(s);
        }

        private static Dynamic StringReplaceLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();

            var arg1 = arguments[1];
            CheckArgType(arg1, Class.String, "string::replace", 1);

            var arg2 = arguments[2];
            CheckArgType(arg2, Class.String, "string::insert", 2);

            string s = StringUtil.GetRegex(arg1.ToString()).Replace(self, arg2.ToString());
            return new String(s);
        }

        private static Dynamic StringTrimLeftLogic(Dynamic[] arguments)
        {
            Dynamic arg1 = arguments[1];
            CheckArgType(arg1, Class.String, "string::ltrim", 1);
            char[] trimChars = arg1.ToString().ToCharArray();

            return new String(arguments[0].ToString().TrimStart(trimChars));
        }

        private static Dynamic StringTrimRightLogic(Dynamic[] arguments)
        {
            Dynamic arg1 = arguments[1];
            CheckArgType(arg1, Class.String, "string::rtrim", 1);
            char[] trimChars = arg1.ToString().ToCharArray();

            return new String(arguments[0].ToString().TrimEnd(trimChars));
        }

        private static Dynamic StringTrimLogic(Dynamic[] arguments)
        {
            Dynamic arg1 = arguments[1];
            CheckArgType(arg1, Class.String, "string::trim", 1);
            char[] trimChars = arg1.ToString().ToCharArray();

            return new String(arguments[0].ToString().Trim(trimChars));
        }

        private static Dynamic StringPadLeftLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();
            int width = arguments[1].AsInt32;

            Dynamic arg2 = arguments[2];
            CheckArgType(arg2, Class.String, "string::lpad", 2);
            
            string padding = arg2.ToString();
            CheckSingleChar(padding, "string::lpad", 2);

            return new String(self.PadLeft(width, padding[0]));
        }

        private static Dynamic StringPadRightLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();
            int width = arguments[1].AsInt32;

            Dynamic arg2 = arguments[2];
            CheckArgType(arg2, Class.String, "string::rpad", 2);

            string padding = arg2.ToString();
            CheckSingleChar(padding, "string::rpad", 2);

            return new String(self.PadRight(width, padding[0]));
        }

        private static Dynamic StringSplitLogic(Dynamic[] arguments)
        {
            string self = arguments[0].ToString();

            var arg1 = arguments[1];
            CheckArgType(arg1, Class.String, "string::split", 1);

            string[] parts = StringUtil.GetRegex(arg1.ToString()).Split(self);
            var items = new List<Dynamic>();
            for (int i = 0; i < parts.Length; ++i)
                items.Add(new String(parts[i]));

            return new List(items);
        }

        #endregion

        #region List specific methods

        private static Dynamic ListCreateLogic(Dynamic[] arguments)
        {
            var values = new Dynamic[arguments[0].AsInt32];
            
            for (int i = 0; i < values.Length; ++i)
                values[i] = Void.Value;

            return new List(values);
        }

        private static Dynamic ListJoinLogic(Dynamic[] arguments)
        {
            Dynamic self = arguments[0], arg1 = arguments[1];
            CheckArgType(arg1, Class.String, "list::join", 1);

            var values = self.AsList.ConvertAll(x => RuntimeServices.ToString(x));
            string s = string.Join(arg1.ToString(), values.ToArray());

            return new String(s);
        }

        private static Dynamic ListAddLogic(Dynamic[] arguments)
        {
            arguments[0].AsList.Add(arguments[1]);
            return Void.Value;
        }

        private static Dynamic ListInsertLogic(Dynamic[] arguments)
        {
            List<Dynamic> list = arguments[0].AsList;
            
            int index = arguments[1].AsInt32;
            while (index < 0) index += list.Count;

            list.Insert(index, arguments[2]);
            return Void.Value;
        }

        private static Dynamic ListInsertAllLogic(Dynamic[] arguments)
        {
            List<Dynamic> list = arguments[0].AsList;

            int index = arguments[1].AsInt32;
            while (index < 0) index += list.Count;

            list.InsertRange(index, arguments[2].AsList);
            return Void.Value;
        }

        private static Dynamic ListIndexOfLogic(Dynamic[] arguments)
        {
            var self = arguments[0].AsList;

            var start = arguments[2].AsInt32;
            while (start < 0) start += self.Count;

            var count = arguments[3].AsInt32;
            if (count <= 0) count = self.Count - start;

            return new Integer(self.IndexOf(arguments[1], start, count));
        }

        private static Dynamic ListLastIndexOfLogic(Dynamic[] arguments)
        {
            var self = arguments[0].AsList;

            var start = arguments[2].AsInt32;
            while (start < 0) start += self.Count;

            var count = arguments[3].AsInt32;
            if (count <= 0) count = start + 1;

            return new Integer(self.LastIndexOf(arguments[1], start, count));
        }

        private static Dynamic ListBinarySearchLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].AsList.BinarySearch(arguments[1]));
        }

        private static Dynamic ListFrequencyOfLogic(Dynamic[] arguments)
        {
            var self = arguments[0].AsList;

            var start = arguments[2].AsInt32;
            while (start < 0) start += self.Count;

            var count = arguments[3].AsInt32;
            if (count <= 0) count = self.Count - start;

            int frequency = 0;
            for (int i = start, j = 0; j < count; ++i, ++j)
                if (self[i].Equals(arguments[1]))
                    ++frequency;

            return new Integer(frequency);
        }

        private static Dynamic ListRemoveLogic(Dynamic[] arguments)
        {
            bool b = arguments[0].AsList.Remove(arguments[1]);
            return Boolean.FromBool(b);
        }

        private static Dynamic ListRemoveAtLogic(Dynamic[] arguments)
        {
            List<Dynamic> list = arguments[0].AsList;
            int count = arguments[2].AsInt32;

            int index = arguments[1].AsInt32;
            while (index < 0) index += list.Count;

            if (count <= 1)
                list.RemoveAt(index);
            else
                list.RemoveRange(index, count);

            return Void.Value;
        }

        private static Dynamic ListClearLogic(Dynamic[] arguments)
        {
            arguments[0].AsList.Clear();
            return Void.Value;
        }

        private static Dynamic ListCountLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].AsList.Count);
        }

        private static Dynamic ListSortLogic(Dynamic[] arguments)
        {
            Dynamic self = arguments[0], arg1 = arguments[1];
            Comparison<Dynamic> cmp = RuntimeServices.CompareTo;

            if (arg1 != Void.Value)
            {
                CheckArgType(arg1, Class.Closure, "list::sort", 1);
                Type cmpType = typeof(Comparison<Dynamic>);
                Delegate cmpDelegate = arg1.AsFunction.ToDelegate(cmpType);
                cmp = (Comparison<Dynamic>) cmpDelegate;
            }

            var sorted = (Dynamic) self.Clone();
            sorted.AsList.Sort(cmp);
            return sorted;
        }

        private static Dynamic ListShuffleLogic(Dynamic[] arguments)
        {
            return new List(arguments[0].AsList.OrderBy(x => NextUInt32(random)).ToList());
        }

        private static Dynamic ListInverseLogic(Dynamic[] arguments)
        {
            var inverse = (Dynamic) arguments[0].Clone();
            inverse.AsList.Reverse();
            return inverse;
        }

        private static Dynamic ListSublistLogic(Dynamic[] arguments)
        {
            List<Dynamic> self = arguments[0].AsList;

            int index = arguments[1].AsInt32;
            while (index < 0) index += self.Count;

            return new List(self.GetRange(index, arguments[2].AsInt32));
        }

        private static Dynamic ListUniqueLogic(Dynamic[] arguments)
        {
            List<Dynamic> self = arguments[0].AsList;
            var unique = new List<Dynamic>();

            foreach (Dynamic item in self)
                if (!unique.Contains(item))
                    unique.Add(item);

            return new List(unique);
        }

        private static Dynamic ListMapToLogic(Dynamic[] arguments)
        {
            List<Dynamic> self = arguments[0].AsList;
            List<Dynamic> other = arguments[1].AsList;
            if (self.Count != other.Count)
                throw new ArgumentException("list::mapTo requires that both lists be of the same length");

            var dict = new Dictionary<Dynamic, Dynamic>();
            for (int i = 0; i < self.Count; ++i)
                dict.Add(self[i], other[i]);
            
            return new Map(dict);
        }

        #endregion

        #region Map specific methods

        private static Dynamic MapContainsKeyLogic(Dynamic[] arguments)
        {
            bool b = arguments[0].AsDictionary.ContainsKey(arguments[1]);
            return Boolean.FromBool(b);
        }

        private static Dynamic MapContainsValueLogic(Dynamic[] arguments)
        {
            bool b = arguments[0].AsDictionary.ContainsValue(arguments[1]);
            return Boolean.FromBool(b);
        }

        private static Dynamic MapKeysLogic(Dynamic[] arguments)
        {
            var keySet = new HashSet<Dynamic>();
            var keys = arguments[0].AsDictionary.Keys;
            
            foreach (Dynamic key in keys)
                keySet.Add(key);
            
            return new Set(keySet);
        }

        private static Dynamic MapValuesLogic(Dynamic[] arguments)
        {
            var valueSet = new HashSet<Dynamic>();
            var values = arguments[0].AsDictionary.Values;
            
            foreach (Dynamic value in values)
                if (!valueSet.Contains(value))
                    valueSet.Add(value);

            return new Set(valueSet);
        }

        private static Dynamic MapFrequencyOfLogic(Dynamic[] arguments)
        {
            int frequency = 0;
            var dict = arguments[0].AsDictionary;

            foreach (KeyValuePair<Dynamic, Dynamic> pair in dict)
                if (pair.Value.Equals(arguments[1]))
                    ++frequency;

            return new Integer(frequency);
        }

        private static Dynamic MapKeysOfLogic(Dynamic[] arguments)
        {
            var keySet = new HashSet<Dynamic>();
            var map = arguments[0].AsDictionary;

            foreach (KeyValuePair<Dynamic, Dynamic> pair in map)
                if (pair.Value.Equals(arguments[1]))
                    keySet.Add(pair.Key);

            return new Set(keySet);
        }

        private static Dynamic MapInverseLogic(Dynamic[] arguments)
        {
            var inverse = new Dictionary<Dynamic, Dynamic>();
            var map = arguments[0].AsDictionary;

            foreach (KeyValuePair<Dynamic, Dynamic> pair in map)
                if (!inverse.ContainsKey(pair.Value))
                    inverse.Add(pair.Value, pair.Key);

            return new Map(inverse);
        }

        private static Dynamic MapRemoveLogic(Dynamic[] arguments)
        {
            bool b = arguments[0].AsDictionary.Remove(arguments[1]);
            return Boolean.FromBool(b);
        }

        private static Dynamic MapRemoveAllLogic(Dynamic[] arguments)
        {
            var self = arguments[0].AsDictionary;
            int removed = 0;
            
            foreach (Dynamic key in arguments[1].AsHashSet)
                if (self.Remove(key))
                    ++removed;

            return new Integer(removed);
        }

        private static Dynamic MapClearLogic(Dynamic[] arguments)
        {
            arguments[0].AsDictionary.Clear();
            return Void.Value;
        }

        private static Dynamic MapCountLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].AsDictionary.Count);
        }

        #endregion

        #region Set specific methods

        private static Dynamic SetAddLogic(Dynamic[] arguments)
        {
            arguments[0].AsHashSet.Add(arguments[1]);
            return Void.Value;
        }

        private static Dynamic SetRemoveLogic(Dynamic[] arguments)
        {
            bool b = arguments[0].AsHashSet.Remove(arguments[1]);
            return Boolean.FromBool(b);
        }

        private static Dynamic SetClearLogic(Dynamic[] arguments)
        {
            arguments[0].AsHashSet.Clear();
            return Void.Value;
        }

        private static Dynamic SetCountLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].AsHashSet.Count);
        }

        #endregion

        #region Queue specific methods

        private static Dynamic QueueEnqueueLogic(Dynamic[] arguments)
        {
            arguments[0].AsQueue.Enqueue(arguments[1]);
            return Void.Value;
        }

        private static Dynamic QueuePeekLogic(Dynamic[] arguments)
        {
            return arguments[0].AsQueue.Peek();
        }

        private static Dynamic QueueDequeueLogic(Dynamic[] arguments)
        {
            return arguments[0].AsQueue.Dequeue();
        }

        private static Dynamic QueueClearLogic(Dynamic[] arguments)
        {
            arguments[0].AsQueue.Clear();
            return Void.Value;
        }

        private static Dynamic QueueCountLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].AsQueue.Count);
        }

        #endregion

        #region Stack specific methods

        private static Dynamic StackPushLogic(Dynamic[] arguments)
        {
            arguments[0].AsStack.Push(arguments[1]);
            return Void.Value;
        }

        private static Dynamic StackPeekLogic(Dynamic[] arguments)
        {
            return arguments[0].AsStack.Peek();
        }

        private static Dynamic StackPopLogic(Dynamic[] arguments)
        {
            return arguments[0].AsStack.Pop();
        }

        private static Dynamic StackClearLogic(Dynamic[] arguments)
        {
            arguments[0].AsStack.Clear();
            return Void.Value;
        }

        private static Dynamic StackCountLogic(Dynamic[] arguments)
        {
            return new Integer(arguments[0].AsStack.Count);
        }

        #endregion

        #endregion

        #region Predefined instances

        #region Global functions

       /// <summary>
        /// Gets a character from its Unicode rank.
        /// </summary>
        public static readonly InnerFunction Char = new InnerFunction("chr", new[] { new Parameter("ascii") }, CharLogic);

        /// <summary>
        /// Gets the Unicode rank of a character.
        /// </summary>
        public static readonly InnerFunction Order = new InnerFunction("ord", new[] { new Parameter("char") }, OrderLogic);

        /// <summary>
        /// Generates an random floatting-point number between 0 and 1.
        /// </summary>
        public static readonly InnerFunction Random = new InnerFunction("rand", Parameter.EmptyArray, RandomLogic);

        /// <summary>
        /// Generates an random integer comprised between the given boundaries.
        /// </summary>
        public static readonly InnerFunction RandomInteger = new InnerFunction("randint", new[] { new Parameter("min"), new Parameter("max", new Integer(0)) }, RandomIntegerLogic);

        /// <summary>
        /// Computes the sine of a number.
        /// </summary>
        public static readonly InnerFunction Sine = new InnerFunction("sin", new[] { new Parameter("x") }, SineLogic);

        /// <summary>
        /// Computes the cosine of a number.
        /// </summary>
        public static readonly InnerFunction Cosine = new InnerFunction("cos", new[] { new Parameter("x") }, CosineLogic);

        /// <summary>
        /// Computes the tangent of a number.
        /// </summary>
        public static readonly InnerFunction Tangent = new InnerFunction("tan", new[] { new Parameter("x") }, TangentLogic);

        /// <summary>
        /// Computes the arc sine of a number.
        /// </summary>
        public static readonly InnerFunction ArcSine = new InnerFunction("asin", new[] { new Parameter("x") }, ArcSineLogic);

        /// <summary>
        /// Computes the arc cosine of a number.
        /// </summary>
        public static readonly InnerFunction ArcCosine = new InnerFunction("acos", new[] { new Parameter("x") }, ArcCosineLogic);

        /// <summary>
        /// Computes the arc tangent of a number.
        /// </summary>
        public static readonly InnerFunction ArcTangent = new InnerFunction("atan", new[] { new Parameter("x") }, ArcTangentLogic);

        /// <summary>
        /// Computes the arc tangent of the ratio of two numbers.
        /// </summary>
        public static readonly InnerFunction ArcTangent2 = new InnerFunction("atan2", new[] { new Parameter("y"), new Parameter("x") }, ArcTangent2Logic);

        /// <summary>
        /// Computes the hyperbolic sine of a number.
        /// </summary>
        public static readonly InnerFunction SineHyperbolic = new InnerFunction("sinh", new[] { new Parameter("x") }, SineHyperbolicLogic);

        /// <summary>
        /// Computes the hyperbolic cosine of a number.
        /// </summary>
        public static readonly InnerFunction CosineHyperbolic = new InnerFunction("cosh", new[] { new Parameter("x") }, CosineHyperbolicLogic);

        /// <summary>
        /// Computes the hyperbolic tangent of a number.
        /// </summary>
        public static readonly InnerFunction TangentHyperbolic = new InnerFunction("tanh", new[] { new Parameter("x") }, TangentHyperbolicLogic);

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        public static readonly InnerFunction DegreesToRadians = new InnerFunction("deg2rad", new[] { new Parameter("x") }, DegreesToRadiansLogic);

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        public static readonly InnerFunction RadiansToDegrees = new InnerFunction("rad2deg", new[] { new Parameter("x") }, RadiansToDegreesLogic);

        /// <summary>
        /// Computes the natural logarithm of a number.
        /// </summary>
        public static readonly InnerFunction Logarithm = new InnerFunction("log", new[] { new Parameter("x") }, LogarithmLogic);

        /// <summary>
        /// Computes the logarithm on base 10 of a number.
        /// </summary>
        public static readonly InnerFunction LogarithmBase10 = new InnerFunction("log10", new[] { new Parameter("x") }, LogarithmBase10Logic);

        /// <summary>
        /// Computes the logarithm of a number on the given base.
        /// </summary>
        public static readonly InnerFunction LogarithmBaseN = new InnerFunction("log2", new[] { new Parameter("x"), new Parameter("base", new Float(2.0)) }, LogarithmBaseNLogic);

        /// <summary>
        /// Computes the exponential of a number.
        /// </summary>
        public static readonly InnerFunction Exponential = new InnerFunction("exp", new[] { new Parameter("x") }, ExponentialLogic);

        /// <summary>
        /// Computes the squared root of a number.
        /// </summary>
        public static readonly InnerFunction SquareRoot = new InnerFunction("sqrt", new[] { new Parameter("x") }, SquareRootLogic);

        /// <summary>
        /// Determines the sign (-1, 0 or 1) of a number.
        /// </summary>
        public static readonly InnerFunction Sign = new InnerFunction("sign", new[] { new Parameter("value") }, SignLogic);

        /// <summary>
        /// Determines the absolute value of a number.
        /// </summary>
        public static readonly InnerFunction AbsoluteValue = new InnerFunction("abs", new[] { new Parameter("value") }, AbsoluteValueLogic);

        /// <summary>
        /// Truncates a number.
        /// </summary>
        public static readonly InnerFunction Truncate = new InnerFunction("trunc", new[] { new Parameter("x") }, TruncateLogic);

        /// <summary>
        /// Determines the floor of a number.
        /// </summary>
        public static readonly InnerFunction Floor = new InnerFunction("floor", new[] { new Parameter("x") }, FloorLogic);

        /// <summary>
        /// Determines the ceiling of a number
        /// </summary>
        public static readonly InnerFunction Ceiling = new InnerFunction("ceil", new[] { new Parameter("x") }, CeilingLogic);

        /// <summary>
        /// Rounds off a number.
        /// </summary>
        public static readonly InnerFunction Round = new InnerFunction("round", new[] { new Parameter("value"), new Parameter("precision", new Integer(0)) }, RoundLogic);

        /// <summary>
        /// Determines the minimum of two number.
        /// </summary>
        public static readonly InnerFunction Minimum = new InnerFunction("min", new[] { new Parameter("values", false, true, null) }, MinimumLogic);

        /// <summary>
        /// Determines the maximum of two number.
        /// </summary>
        public static readonly InnerFunction Maximum = new InnerFunction("max", new[] { new Parameter("values", false, true, null) }, MaximumLogic);

        /// <summary>
        /// Creates a queue with the given initial content.
        /// </summary>
        public static readonly InnerFunction Fifo = new InnerFunction("fifo", new[] { new Parameter("values", false, true, null) }, FifoLogic);

        /// <summary>
        /// Creates a stack with the given initial content.
        /// </summary>
        public static readonly InnerFunction Lifo = new InnerFunction("lifo", new[] { new Parameter("values", false, true, null) }, LifoLogic);

        /// <summary>
        /// Gets the current date.
        /// </summary>
        public static readonly InnerFunction Now = new InnerFunction("now", Parameter.EmptyArray, NowLogic);

        /// <summary>
        /// Format a string with the given mask.
        /// </summary>
        public static readonly InnerFunction Format = new InnerFunction("format", new[] { new Parameter("mask"), new Parameter("values", false, true, null) }, FormatLogic);

        /// <summary>
        /// Reads a line of text from the standard input device.
        /// </summary>
        public static readonly InnerFunction ReadLine = new InnerFunction("readln", new[] { new Parameter("prompt", new String("")) }, ReadLineLogic);

        /// <summary>
        /// Prints some text to the standard output device.
        /// </summary>
        public static readonly InnerFunction Print = new InnerFunction("print", new[] { new Parameter("mask"), new Parameter("values", false, true, null) }, PrintLogic);

        /// <summary>
        /// Prints a line of text to the standard output device.
        /// </summary>
        public static readonly InnerFunction PrintLine = new InnerFunction("println", new[] { new Parameter("mask", new String("")), new Parameter("values", false, true, null) }, PrintLineLogic);

        /// <summary>
        /// Evaluates the expression contained in a string.
        /// </summary>
        public static readonly InnerFunction Evaluate = new InnerFunction("eval", new[] { new Parameter("expression") }, EvaluateLogic);

        /// <summary>
        /// Packs several values in a binary string: a way to create structured data objects.
        /// </summary>
        public static readonly InnerFunction Pack = new InnerFunction("pack", new[] { new Parameter("fmt"), new Parameter("values", false, true, null) }, PackLogic);

        /// <summary>
        /// Unpacks the values combined in a binary string following the given format.
        /// </summary>
        public static readonly InnerFunction UnPack = new InnerFunction("unpack", new[] { new Parameter("fmt"), new Parameter("str") }, UnPackLogic);

        #endregion

        #region Common methods

        /// <summary>
        /// Checks that both arguments are equal.
        /// </summary>
        public static readonly InnerFunction EqualsFunction = new InnerFunction("equals", new[] { new Parameter("self"), new Parameter("other") }, EqualsLogic);

        /// <summary>
        /// Gets the hashcode of its argument.
        /// </summary>
        public static readonly InnerFunction HashCodeFunction = new InnerFunction("hashCode", new[] { new Parameter("self") }, HashCodeLogic);

        /// <summary>
        /// Compares its arguments.
        /// </summary>
        public static readonly InnerFunction CompareToFunction = new InnerFunction("compareTo", new[] { new Parameter("self"), new Parameter("other") }, CompareToLogic);

        /// <summary>
        /// Gets the textual representation of its argument.
        /// </summary>
        public static readonly InnerFunction ToStringFunction = new InnerFunction("toString", new[] { new Parameter("self"), new Parameter("format", new String("")) }, ToStringLogic);

        /// <summary>
        /// Creates a deep copy of its argument.
        /// </summary>
        public static readonly InnerFunction CloneFunction = new InnerFunction("clone", new[] { new Parameter("self") }, CloneLogic);

        /// <summary>
        /// Releases the unmanaged resources allocated to its argument.
        /// </summary>
        public static readonly InnerFunction DisposeFunction = new InnerFunction("dispose", new[] { new Parameter("self") }, DisposeLogic);

        #endregion

        #region Rational specific methods

        /// <summary>
        /// Extracts the numerator of a rational number.
        /// </summary>
        public static readonly InnerFunction RationalNum = new InnerFunction("num", new[] { new Parameter("self") }, RationalNumLogic);

        /// <summary>
        /// Extracts the denominator of a rational number.
        /// </summary>
        public static readonly InnerFunction RationalDen = new InnerFunction("den", new[] { new Parameter("self") }, RationalDenLogic);

        /// <summary>
        /// Computes the inverse of a rational number.
        /// </summary>
        public static readonly InnerFunction RationalInverse = new InnerFunction("inverse", new[] { new Parameter("self") }, RationalInverseLogic);

        #endregion

        #region Complex specific methods

        /// <summary>
        /// Extracts the real part of a complex number.
        /// </summary>
        public static readonly InnerFunction ComplexReal = new InnerFunction("real", new[] { new Parameter("self") }, ComplexRealLogic);

        /// <summary>
        /// Extracts the imaginary part of a complex number.
        /// </summary>
        public static readonly InnerFunction ComplexImaginary = new InnerFunction("imag", new[] { new Parameter("self") }, ComplexImaginaryLogic);

        /// <summary>
        /// Computes the conjugate of a complex number.
        /// </summary>
        public static readonly InnerFunction ComplexConjugate = new InnerFunction("conjugate", new[] { new Parameter("self") }, ComplexConjugateLogic);

        #endregion

        #region Date specific methods

        /// <summary>
        /// Creates a date from the given values.
        /// </summary>
        public static readonly InnerFunction DateCreate = new InnerFunction("create", new[] { new Parameter("values", false, true, null) }, DateCreateLogic);

        /// <summary>
        /// Extracts the date part of a date/time value.
        /// </summary>
        public static readonly InnerFunction DateGetDate = new InnerFunction("date", new[] { new Parameter("self") }, DateGetDateLogic);

        /// <summary>
        /// Extracts the time part of a date/time value.
        /// </summary>
        public static readonly InnerFunction DateGetTime = new InnerFunction("time", new[] { new Parameter("self") }, DateGetTimeLogic);

        /// <summary>
        /// Extracts a date's part.
        /// </summary>
        public static readonly InnerFunction DateGet = new InnerFunction("get", new[] { new Parameter("self"), new Parameter("part") }, DateGetLogic);

        /// <summary>
        /// Gets the number of ticks of a date/time value.
        /// </summary>
        public static readonly InnerFunction DateGetTicks = new InnerFunction("ticks", new[] { new Parameter("self") }, DateGetTicksLogic);

        /// <summary>
        /// Adds some time units to a date.
        /// </summary>
        public static readonly InnerFunction DateAdd = new InnerFunction("add", new[] { new Parameter("self"), new Parameter("value"), new Parameter("unit") }, DateAddLogic);

        /// <summary>
        /// Adds some ticks to a date.
        /// </summary>
        public static readonly InnerFunction DateAddTicks = new InnerFunction("addTicks", new[] { new Parameter("self"), new Parameter("ticks") }, DateAddTicksLogic);

        /// <summary>
        /// Computes the difference between two dates.
        /// </summary>
        public static readonly InnerFunction DateSubtract = new InnerFunction("subtract", new[] { new Parameter("self"), new Parameter("other"), new Parameter("unit") }, DateSubtractLogic);

        #endregion

        #region String specific methods

        /// <summary>
        /// Determines the length of a string.
        /// </summary>
        public static readonly InnerFunction StringLength = new InnerFunction("length", new[] { new Parameter("self") }, StringLengthLogic);

        /// <summary>
        /// Searches for a string in another string.
        /// </summary>
        public static readonly InnerFunction StringIndexOf = new InnerFunction("indexOf", new[] { new Parameter("self"), new Parameter("value"), new Parameter("start", new Integer(0)), new Parameter("length", new Integer(0)) }, StringIndexOfLogic);

        /// <summary>
        /// Reversely searches for a string in another string.
        /// </summary>
        public static readonly InnerFunction StringLastIndexOf = new InnerFunction("lastIndexOf", new[] { new Parameter("self"), new Parameter("value"), new Parameter("start", new Integer(-1)), new Parameter("length", new Integer(0)) }, StringLastIndexOfLogic);

        /// <summary>
        /// Converts all characters in a string to lowercase equivalent.
        /// </summary>
        public static readonly InnerFunction StringToLower = new InnerFunction("toLower", new[] { new Parameter("self") }, StringToLowerLogic);

        /// <summary>
        /// Converts all characters in a string to uppercase equivalent.
        /// </summary>
        public static readonly InnerFunction StringToUpper = new InnerFunction("toUpper", new[] { new Parameter("self") }, StringToUpperLogic);

        /// <summary>
        /// Capitalzes a string.
        /// </summary>
        public static readonly InnerFunction StringCapitalize = new InnerFunction("capitalize", new[] { new Parameter("self") }, StringCapitalizeLogic);

        /// <summary>
        /// Uncapitalizes a string.
        /// </summary>
        public static readonly InnerFunction StringUncapitalize = new InnerFunction("uncapitalize", new[] { new Parameter("self") }, StringUncapitalizeLogic);

        /// <summary>
        /// Extract a substring.
        /// </summary>
        public static readonly InnerFunction StringSubstring = new InnerFunction("substring", new[] { new Parameter("self"), new Parameter("start"), new Parameter("length", new Integer(0)) }, StringSubstringLogic);

        /// <summary>
        /// Inserts a string in another at a given position.
        /// </summary>
        public static readonly InnerFunction StringInsert = new InnerFunction("insert", new[] { new Parameter("self"), new Parameter("index"), new Parameter("value") }, StringInsertLogic);

        /// <summary>
        /// Deletes a sequence of characters from within a string.
        /// </summary>
        public static readonly InnerFunction StringRemove = new InnerFunction("remove", new[] { new Parameter("self"), new Parameter("index"), new Parameter("count", new Integer(0)) }, StringRemoveLogic);

        /// <summary>
        /// Replaces of occurences of some substring with another substring.
        /// </summary>
        public static readonly InnerFunction StringReplace = new InnerFunction("replace", new[] { new Parameter("self"), new Parameter("pattern"), new Parameter("value") }, StringReplaceLogic);

        /// <summary>
        /// Removes the given characters from the left of a string.
        /// </summary>
        public static readonly InnerFunction StringTrimLeft = new InnerFunction("ltrim", new[] { new Parameter("self"), new Parameter("chars", new String(" ")) }, StringTrimLeftLogic);

        /// <summary>
        /// Removes the given characters from the right of a string.
        /// </summary>
        public static readonly InnerFunction StringTrimRight = new InnerFunction("rtrim", new[] { new Parameter("self"), new Parameter("chars", new String(" ")) }, StringTrimRightLogic);

        /// <summary>
        /// Removes the given characters around a string.
        /// </summary>
        public static readonly InnerFunction StringTrim = new InnerFunction("trim", new[] { new Parameter("self"), new Parameter("chars", new String(" ")) }, StringTrimLogic);

        /// <summary>
        /// Adds the given characters to the left of a string until it reaches the given width.
        /// </summary>
        public static readonly InnerFunction StringPadLeft = new InnerFunction("lpad", new[] { new Parameter("self"), new Parameter("width"), new Parameter("padding", new String(" ")) }, StringPadLeftLogic);

        /// <summary>
        /// Adds the given characters to the right of a string until it reaches the given width.
        /// </summary>
        public static readonly InnerFunction StringPadRight = new InnerFunction("rpad", new[] { new Parameter("self"), new Parameter("width"), new Parameter("padding", new String(" ")) }, StringPadRightLogic);

        /// <summary>
        /// Split a string into substrings separated by the given pattern.
        /// </summary>
        public static readonly InnerFunction StringSplit = new InnerFunction("split", new[] { new Parameter("self"), new Parameter("pattern", new String("/\\s+/")) }, StringSplitLogic);

        #endregion

        #region List specific methods

        /// <summary>
        /// Creates an empty list with the given size.
        /// </summary>
        public static readonly InnerFunction ListCreate = new InnerFunction("create", new[] { new Parameter("size") }, ListCreateLogic);

        /// <summary>
        /// Joins several strings into one.
        /// </summary>
        public static readonly InnerFunction ListJoin = new InnerFunction("join", new[] { new Parameter("self"), new Parameter("separator", new String("")) }, ListJoinLogic);

        /// <summary>
        /// Adds an item in a list.
        /// </summary>
        public static readonly InnerFunction ListAdd = new InnerFunction("add", new[] { new Parameter("self"), new Parameter("value") }, ListAddLogic);

        /// <summary>
        /// Inserts an item in a list at the specified position.
        /// </summary>
        public static readonly InnerFunction ListInsert = new InnerFunction("insert", new[] { new Parameter("self"), new Parameter("index"), new Parameter("value") }, ListInsertLogic);

        /// <summary>
        /// Inserts a list in anoter list at the specified position.
        /// </summary>
        public static readonly InnerFunction ListInsertAll = new InnerFunction("insertAll", new[] { new Parameter("self"), new Parameter("index"), new Parameter("other") }, ListInsertAllLogic);

        /// <summary>
        /// Gets the first index of an item in a list.
        /// </summary>
        public static readonly InnerFunction ListIndexOf = new InnerFunction("indexOf", new[] { new Parameter("self"), new Parameter("value"), new Parameter("start", new Integer(0)), new Parameter("count", new Integer(0)) }, ListIndexOfLogic);

        /// <summary>
        /// Gets the last index of an item in a list.
        /// </summary>
        public static readonly InnerFunction ListLastIndexOf = new InnerFunction("lastIndexOf", new[] { new Parameter("self"), new Parameter("value"), new Parameter("start", new Integer(-1)), new Parameter("count", new Integer(0)) }, ListLastIndexOfLogic);

        /// <summary>
        /// Makes a binary search on a sorted list.
        /// </summary>
        public static readonly InnerFunction ListBinarySearch = new InnerFunction("bsearch", new[] { new Parameter("self"), new Parameter("value"), new Parameter("start") }, ListBinarySearchLogic);

        /// <summary>
        /// Gets the number of occurences of a value in a list.
        /// </summary>
        public static readonly InnerFunction ListFrequencyOf = new InnerFunction("frequencyOf", new[] { new Parameter("self"), new Parameter("value"), new Parameter("start", new Integer(0)), new Parameter("count", new Integer(0)) }, ListFrequencyOfLogic);

        /// <summary>
        /// Removes an item from a list.
        /// </summary>
        public static readonly InnerFunction ListRemove = new InnerFunction("remove", new[] { new Parameter("self"), new Parameter("value") }, ListRemoveLogic);

        /// <summary>
        /// Removes an item  or a range of items from a list at the given position.
        /// </summary>
        public static readonly InnerFunction ListRemoveAt = new InnerFunction("removeAt", new[] { new Parameter("self"), new Parameter("index"), new Parameter("count", new Integer(1)) }, ListRemoveAtLogic);

        /// <summary>
        /// Clears the content of a list.
        /// </summary>
        public static readonly InnerFunction ListClear = new InnerFunction("clear", new[] { new Parameter("self") }, ListClearLogic);

        /// <summary>
        /// Gets the number of items in a list.
        /// </summary>
        public static readonly InnerFunction ListCount = new InnerFunction("count", new[] { new Parameter("self") }, ListCountLogic);

        /// <summary>
        /// Sorts a list in ascending order.
        /// </summary>
        public static readonly InnerFunction ListSort = new InnerFunction("sort", new[] { new Parameter("self"), new Parameter("comparison", Void.Value) }, ListSortLogic);

        /// <summary>
        /// Gets the inverse of a list.
        /// </summary>
        public static readonly InnerFunction ListInverse = new InnerFunction("inverse", new[] { new Parameter("self") }, ListInverseLogic);

        /// <summary>
        /// Gets a sublist of a list.
        /// </summary>
        public static readonly InnerFunction ListSublist = new InnerFunction("sublist", new[] { new Parameter("self"), new Parameter("index"), new Parameter("count") }, ListSublistLogic);

        /// <summary>
        /// Gets a copy of the calling list where any item is unique.
        /// </summary>
        public static readonly InnerFunction ListUnique = new InnerFunction("unique", new[] { new Parameter("self") }, ListUniqueLogic);

        /// <summary>
        /// Maps a list to another.
        /// </summary>
        public static readonly InnerFunction ListMapTo = new InnerFunction("mapTo", new[] { new Parameter("self"), new Parameter("other") }, ListMapToLogic);

        /// <summary>
        /// Sorts a list in a random order.
        /// </summary>
        public static readonly InnerFunction ListShuffle = new InnerFunction("shuffle", new[] { new Parameter("self") }, ListShuffleLogic);

        #endregion

        #region Map specific methods

        /// <summary>
        /// Checks if a map contains some key.
        /// </summary>
        public static readonly InnerFunction MapContainsKey = new InnerFunction("containsKey", new[] { new Parameter("self"), new Parameter("key") }, MapContainsKeyLogic);

        /// <summary>
        /// Checks if a map contains some value.
        /// </summary>
        public static readonly InnerFunction MapContainsValue = new InnerFunction("containsValue", new[] { new Parameter("self"), new Parameter("value") }, MapContainsValueLogic);

        /// <summary>
        /// Gets the set of keys of a map.
        /// </summary>
        public static readonly InnerFunction MapKeys = new InnerFunction("keys", new[] { new Parameter("self") }, MapKeysLogic);

        /// <summary>
        /// Gets the set of values of a map.
        /// </summary>
        public static readonly InnerFunction MapValues = new InnerFunction("values", new[] { new Parameter("self") }, MapValuesLogic);

        /// <summary>
        /// Gets the number of occurences of a value in a map.
        /// </summary>
        public static readonly InnerFunction MapFrequencyOf = new InnerFunction("frequencyOf", new[] { new Parameter("self"), new Parameter("value") }, MapFrequencyOfLogic);

        /// <summary>
        /// Gets the set of distinct keys to which a value is bound in a map.
        /// </summary>
        public static readonly InnerFunction MapKeysOf = new InnerFunction("keysOf", new[] { new Parameter("self"), new Parameter("value") }, MapKeysOfLogic);

        /// <summary>
        /// Creates a new map from the given one where key-value pairs are reversed.
        /// </summary>
        public static readonly InnerFunction MapInverse = new InnerFunction("inverse", new[] { new Parameter("self") }, MapInverseLogic);

        /// <summary>
        /// Remove a key-value pair from a map.
        /// </summary>
        public static readonly InnerFunction MapRemove = new InnerFunction("remove", new[] { new Parameter("self"), new Parameter("key") }, MapRemoveLogic);

        /// <summary>
        /// Remove a set of key-value pairs from a map.
        /// </summary>
        public static readonly InnerFunction MapRemoveAll = new InnerFunction("removeAll", new[] { new Parameter("self"), new Parameter("keys") }, MapRemoveAllLogic);

        /// <summary>
        /// Empties a map.
        /// </summary>
        public static readonly InnerFunction MapClear = new InnerFunction("clear", new[] { new Parameter("self") }, MapClearLogic);

        /// <summary>
        /// Gets the number of key-value pairs of a map.
        /// </summary>
        public static readonly InnerFunction MapCount = new InnerFunction("count", new[] { new Parameter("self") }, MapCountLogic);

        #endregion

        #region Set specific methods

        /// <summary>
        /// Adds an item in a set.
        /// </summary>
        public static readonly InnerFunction SetAdd = new InnerFunction("add", new[] { new Parameter("self"), new Parameter("value") }, SetAddLogic);

        /// <summary>
        /// Removes an item from a set.
        /// </summary>
        public static readonly InnerFunction SetRemove = new InnerFunction("remove", new[] { new Parameter("self"), new Parameter("value") }, SetRemoveLogic);

        /// <summary>
        /// Clears the content of a set.
        /// </summary>
        public static readonly InnerFunction SetClear = new InnerFunction("clear", new[] { new Parameter("self") }, SetClearLogic);

        /// <summary>
        /// Gets the number of items in a set.
        /// </summary>
        public static readonly InnerFunction SetCount = new InnerFunction("count", new[] { new Parameter("self") }, SetCountLogic);

        #endregion

        #region Queue specific methods

        /// <summary>
        /// Enqueues an item in a set.
        /// </summary>
        public static readonly InnerFunction QueueEnqueue = new InnerFunction("enqueue", new[] { new Parameter("self"), new Parameter("value") }, QueueEnqueueLogic);

        /// <summary>
        /// Checks if a set contains some value.
        /// </summary>
        public static readonly InnerFunction QueuePeek = new InnerFunction("peek", new[] { new Parameter("self") }, QueuePeekLogic);

        /// <summary>
        /// Dequeues an item from a set.
        /// </summary>
        public static readonly InnerFunction QueueDequeue = new InnerFunction("dequeue", new[] { new Parameter("self") }, QueueDequeueLogic);

        /// <summary>
        /// Clears the content of a set.
        /// </summary>
        public static readonly InnerFunction QueueClear = new InnerFunction("clear", new[] { new Parameter("self") }, QueueClearLogic);

        /// <summary>
        /// Gets the number of items in a set.
        /// </summary>
        public static readonly InnerFunction QueueCount = new InnerFunction("count", new[] { new Parameter("self") }, QueueCountLogic);

        #endregion

        #region Stack specific methods

        /// <summary>
        /// Pushs an item in a set.
        /// </summary>
        public static readonly InnerFunction StackPush = new InnerFunction("push", new[] { new Parameter("self"), new Parameter("value") }, StackPushLogic);

        /// <summary>
        /// Checks if a set contains some value.
        /// </summary>
        public static readonly InnerFunction StackPeek = new InnerFunction("peek", new[] { new Parameter("self") }, StackPeekLogic);

        /// <summary>
        /// Pops an item from a set.
        /// </summary>
        public static readonly InnerFunction StackPop = new InnerFunction("pop", new[] { new Parameter("self") }, StackPopLogic);

        /// <summary>
        /// Clears the content of a set.
        /// </summary>
        public static readonly InnerFunction StackClear = new InnerFunction("clear", new[] { new Parameter("self") }, StackClearLogic);

        /// <summary>
        /// Gets the number of items in a set.
        /// </summary>
        public static readonly InnerFunction StackCount = new InnerFunction("count", new[] { new Parameter("self") }, StackCountLogic);

        #endregion

        #endregion

        #region Creation of callable wrappers

        /// <summary>
        /// Generates a wrapper <see cref="Function"/> to make an inner function callable with the standard syntax.
        /// </summary>
        /// <returns>A <see cref="Function"/></returns>
        public Function ToFunction()
        {
            var arguments = new Expression[Parameters.Length];

            for (int i = 0; i < Parameters.Length; ++i)
                arguments[i] = new VariableRef(Parameters[i].Name);

            return new Function(Parameters,
                                Block.Return(new InnerFunctionCall(this, arguments)));
        }

        /// <summary>
        /// Generates a wrapper <see cref="Function"/> to make an inner function callable as a method.
        /// The first arguments is assumed to be the caller; so it's fixed to a <see cref="ThisReference"/>
        /// </summary>
        /// <returns>A <see cref="Function"/></returns>
        public Function ToMethodFunction()
        {
            var arguments = new Expression[Parameters.Length];
            arguments[0] = new ThisReference();

            for (int i = 1; i < Parameters.Length; ++i)
                arguments[i] = new VariableRef(Parameters[i].Name);

            var parameters = new List<Parameter>(Parameters);
            parameters.RemoveAt(0);

            return new Function(parameters.ToArray(),
                                Block.Return(new InnerFunctionCall(this, arguments)));
        }

        /// <summary>
        /// Generates an equivalent static method for a class.
        /// </summary>
        /// <returns>A <see cref="ClassMethod"/></returns>
        public ClassMethod ToStaticMethod()
        {
            return new ClassMethod(Name, Scope.Public, Modifier.Static, ToFunction());
        }

        /// <summary>
        /// Wraps an inner function into an instance method.
        /// </summary>
        /// <returns>A <see cref="ClassMethod"/></returns>
        public ClassMethod ToInstanceMethod()
        {
            return new ClassMethod(Name, Scope.Public, Modifier.Default, ToMethodFunction());
        }

        /// <summary>
        /// Generates an equivalent read-only static property for a class.
        /// </summary>
        /// <returns>A <see cref="ClassProperty"/></returns>
        public ClassProperty ToStaticProperty()
        {
            var getter = new ClassMethod(ClassProperty.GetReaderName(Name), Scope.Public, Modifier.Static, ToFunction());
            return new ClassProperty(Name, Scope.Public, Modifier.Static, getter, null);
        }

        /// <summary>
        /// Wraps an inner function into an instance read-only property.
        /// </summary>
        /// <returns>A <see cref="ClassProperty"/></returns>
        public ClassProperty ToInstanceProperty()
        {
            var getter = new ClassMethod(ClassProperty.GetReaderName(Name), Scope.Public, Modifier.Default, ToMethodFunction());
            return new ClassProperty(Name, Scope.Public, Modifier.Default, getter, null);
        }

        #endregion

        #region Utility

        private static uint NextUInt32(RNGCryptoServiceProvider rnd)
        {
            var uintBytes = new byte[4];
            rnd.GetBytes(uintBytes);
            return BitConverter.ToUInt32(uintBytes, 0);
        }

        private static double NextDouble(RNGCryptoServiceProvider rnd)
        {
            return (double) NextUInt32(rnd) / uint.MaxValue;
        }

        private static void CheckArgType(Dynamic arg, Class klass, string function, int rank)
        {
            if (arg.Class != klass)
                throw new ArgumentException(string.Format(Resources.ArgMustBeOfType, rank, function, klass.Name));
        }

        private static void CheckSingleChar(string s, string function, int rank)
        {
            if (s.Length != 1)
                throw new ArgumentException(string.Format(Resources.SingleCharExpected, rank, function));
        }

        private static string FormatList(string mask, List<Dynamic> values)
        {
            var sbResult = new StringBuilder();
            var sbItem = new StringBuilder();
            int i = 0, l = mask.Length;

            while (i < l)
            {
                char ch = mask[i];
                switch (ch)
                {
                    case '{':
                        if (i >= l - 1) throw new FormatException();
                        if (mask[i + 1] == ch)
                        {
                            sbResult.Append(ch);
                            i += 2;
                        }
                        else
                        {
                            int j = i + 1;
                            if (mask[j] == '}') throw new FormatException();

                            for (; j < l && mask[j] != '}'; ++j) sbItem.Append(mask[j]);
                            if (j >= l) throw new FormatException();

                            string value = FormatItem(sbItem.ToString(), values);
                            sbResult.Append(value);
                            sbItem.Remove(0, sbItem.Length);
                            i = j + 1;
                        }
                        break;
                    case '}':
                        if (i >= l - 1 || mask[i + 1] != ch) throw new FormatException();
                        sbResult.Append(ch);
                        i += 2;
                        break;
                    default:
                        sbResult.Append(ch);
                        ++i;
                        break;
                }
            }

            return sbResult.ToString();
        }

        private static string FormatItem(string mask, List<Dynamic> values)
        {
            int index, length = 0;
            string format = string.Empty;

            string[] parts = mask.Split(',');
            if (parts.Length == 2)
            {
                index = int.Parse(parts[0]);
                length = int.Parse(parts[1]);
            }
            else
            {
                parts = mask.Split(':');
                if (parts.Length == 2)
                {
                    index = int.Parse(parts[0]);
                    format = parts[1];
                }
                else
                    index = int.Parse(mask);
            }

            var value = RuntimeServices.ToString(values[index], format);
            if (length < 0)
                value = value.PadRight(-length);
            else if (length > 0)
                value = value.PadLeft(length);

            return value;
        }

        private static int YearDiff(DateTime d1, DateTime d2)
        {
            int yearDiff = d1.Year - d2.Year;
            int monthDiff = d1.Month - d2.Month;
            int dayDiff = d1.Day - d2.Day;

            if (monthDiff < 0 || (monthDiff == 0 & dayDiff < 0))
                --yearDiff;

            return yearDiff;
        }

        private static int MonthDiff(DateTime d1, DateTime d2)
        {
            int yearDiff = d1.Year - d2.Year;
            int monthDiff = d1.Month - d2.Month;
            int dayDiff = d1.Day - d2.Day;

            if (monthDiff < 0)
            {
                monthDiff += 12;
                --yearDiff;
            }

            if (dayDiff < 0) --monthDiff;

            return 12 * yearDiff + monthDiff;
        }

        #endregion
    }
}