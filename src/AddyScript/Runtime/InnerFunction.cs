using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Complex64 = System.Numerics.Complex;

using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Parsers;
using AddyScript.Properties;
using AddyScript.Runtime.DataItems;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;
using Boolean = AddyScript.Runtime.DataItems.Boolean;
using Complex = AddyScript.Runtime.DataItems.Complex;
using Decimal = AddyScript.Runtime.DataItems.Decimal;
using String = AddyScript.Runtime.DataItems.String;
using Void = AddyScript.Runtime.DataItems.Void;


namespace AddyScript.Runtime;


/// <summary>
/// A method that can be wrapped in an instance of <see cref="InnerFunction"/>
/// </summary>
/// <param name="arguments">The arguments passed to the function when it's called</param>
/// <returns>A <see cref="DataItem"/></returns>
public delegate DataItem InnerFunctionLogic(DataItem[] arguments);

/// <summary>
/// An operation that is handled by the scripting engine as an atomic action.
/// </summary>
/// <remarks>
/// Initializes a new instance of InnerFunction
/// </remarks>
/// <param name="name">The name of this function</param>
/// <param name="parameters">The set of parameters required for a call to this function</param>
/// <param name="logic">The logic of this function</param>
public class InnerFunction(string name, Parameter[] parameters, InnerFunctionLogic logic)
{
    #region Fields

    /// <summary>
    /// A registry for global instances of <see cref="InnerFunction"/>.
    /// </summary>
    public static readonly List<InnerFunction> Globals;

    /// <summary>
    /// The random numbers generator.
    /// </summary>
    private static readonly RandomNumberGenerator random = RandomNumberGenerator.Create();

    #endregion

    #region Initialization

    /// <summary>
    /// Class initializer: registers global functions and attaches methods to corresponding classes.
    /// </summary>
    static InnerFunction()
    {
        Globals = [
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
            Now,
            Format,
            ReadLine,
            Print,
            PrintLine,
            Evaluate,
            Pack,
            UnPack
        ];

        InnerFunction[] commonFunctions = [EqualsFunction, HashCodeFunction, CompareToFunction, ToStringFunction, CloneFunction, DisposeFunction];
        InnerFunction[] dateProperties = [DateGetDate, DateGetTime, DateGetTicks];
        InnerFunction[] dateFunctions = [DateAdd, DateAddTicks, DateSubtract];
        InnerFunction[] stringFunctions = [StringIndexOf, StringLastIndexOf, StringToLower, StringToUpper, StringCapitalize, StringUncapitalize, StringSubstring, StringInsert, StringRemove, StringReplace, StringTrimLeft, StringTrimRight, StringTrim, StringPadLeft, StringPadRight, StringSplit];
        InnerFunction[] blobStaticFunctions = [BlobOf, BlobFromHexString, BlobFromBase64String];
        InnerFunction[] blobFunctions = [BlobToHexString, BlobToBase64String, BlobIndexOf, BlobLastIndexOf, BlobFill, BlobCopyTo, BlobResize];
        InnerFunction[] tupleFunctions = [TupleIndexOf, TupleLastIndexOf];
        InnerFunction[] listFunctions = [ListJoin, ListAdd, ListInsert, ListInsertAll, ListIndexOf, ListLastIndexOf, ListBinarySearch, ListFrequencyOf, ListRemove, ListRemoveAt, ListClear, ListSort, ListShuffle, ListInverse, ListSublist, ListUnique, ListMapTo];
        InnerFunction[] mapProperties = [MapSize, MapKeys, MapValues];
        InnerFunction[] setFunctions = [SetAdd, SetRemove, SetClear];
        InnerFunction[] queueFunctions = [QueueEnqueue, QueuePeek, QueueDequeue, QueueClear];
        InnerFunction[] stackFunctions = [StackPush, StackPeek, StackPop, StackClear];
        InnerFunction[] mapFunctions = [MapContainsKey, MapContainsValue, MapFrequencyOf, MapKeysOf, MapInverse, MapRemove, MapRemoveAll, MapClear];

        foreach (InnerFunction function in commonFunctions)
            foreach (Class cls in Class.Predefined)
                if (cls.SuperClass == null)
                    cls.RegisterMethod(function.ToInstanceMethod());

        Class.Rational.RegisterProperty(RationalNum.ToInstanceProperty());
        Class.Rational.RegisterProperty(RationalDen.ToInstanceProperty());
        Class.Rational.RegisterMethod(RationalInverse.ToInstanceMethod());

        Class.Complex.RegisterProperty(ComplexReal.ToInstanceProperty());
        Class.Complex.RegisterProperty(ComplexImaginary.ToInstanceProperty());
        Class.Complex.RegisterMethod(ComplexOf.ToStaticMethod());
        Class.Complex.RegisterMethod(ComplexConjugate.ToInstanceMethod());

        Class.Date.RegisterIndexer(DateGet.ToIndexer());
        Class.Date.RegisterMethod(DateOf.ToStaticMethod());
        foreach (InnerFunction function in dateProperties)
            Class.Date.RegisterProperty(function.ToInstanceProperty());
        foreach (InnerFunction function in dateFunctions)
            Class.Date.RegisterMethod(function.ToInstanceMethod());

        Class.String.RegisterProperty(StringLength.ToInstanceProperty());
        foreach (InnerFunction function in stringFunctions)
            Class.String.RegisterMethod(function.ToInstanceMethod());

        Class.Blob.RegisterProperty(BlobLength.ToInstanceProperty());
        foreach (InnerFunction function in blobStaticFunctions)
            Class.Blob.RegisterMethod(function.ToStaticMethod());
        foreach (InnerFunction function in blobFunctions)
            Class.Blob.RegisterMethod(function.ToInstanceMethod());

        Class.Tuple.RegisterProperty(TupleSize.ToInstanceProperty());
        foreach (InnerFunction function in tupleFunctions)
            Class.Tuple.RegisterMethod(function.ToInstanceMethod());

        Class.List.RegisterProperty(ListSize.ToInstanceProperty());
        foreach (InnerFunction function in listFunctions)
            Class.List.RegisterMethod(function.ToInstanceMethod());

        Class.Set.RegisterProperty(SetSize.ToInstanceProperty());
        foreach (InnerFunction function in setFunctions)
            Class.Set.RegisterMethod(function.ToInstanceMethod());

        Class.Queue.RegisterProperty(QueueSize.ToInstanceProperty());
        Class.Queue.RegisterMethod(QueueOf.ToStaticMethod());
        foreach (InnerFunction function in queueFunctions)
            Class.Queue.RegisterMethod(function.ToInstanceMethod());

        Class.Stack.RegisterProperty(StackSize.ToInstanceProperty());
        Class.Stack.RegisterMethod(StackOf.ToStaticMethod());
        foreach (InnerFunction function in stackFunctions)
            Class.Stack.RegisterMethod(function.ToInstanceMethod());

        foreach (InnerFunction function in mapProperties)
            Class.Map.RegisterProperty(function.ToInstanceProperty());
        foreach (InnerFunction function in mapFunctions)
            Class.Map.RegisterMethod(function.ToInstanceMethod());
    }

    #endregion

    #region Properties

    /// <summary>
    /// The name of this function.
    /// </summary>
    public string Name { get; private set; } = name;

    /// <summary>
    /// The set of parameters required for a call to this function.
    /// </summary>
    public Parameter[] Parameters { get; private set; } = parameters;

    /// <summary>
    /// The logic of this function.
    /// </summary>
    public InnerFunctionLogic Logic { get; private set; } = logic;

    #endregion

    #region Predefined logics

    #region Global functions

    private static DataItem CharLogic(DataItem[] arguments)
    {
        char ch = Convert.ToChar(arguments[0].AsInt32);
        return new String(ch.ToString());
    }

    private static DataItem OrderLogic(DataItem[] arguments)
    {
        DataItem arg0 = arguments[0];
        CheckArgType(arg0, Class.String, "ord", 1);

        string s = arg0.ToString();
        CheckSingleChar(s, "ord", 1);

        return new Integer(Convert.ToInt32(s[0]));
    }

    private static DataItem RandomLogic(DataItem[] arguments)
    {
        return new Float(NextDouble(random));
    }

    private static DataItem RandomIntegerLogic(DataItem[] arguments)
    {
        int min = arguments[0].AsInt32, max = arguments[1].AsInt32;
        if (min > max) (min, max) = (max, min);
        return new Integer(min + (int)(NextDouble(random) * (max - min)));
    }

    private static DataItem SineLogic(DataItem[] arguments)
    {
        return new Float(Math.Sin(arguments[0].AsDouble));
    }

    private static DataItem CosineLogic(DataItem[] arguments)
    {
        return new Float(Math.Cos(arguments[0].AsDouble));
    }

    private static DataItem TangentLogic(DataItem[] arguments)
    {
        return new Float(Math.Tan(arguments[0].AsDouble));
    }

    private static DataItem ArcSineLogic(DataItem[] arguments)
    {
        return new Float(Math.Asin(arguments[0].AsDouble));
    }

    private static DataItem ArcCosineLogic(DataItem[] arguments)
    {
        return new Float(Math.Acos(arguments[0].AsDouble));
    }

    private static DataItem ArcTangentLogic(DataItem[] arguments)
    {
        return new Float(Math.Atan(arguments[0].AsDouble));
    }

    private static DataItem ArcTangent2Logic(DataItem[] arguments)
    {
        return new Float(Math.Atan2(arguments[0].AsDouble, arguments[1].AsDouble));
    }

    private static DataItem SineHyperbolicLogic(DataItem[] arguments)
    {
        return new Float(Math.Sinh(arguments[0].AsDouble));
    }

    private static DataItem CosineHyperbolicLogic(DataItem[] arguments)
    {
        return new Float(Math.Cosh(arguments[0].AsDouble));
    }

    private static DataItem TangentHyperbolicLogic(DataItem[] arguments)
    {
        return new Float(Math.Tanh(arguments[0].AsDouble));
    }

    private static DataItem DegreesToRadiansLogic(DataItem[] arguments)
    {
        return new Float(arguments[0].AsDouble * Math.PI / 180.0);
    }

    private static DataItem RadiansToDegreesLogic(DataItem[] arguments)
    {
        return new Float(arguments[0].AsDouble * 180.0 / Math.PI);
    }

    private static DataItem LogarithmLogic(DataItem[] arguments)
    {
        return new Float(Math.Log(arguments[0].AsDouble));
    }

    private static DataItem LogarithmBase10Logic(DataItem[] arguments)
    {
        return new Float(Math.Log10(arguments[0].AsDouble));
    }

    private static DataItem LogarithmBaseNLogic(DataItem[] arguments)
    {
        return new Float(Math.Log(arguments[0].AsDouble, arguments[1].AsDouble));
    }

    private static DataItem ExponentialLogic(DataItem[] arguments)
    {
        return new Float(Math.Exp(arguments[0].AsDouble));
    }

    private static DataItem SquareRootLogic(DataItem[] arguments)
    {
        double x = arguments[0].AsDouble;
        return x < 0
             ? new Complex(Complex64.ImaginaryOne * Math.Sqrt(-x))
             : new Float(Math.Sqrt(x));
    }

    private static DataItem SignLogic(DataItem[] arguments)
    {
        DataItem arg = arguments[0];
        return arg.Class.ClassID switch
        {
            ClassID.Integer => new Integer(Math.Sign(arg.AsInt32)),
            ClassID.Long => new Integer(arg.AsBigInteger.Sign),
            ClassID.Rational => new Integer(arg.AsRational32.Sign),
            ClassID.Float => new Integer(Math.Sign(arg.AsDouble)),
            ClassID.Decimal => new Integer(arg.AsBigDecimal.Sign),
            _ => throw new InvalidOperationException(string.Format(Resources.TypeDoesNotSupportFunction, "sign,", arg.Class.Name)),
        };
    }

    private static DataItem AbsoluteValueLogic(DataItem[] arguments)
    {
        DataItem arg = arguments[0];
        return arg.Class.ClassID switch
        {
            ClassID.Integer => new Integer(Math.Abs(arg.AsInt32)),
            ClassID.Long => new Long(BigInteger.Abs(arg.AsBigInteger)),
            ClassID.Rational => new Rational(arg.AsRational32.Abs()),
            ClassID.Float => new Float(Math.Abs(arg.AsDouble)),
            ClassID.Decimal => new Decimal(arg.AsBigDecimal.Abs()),
            ClassID.Complex => new Float(Complex64.Abs(arg.AsComplex64)),
            _ => throw new InvalidOperationException(string.Format(Resources.TypeDoesNotSupportFunction, "abs,", arg.Class.Name)),
        };
    }

    private static DataItem MinimumLogic(DataItem[] arguments)
    {
        List<DataItem> list = arguments[0].AsList;
        return list.Count switch
        {
            0 => Void.Value,
            _ => list.Skip(1).Aggregate(list[0], (acc, val) => val.CompareTo(acc) < 0 ? val : acc),
        };
    }

    private static DataItem MaximumLogic(DataItem[] arguments)
    {
        List<DataItem> list = arguments[0].AsList;
        return list.Count switch
        {
            0 => Void.Value,
            _ => list.Skip(1).Aggregate(list[0], (acc, val) => val.CompareTo(acc) > 0 ? val : acc),
        };
    }

    private static DataItem TruncateLogic(DataItem[] arguments)
    {
        DataItem arg = arguments[0];
        return arg.Class.ClassID switch
        {
            ClassID.Float => new Float(Math.Truncate(arg.AsDouble)),
            ClassID.Decimal => new Decimal(arg.AsBigDecimal.Truncate()),
            _ => throw new InvalidOperationException(string.Format(Resources.TypeDoesNotSupportFunction, "trunc,", arg.Class.Name)),
        };
    }

    private static DataItem FloorLogic(DataItem[] arguments)
    {
        DataItem arg = arguments[0];
        return arg.Class.ClassID switch
        {
            ClassID.Float => new Float(Math.Floor(arg.AsDouble)),
            ClassID.Decimal => new Decimal(arg.AsBigDecimal.Floor()),
            _ => throw new InvalidOperationException(string.Format(Resources.TypeDoesNotSupportFunction, "floor,", arg.Class.Name)),
        };
    }

    private static DataItem CeilingLogic(DataItem[] arguments)
    {
        DataItem arg = arguments[0];
        return arg.Class.ClassID switch
        {
            ClassID.Float => new Float(Math.Ceiling(arg.AsDouble)),
            ClassID.Decimal => new Decimal(arg.AsBigDecimal.Ceiling()),
            _ => throw new InvalidOperationException(string.Format(Resources.TypeDoesNotSupportFunction, "ceil,", arg.Class.Name)),
        };
    }

    private static DataItem RoundLogic(DataItem[] arguments)
    {
        DataItem arg1 = arguments[0], arg2 = arguments[1];
        return arg1.Class.ClassID switch
        {
            ClassID.Float => new Float(Math.Round(arg1.AsDouble, arg2.AsInt32)),
            ClassID.Decimal => new Decimal(arg1.AsBigDecimal.Round(arg2.AsInt32)),
            _ => throw new InvalidOperationException(string.Format(Resources.TypeDoesNotSupportFunction, "round,", arg1.Class.Name)),
        };
    }

    private static DataItem NowLogic(DataItem[] arguments)
    {
        return new Date(DateTime.Now);
    }

    private static DataItem FormatLogic(DataItem[] arguments)
    {
        DataItem arg0 = arguments[0];
        CheckArgType(arg0, Class.String, "format", 1);

        string mask = arg0.ToString();
        var values = arguments[1].AsList;

        return new String(FormatList(mask, values));
    }

    private static DataItem ReadLineLogic(DataItem[] arguments)
    {
        DataItem arg0 = arguments[0];
        CheckArgType(arg0, Class.String, "readln", 1);
        RuntimeServices.Out.Write(arg0.ToString());

        return new String(RuntimeServices.In.ReadLine());
    }

    private static DataItem PrintLogic(DataItem[] arguments)
    {
        DataItem arg0 = arguments[0];
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

    private static DataItem PrintLineLogic(DataItem[] arguments)
    {
        DataItem arg0 = arguments[0];
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

    private static DataItem EvaluateLogic(DataItem[] arguments)
    {
        DataItem arg0 = arguments[0];
        CheckArgType(arg0, Class.String, "eval", 1);

        string command = arg0 + ";"; // A semi-colon is appended to the input for safety
        var interpreter = RuntimeServices.Interpreter;

        try
        {
            while (command.Length > 0)
            {
                var parser = new Parser(new Lexer(new StringReader(command)));
                var statement = parser.RequiredStatement();
                statement.AcceptTranslator(interpreter);
                command = command[statement.End.Offset..];
            }
        }
        catch (ParseException)
        {
            throw new ArgumentException(Resources.InvalidEvalParam);
        }

        return interpreter.ReturnedValue;
    }

    private static DataItem PackLogic(DataItem[] arguments)
    {
        DataItem arg0 = arguments[0];
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
                    case PackFormatType.Byte or PackFormatType.Character:
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
                                tmpString = tmpString[..item.Count];
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
                                tmpString = tmpString[..count];
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

        return new Blob(ms.ToArray());
    }

    private static DataItem UnPackLogic(DataItem[] arguments)
    {
        CheckArgType(arguments[0], Class.String, "unpack", 1);
        CheckArgType(arguments[1], Class.Blob, "unpack", 2);

        var format = PackFormat.Parse(arguments[0].ToString());
        var bytes = arguments[1].AsByteArray;
        var list = new List<DataItem>();

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
                    case PackFormatType.Byte or PackFormatType.Character:
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

    private static DataItem EqualsLogic(DataItem[] arguments)
    {
        return Boolean.FromBool(arguments[0].Equals(arguments[1]));
    }

    private static DataItem HashCodeLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].GetHashCode());
    }

    private static DataItem CompareToLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].CompareTo(arguments[1]));
    }

    private static DataItem ToStringLogic(DataItem[] arguments)
    {
        string s = arguments[0].ToString(arguments[1].ToString(), CultureInfo.CurrentUICulture);
        return new String(s);
    }

    private static DataItem CloneLogic(DataItem[] arguments)
    {
        return (DataItem)arguments[0].Clone();
    }

    private static DataItem DisposeLogic(DataItem[] arguments)
    {
        arguments[0].Dispose();
        return Void.Value;
    }

    #endregion

    #region Rational specific methods

    private static DataItem RationalNumLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsRational32.Numerator);
    }

    private static DataItem RationalDenLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsRational32.Denominator);
    }

    private static DataItem RationalInverseLogic(DataItem[] arguments)
    {
        return new Rational(arguments[0].AsRational32.Inverse());
    }

    #endregion

    #region Complex specific methods

    private static DataItem ComplexOfLogic(DataItem[] arguments)
    {
        return new Complex(arguments[0].AsDouble, arguments[1].AsDouble);
    }

    private static DataItem ComplexRealLogic(DataItem[] arguments)
    {
        return new Float(arguments[0].AsComplex64.Real);
    }

    private static DataItem ComplexImaginaryLogic(DataItem[] arguments)
    {
        return new Float(arguments[0].AsComplex64.Imaginary);
    }

    private static DataItem ComplexConjugateLogic(DataItem[] arguments)
    {
        return new Complex(Complex64.Conjugate(arguments[0].AsComplex64));
    }

    #endregion

    #region Date specific methods

    private static DataItem DateOfLogic(DataItem[] arguments)
    {
        DataItem[] values = [.. arguments[0].AsList];
        var date = values.Length switch
        {
            // year, month and day
            3 => new DateTime(values[0].AsInt32, values[1].AsInt32, values[2].AsInt32),
            // hour, minute, second and millisecond
            4 => new DateTime(1, 1, 1, values[0].AsInt32, values[1].AsInt32, values[2].AsInt32, values[3].AsInt32),
            // year, month, day, hour, minute and second
            6 => new DateTime(values[0].AsInt32, values[1].AsInt32, values[2].AsInt32,
                              values[3].AsInt32, values[4].AsInt32, values[5].AsInt32),
            // year, month, day, hour, minute, second and millisecond
            7 => new DateTime(values[0].AsInt32, values[1].AsInt32, values[2].AsInt32,
                              values[3].AsInt32, values[4].AsInt32, values[5].AsInt32, values[6].AsInt32),
            _ => throw new InvalidOperationException(string.Format(Resources.BadDateOfCall, values.Length)),
        };

        return new Date(date);
    }

    private static DataItem DateGetDateLogic(DataItem[] arguments)
    {
        return new Date(arguments[0].AsDateTime.Date);
    }

    private static DataItem DateGetTimeLogic(DataItem[] arguments)
    {
        long ticks = arguments[0].AsDateTime.TimeOfDay.Ticks;
        return new Date(new DateTime(ticks));
    }

    private static DataItem DateGetLogic(DataItem[] arguments)
    {
        DataItem self = arguments[0], arg1 = arguments[1];
        CheckArgType(arg1, Class.String, "date::get", 1);

        return arg1.ToString() switch
        {
            "year" => new Integer(self.AsDateTime.Year),
            "month" => new Integer(self.AsDateTime.Month),
            "day" => new Integer(self.AsDateTime.Day),
            "weekday" => new String(self.AsDateTime.DayOfWeek.ToString()),
            "yearday" => new Integer(self.AsDateTime.DayOfYear),
            "hour" => new Integer(self.AsDateTime.Hour),
            "minute" => new Integer(self.AsDateTime.Minute),
            "second" => new Integer(self.AsDateTime.Second),
            "millisecond" => new Integer(self.AsDateTime.Millisecond),
            _ => throw new ArgumentException(string.Format(Resources.InvalidDatePart, arg1)),
        };
    }

    private static DataItem DateGetTicksLogic(DataItem[] arguments)
    {
        return new Long(arguments[0].AsDateTime.Ticks);
    }

    private static DataItem DateAddLogic(DataItem[] arguments)
    {
        DataItem self = arguments[0], arg1 = arguments[1], arg2 = arguments[2];
        CheckArgType(arg2, Class.String, "date::add", 2);

        return arg2.ToString() switch
        {
            "year" => new Date(self.AsDateTime.AddYears(arg1.AsInt32)),
            "month" => new Date(self.AsDateTime.AddMonths(arg1.AsInt32)),
            "day" => new Date(self.AsDateTime.AddDays(arg1.AsDouble)),
            "hour" => new Date(self.AsDateTime.AddHours(arg1.AsDouble)),
            "minute" => new Date(self.AsDateTime.AddMinutes(arg1.AsDouble)),
            "second" => new Date(self.AsDateTime.AddSeconds(arg1.AsDouble)),
            "millisecond" => new Date(self.AsDateTime.AddMilliseconds(arg1.AsDouble)),
            _ => throw new ArgumentException(string.Format(Resources.InvalidDatePart, arg2)),
        };
    }

    private static DataItem DateAddTicksLogic(DataItem[] arguments)
    {
        var ticks = (long) arguments[1].AsBigInteger;
        return new Date(arguments[0].AsDateTime.AddTicks(ticks));
    }

    private static DataItem DateSubtractLogic(DataItem[] arguments)
    {
        DataItem self = arguments[0], arg1 = arguments[1], arg2 = arguments[2];
        CheckArgType(arg2, Class.String, "date::subtract", 2);

        return arg2.ToString() switch
        {
            "year" => new Integer(YearDiff(self.AsDateTime, arg1.AsDateTime)),
            "month" => new Integer(MonthDiff(self.AsDateTime, arg1.AsDateTime)),
            "day" => new Integer((self.AsDateTime - arg1.AsDateTime).Days),
            "hour" => new Long((BigInteger)(self.AsDateTime - arg1.AsDateTime).TotalHours),
            "minute" => new Long((BigInteger)(self.AsDateTime - arg1.AsDateTime).TotalMinutes),
            "second" => new Long((BigInteger)(self.AsDateTime - arg1.AsDateTime).TotalSeconds),
            "millisecond" => new Long((BigInteger)(self.AsDateTime - arg1.AsDateTime).TotalMilliseconds),
            _ => throw new ArgumentException(string.Format(Resources.InvalidDatePart, arg2)),
        };
    }

    #endregion

    #region String specific methods

    private static DataItem StringLengthLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].ToString().Length);
    }

    private static DataItem StringIndexOfLogic(DataItem[] arguments)
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

    private static DataItem StringLastIndexOfLogic(DataItem[] arguments)
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

    private static DataItem StringToLowerLogic(DataItem[] arguments)
    {
        return new String(arguments[0].ToString().ToLower());
    }

    private static DataItem StringToUpperLogic(DataItem[] arguments)
    {
        return new String(arguments[0].ToString().ToUpper());
    }

    private static DataItem StringCapitalizeLogic(DataItem[] arguments)
    {
        string self = arguments[0].ToString();
        return new String(StringUtil.Capitalize(self));
    }

    private static DataItem StringUncapitalizeLogic(DataItem[] arguments)
    {
        string self = arguments[0].ToString();
        return new String(StringUtil.Uncapitalize(self));
    }

    private static DataItem StringSubstringLogic(DataItem[] arguments)
    {
        string self = arguments[0].ToString();
        int length = arguments[2].AsInt32;

        int index = arguments[1].AsInt32;
        while (index < 0) index += self.Length;

        string substr = length > 0 ? self.Substring(index, length) : self.Substring(index);
        return new String(substr);
    }

    private static DataItem StringInsertLogic(DataItem[] arguments)
    {
        string self = arguments[0].ToString();

        int index = arguments[1].AsInt32;
        while (index < 0) index += self.Length;

        var arg2 = arguments[2];
        CheckArgType(arg2, Class.String, "string::insert", 2);

        return new String(self.Insert(index, arg2.ToString()));
    }

    private static DataItem StringRemoveLogic(DataItem[] arguments)
    {
        string self = arguments[0].ToString();
        int count = arguments[2].AsInt32;

        int index = arguments[1].AsInt32;
        while (index < 0) index += self.Length;

        string s = count <= 0 ? self.Remove(index) : self.Remove(index, count);
        return new String(s);
    }

    private static DataItem StringReplaceLogic(DataItem[] arguments)
    {
        string self = arguments[0].ToString();

        var arg1 = arguments[1];
        CheckArgType(arg1, Class.String, "string::replace", 1);

        var arg2 = arguments[2];
        CheckArgType(arg2, Class.String, "string::insert", 2);

        string s = StringUtil.GetRegex(arg1.ToString()).Replace(self, arg2.ToString());
        return new String(s);
    }

    private static DataItem StringTrimLeftLogic(DataItem[] arguments)
    {
        DataItem arg1 = arguments[1];
        CheckArgType(arg1, Class.String, "string::ltrim", 1);
        char[] trimChars = arg1.ToString().ToCharArray();

        return new String(arguments[0].ToString().TrimStart(trimChars));
    }

    private static DataItem StringTrimRightLogic(DataItem[] arguments)
    {
        DataItem arg1 = arguments[1];
        CheckArgType(arg1, Class.String, "string::rtrim", 1);
        char[] trimChars = arg1.ToString().ToCharArray();

        return new String(arguments[0].ToString().TrimEnd(trimChars));
    }

    private static DataItem StringTrimLogic(DataItem[] arguments)
    {
        DataItem arg1 = arguments[1];
        CheckArgType(arg1, Class.String, "string::trim", 1);
        char[] trimChars = arg1.ToString().ToCharArray();

        return new String(arguments[0].ToString().Trim(trimChars));
    }

    private static DataItem StringPadLeftLogic(DataItem[] arguments)
    {
        string self = arguments[0].ToString();
        int width = arguments[1].AsInt32;

        DataItem arg2 = arguments[2];
        CheckArgType(arg2, Class.String, "string::lpad", 2);
        
        string padding = arg2.ToString();
        CheckSingleChar(padding, "string::lpad", 2);

        return new String(self.PadLeft(width, padding[0]));
    }

    private static DataItem StringPadRightLogic(DataItem[] arguments)
    {
        string self = arguments[0].ToString();
        int width = arguments[1].AsInt32;

        DataItem arg2 = arguments[2];
        CheckArgType(arg2, Class.String, "string::rpad", 2);

        string padding = arg2.ToString();
        CheckSingleChar(padding, "string::rpad", 2);

        return new String(self.PadRight(width, padding[0]));
    }

    private static DataItem StringSplitLogic(DataItem[] arguments)
    {
        string self = arguments[0].ToString();

        var arg1 = arguments[1];
        CheckArgType(arg1, Class.String, "string::split", 1);

        string[] parts = StringUtil.GetRegex(arg1.ToString()).Split(self);
        var items = new List<DataItem>();
        for (int i = 0; i < parts.Length; ++i)
            items.Add(new String(parts[i]));

        return new List(items);
    }

    #endregion

    #region Blob specific methods

    private static DataItem BlobOfLogic(DataItem[] arguments)
    {
        return new Blob(new byte[arguments[0].AsInt32]);
    }

    private static DataItem BlobFromHexStringLogic(DataItem[] arguments)
    {
        return new Blob(Convert.FromHexString(arguments[0].ToString()));
    }

    private static DataItem BlobToHexStringLogic(DataItem[] arguments)
    {
        return new String(Convert.ToHexString(arguments[0].AsByteArray));
    }

    private static DataItem BlobToBase64StringLogic(DataItem[] arguments)
    {
        return new String(Convert.ToBase64String(arguments[0].AsByteArray));
    }

    private static DataItem BlobFromBase64StringLogic(DataItem[] arguments)
    {
        return new Blob(Convert.FromBase64String(arguments[0].ToString()));
    }

    private static DataItem BlobLengthLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsByteArray.Length);
    }

    private static DataItem BlobIndexOfLogic(DataItem[] arguments)
    {
        var self = arguments[0].AsByteArray;

        var start = arguments[2].AsInt32;
        while (start < 0) start += self.Length;

        var length = arguments[3].AsInt32;
        if (length <= 0) length = self.Length - start;

        return new Integer(Array.IndexOf(self, (byte)arguments[1].AsInt32, start, length));
    }

    private static DataItem BlobLastIndexOfLogic(DataItem[] arguments)
    {
        var self = arguments[0].AsByteArray;

        int start = arguments[2].AsInt32;
        while (start < 0) start += self.Length;

        var length = arguments[3].AsInt32;
        if (length <= 0) length = start + 1;

        return new Integer(Array.LastIndexOf(self, (byte)arguments[1].AsInt32, start, length));
    }

    private static DataItem BlobFillLogic(DataItem[] arguments)
    {
        var self = arguments[0].AsByteArray;
        var fillByte = (byte)arguments[1].AsInt32;

        int start = arguments[2].AsInt32;
        while (start < 0) start += self.Length;

        var length = arguments[3].AsInt32;
        if (length <= 0) length = self.Length - start;

        Array.Fill(self, fillByte, start, length);

        return Void.Value;
    }

    private static DataItem BlobCopyToLogic(DataItem[] arguments)
    {
        var self = arguments[0].AsByteArray;

        CheckArgType(arguments[1], Class.Blob, "copyTo", 1);
        var other = arguments[1].AsByteArray;

        int sourceIndex = arguments[2].AsInt32;
        while (sourceIndex < 0) sourceIndex += self.Length;

        int destIndex = arguments[3].AsInt32;
        while (destIndex < 0) destIndex += other.Length;

        var length = arguments[4].AsInt32;
        if (length <= 0) length = self.Length - sourceIndex;

        Array.Copy(self, sourceIndex, other, destIndex, length);

        return Void.Value;
    }

    private static DataItem BlobResizeLogic(DataItem[] arguments)
    {
        ((Blob)arguments[0]).Resize(arguments[1].AsInt32);
        return Void.Value;
    }

    #endregion

    #region Tuple specific methods

    private static DataItem TupleSizeLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsArray.Length);
    }

    private static DataItem TupleIndexOfLogic(DataItem[] arguments)
    {
        var self = arguments[0].AsArray;

        var start = arguments[2].AsInt32;
        while (start < 0) start += self.Length;

        var count = arguments[3].AsInt32;
        if (count <= 0) count = self.Length - start;

        return new Integer(Array.IndexOf(self, arguments[1], start, count));
    }

    private static DataItem TupleLastIndexOfLogic(DataItem[] arguments)
    {
        var self = arguments[0].AsArray;

        var start = arguments[2].AsInt32;
        while (start < 0) start += self.Length;

        var count = arguments[3].AsInt32;
        if (count <= 0) count = start + 1;

        return new Integer(Array.LastIndexOf(self, arguments[1], start, count));
    }

    #endregion

    #region List specific methods

    private static DataItem ListSizeLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsList.Count);
    }

    private static DataItem ListJoinLogic(DataItem[] arguments)
    {
        DataItem self = arguments[0], arg1 = arguments[1];
        CheckArgType(arg1, Class.String, "list::join", 1);

        var values = self.AsList.ConvertAll(x => RuntimeServices.ToString(x));
        string s = string.Join(arg1.ToString(), values.ToArray());

        return new String(s);
    }

    private static DataItem ListAddLogic(DataItem[] arguments)
    {
        arguments[0].AsList.Add(arguments[1]);
        return Void.Value;
    }

    private static DataItem ListInsertLogic(DataItem[] arguments)
    {
        List<DataItem> list = arguments[0].AsList;
        
        int index = arguments[1].AsInt32;
        while (index < 0) index += list.Count;

        list.Insert(index, arguments[2]);
        return Void.Value;
    }

    private static DataItem ListInsertAllLogic(DataItem[] arguments)
    {
        List<DataItem> list = arguments[0].AsList;

        int index = arguments[1].AsInt32;
        while (index < 0) index += list.Count;

        list.InsertRange(index, arguments[2].AsList);
        return Void.Value;
    }

    private static DataItem ListIndexOfLogic(DataItem[] arguments)
    {
        var self = arguments[0].AsList;

        var start = arguments[2].AsInt32;
        while (start < 0) start += self.Count;

        var count = arguments[3].AsInt32;
        if (count <= 0) count = self.Count - start;

        return new Integer(self.IndexOf(arguments[1], start, count));
    }

    private static DataItem ListLastIndexOfLogic(DataItem[] arguments)
    {
        var self = arguments[0].AsList;

        var start = arguments[2].AsInt32;
        while (start < 0) start += self.Count;

        var count = arguments[3].AsInt32;
        if (count <= 0) count = start + 1;

        return new Integer(self.LastIndexOf(arguments[1], start, count));
    }

    private static DataItem ListBinarySearchLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsList.BinarySearch(arguments[1]));
    }

    private static DataItem ListFrequencyOfLogic(DataItem[] arguments)
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

    private static DataItem ListRemoveLogic(DataItem[] arguments)
    {
        bool b = arguments[0].AsList.Remove(arguments[1]);
        return Boolean.FromBool(b);
    }

    private static DataItem ListRemoveAtLogic(DataItem[] arguments)
    {
        List<DataItem> list = arguments[0].AsList;
        int count = arguments[2].AsInt32;

        int index = arguments[1].AsInt32;
        while (index < 0) index += list.Count;

        if (count <= 1)
            list.RemoveAt(index);
        else
            list.RemoveRange(index, count);

        return Void.Value;
    }

    private static DataItem ListClearLogic(DataItem[] arguments)
    {
        arguments[0].AsList.Clear();
        return Void.Value;
    }

    private static DataItem ListSortLogic(DataItem[] arguments)
    {
        DataItem self = arguments[0], arg1 = arguments[1];
        Comparison<DataItem> cmp = RuntimeServices.CompareTo;

        if (arg1 != Void.Value)
        {
            CheckArgType(arg1, Class.Closure, "list::sort", 1);
            Type cmpType = typeof(Comparison<DataItem>);
            Delegate cmpDelegate = arg1.AsFunction.ToDelegate(cmpType);
            cmp = (Comparison<DataItem>) cmpDelegate;
        }

        var sorted = (DataItem)self.Clone();
        sorted.AsList.Sort(cmp);
        return sorted;
    }

    private static DataItem ListShuffleLogic(DataItem[] arguments)
    {
        return new List(arguments[0].AsList.OrderBy(x => NextUInt32(random)).ToList());
    }

    private static DataItem ListInverseLogic(DataItem[] arguments)
    {
        var inverse = (DataItem)arguments[0].Clone();
        inverse.AsList.Reverse();
        return inverse;
    }

    private static DataItem ListSublistLogic(DataItem[] arguments)
    {
        List<DataItem> self = arguments[0].AsList;

        int index = arguments[1].AsInt32;
        while (index < 0) index += self.Count;

        return new List(self.GetRange(index, arguments[2].AsInt32));
    }

    private static DataItem ListUniqueLogic(DataItem[] arguments)
    {
        List<DataItem> self = arguments[0].AsList;
        var unique = new List<DataItem>();

        foreach (DataItem item in self)
            if (!unique.Contains(item))
                unique.Add(item);

        return new List(unique);
    }

    private static DataItem ListMapToLogic(DataItem[] arguments)
    {
        List<DataItem> self = arguments[0].AsList;
        List<DataItem> other = arguments[1].AsList;
        if (self.Count != other.Count)
            throw new ArgumentException("list::mapTo requires that both lists be of the same length");

        var dict = new Dictionary<DataItem, DataItem>();
        for (int i = 0; i < self.Count; ++i)
            dict.Add(self[i], other[i]);
        
        return new Map(dict);
    }

    #endregion

    #region Set specific methods

    private static DataItem SetSizeLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsHashSet.Count);
    }

    private static DataItem SetAddLogic(DataItem[] arguments)
    {
        arguments[0].AsHashSet.Add(arguments[1]);
        return Void.Value;
    }

    private static DataItem SetRemoveLogic(DataItem[] arguments)
    {
        bool b = arguments[0].AsHashSet.Remove(arguments[1]);
        return Boolean.FromBool(b);
    }

    private static DataItem SetClearLogic(DataItem[] arguments)
    {
        arguments[0].AsHashSet.Clear();
        return Void.Value;
    }

    #endregion

    #region Queue specific methods

    private static DataItem QueueSizeLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsQueue.Count);
    }

    private static DataItem QueueOfLogic(DataItem[] arguments)
    {
        return new Queue(new Queue<DataItem>(arguments[0].AsList));
    }

    private static DataItem QueueEnqueueLogic(DataItem[] arguments)
    {
        arguments[0].AsQueue.Enqueue(arguments[1]);
        return Void.Value;
    }

    private static DataItem QueuePeekLogic(DataItem[] arguments)
    {
        return arguments[0].AsQueue.Peek();
    }

    private static DataItem QueueDequeueLogic(DataItem[] arguments)
    {
        return arguments[0].AsQueue.Dequeue();
    }

    private static DataItem QueueClearLogic(DataItem[] arguments)
    {
        arguments[0].AsQueue.Clear();
        return Void.Value;
    }

    #endregion

    #region Stack specific methods

    private static DataItem StackSizeLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsStack.Count);
    }

    private static DataItem StackOfLogic(DataItem[] arguments)
    {
        return new Stack(new Stack<DataItem>(arguments[0].AsList));
    }

    private static DataItem StackPushLogic(DataItem[] arguments)
    {
        arguments[0].AsStack.Push(arguments[1]);
        return Void.Value;
    }

    private static DataItem StackPeekLogic(DataItem[] arguments)
    {
        return arguments[0].AsStack.Peek();
    }

    private static DataItem StackPopLogic(DataItem[] arguments)
    {
        return arguments[0].AsStack.Pop();
    }

    private static DataItem StackClearLogic(DataItem[] arguments)
    {
        arguments[0].AsStack.Clear();
        return Void.Value;
    }

    #endregion

    #region Map specific methods

    private static DataItem MapSizeLogic(DataItem[] arguments)
    {
        return new Integer(arguments[0].AsDictionary.Count);
    }

    private static DataItem MapContainsKeyLogic(DataItem[] arguments)
    {
        bool b = arguments[0].AsDictionary.ContainsKey(arguments[1]);
        return Boolean.FromBool(b);
    }

    private static DataItem MapContainsValueLogic(DataItem[] arguments)
    {
        bool b = arguments[0].AsDictionary.ContainsValue(arguments[1]);
        return Boolean.FromBool(b);
    }

    private static DataItem MapKeysLogic(DataItem[] arguments)
    {
        var keySet = new HashSet<DataItem>();
        var keys = arguments[0].AsDictionary.Keys;

        foreach (DataItem key in keys)
            keySet.Add(key);

        return new Set(keySet);
    }

    private static DataItem MapValuesLogic(DataItem[] arguments)
    {
        var valueSet = new HashSet<DataItem>();
        var values = arguments[0].AsDictionary.Values;

        foreach (DataItem value in values)
            if (!valueSet.Contains(value))
                valueSet.Add(value);

        return new Set(valueSet);
    }

    private static DataItem MapFrequencyOfLogic(DataItem[] arguments)
    {
        int frequency = 0;
        var dict = arguments[0].AsDictionary;

        foreach (KeyValuePair<DataItem, DataItem> pair in dict)
            if (pair.Value.Equals(arguments[1]))
                ++frequency;

        return new Integer(frequency);
    }

    private static DataItem MapKeysOfLogic(DataItem[] arguments)
    {
        var keySet = new HashSet<DataItem>();
        var map = arguments[0].AsDictionary;

        foreach (KeyValuePair<DataItem, DataItem> pair in map)
            if (pair.Value.Equals(arguments[1]))
                keySet.Add(pair.Key);

        return new Set(keySet);
    }

    private static DataItem MapInverseLogic(DataItem[] arguments)
    {
        var inverse = new Dictionary<DataItem, DataItem>();
        var map = arguments[0].AsDictionary;

        foreach (KeyValuePair<DataItem, DataItem> pair in map)
            if (!inverse.ContainsKey(pair.Value))
                inverse.Add(pair.Value, pair.Key);

        return new Map(inverse);
    }

    private static DataItem MapRemoveLogic(DataItem[] arguments)
    {
        bool b = arguments[0].AsDictionary.Remove(arguments[1]);
        return Boolean.FromBool(b);
    }

    private static DataItem MapRemoveAllLogic(DataItem[] arguments)
    {
        var self = arguments[0].AsDictionary;
        int removed = 0;

        foreach (DataItem key in arguments[1].AsHashSet)
            if (self.Remove(key))
                ++removed;

        return new Integer(removed);
    }

    private static DataItem MapClearLogic(DataItem[] arguments)
    {
        arguments[0].AsDictionary.Clear();
        return Void.Value;
    }

    #endregion

    #endregion

    #region Predefined instances

    #region Global functions

    /// <summary>
    /// Gets a character from its Unicode rank.
    /// </summary>
    public static readonly InnerFunction Char = new ("chr", [new("ascii")], CharLogic);

    /// <summary>
    /// Gets the Unicode rank of a character.
    /// </summary>
    public static readonly InnerFunction Order = new ("ord", [new("char")], OrderLogic);

    /// <summary>
    /// Generates an random floatting-point number between 0 and 1.
    /// </summary>
    public static readonly InnerFunction Random = new ("rand", [], RandomLogic);

    /// <summary>
    /// Generates an random integer comprised between the given boundaries.
    /// </summary>
    public static readonly InnerFunction RandomInteger = new ("randint", [new("min"), new("max", new Integer(0))], RandomIntegerLogic);

    /// <summary>
    /// Computes the sine of a number.
    /// </summary>
    public static readonly InnerFunction Sine = new ("sin", [new("x")], SineLogic);

    /// <summary>
    /// Computes the cosine of a number.
    /// </summary>
    public static readonly InnerFunction Cosine = new ("cos", [new("x")], CosineLogic);

    /// <summary>
    /// Computes the tangent of a number.
    /// </summary>
    public static readonly InnerFunction Tangent = new ("tan", [new("x")], TangentLogic);

    /// <summary>
    /// Computes the arc sine of a number.
    /// </summary>
    public static readonly InnerFunction ArcSine = new ("asin", [new("x")], ArcSineLogic);

    /// <summary>
    /// Computes the arc cosine of a number.
    /// </summary>
    public static readonly InnerFunction ArcCosine = new ("acos", [new("x")], ArcCosineLogic);

    /// <summary>
    /// Computes the arc tangent of a number.
    /// </summary>
    public static readonly InnerFunction ArcTangent = new ("atan", [new("x")], ArcTangentLogic);

    /// <summary>
    /// Computes the arc tangent of the ratio of two numbers.
    /// </summary>
    public static readonly InnerFunction ArcTangent2 = new ("atan2", [new("y"), new("x")], ArcTangent2Logic);

    /// <summary>
    /// Computes the hyperbolic sine of a number.
    /// </summary>
    public static readonly InnerFunction SineHyperbolic = new ("sinh", [new("x")], SineHyperbolicLogic);

    /// <summary>
    /// Computes the hyperbolic cosine of a number.
    /// </summary>
    public static readonly InnerFunction CosineHyperbolic = new ("cosh", [new("x")], CosineHyperbolicLogic);

    /// <summary>
    /// Computes the hyperbolic tangent of a number.
    /// </summary>
    public static readonly InnerFunction TangentHyperbolic = new ("tanh", [new("x")], TangentHyperbolicLogic);

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    public static readonly InnerFunction DegreesToRadians = new ("deg2rad", [new("x")], DegreesToRadiansLogic);

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    public static readonly InnerFunction RadiansToDegrees = new ("rad2deg", [new("x")], RadiansToDegreesLogic);

    /// <summary>
    /// Computes the natural logarithm of a number.
    /// </summary>
    public static readonly InnerFunction Logarithm = new ("log", [new("x")], LogarithmLogic);

    /// <summary>
    /// Computes the logarithm on base 10 of a number.
    /// </summary>
    public static readonly InnerFunction LogarithmBase10 = new ("log10", [new("x")], LogarithmBase10Logic);

    /// <summary>
    /// Computes the logarithm of a number on the given base.
    /// </summary>
    public static readonly InnerFunction LogarithmBaseN = new ("log2", [new("x"), new("base", new Float(2.0))], LogarithmBaseNLogic);

    /// <summary>
    /// Computes the exponential of a number.
    /// </summary>
    public static readonly InnerFunction Exponential = new ("exp", [new("x")], ExponentialLogic);

    /// <summary>
    /// Computes the squared root of a number.
    /// </summary>
    public static readonly InnerFunction SquareRoot = new ("sqrt", [new("x")], SquareRootLogic);

    /// <summary>
    /// Determines the sign (-1, 0 or 1) of a number.
    /// </summary>
    public static readonly InnerFunction Sign = new ("sign", [new("value")], SignLogic);

    /// <summary>
    /// Determines the absolute value of a number.
    /// </summary>
    public static readonly InnerFunction AbsoluteValue = new ("abs", [new("value")], AbsoluteValueLogic);

    /// <summary>
    /// Truncates a number.
    /// </summary>
    public static readonly InnerFunction Truncate = new ("trunc", [new("x")], TruncateLogic);

    /// <summary>
    /// Determines the floor of a number.
    /// </summary>
    public static readonly InnerFunction Floor = new ("floor", [new("x")], FloorLogic);

    /// <summary>
    /// Determines the ceiling of a number
    /// </summary>
    public static readonly InnerFunction Ceiling = new ("ceil", [new("x")], CeilingLogic);

    /// <summary>
    /// Rounds off a number.
    /// </summary>
    public static readonly InnerFunction Round = new ("round", [new("value"), new("precision", new Integer(0))], RoundLogic);

    /// <summary>
    /// Determines the minimum of two number.
    /// </summary>
    public static readonly InnerFunction Minimum = new ("min", [new("values", false, true, null)], MinimumLogic);

    /// <summary>
    /// Determines the maximum of two number.
    /// </summary>
    public static readonly InnerFunction Maximum = new ("max", [new("values", false, true, null)], MaximumLogic);

    /// <summary>
    /// Gets the current date.
    /// </summary>
    public static readonly InnerFunction Now = new ("now", [], NowLogic);

    /// <summary>
    /// Format a string with the given mask.
    /// </summary>
    public static readonly InnerFunction Format = new ("format", [new("mask"), new("values", false, true, null)], FormatLogic);

    /// <summary>
    /// Reads a line of text from the standard input device.
    /// </summary>
    public static readonly InnerFunction ReadLine = new ("readln", [new("prompt", new String(""))], ReadLineLogic);

    /// <summary>
    /// Prints some text to the standard output device.
    /// </summary>
    public static readonly InnerFunction Print = new ("print", [new("mask"), new("values", false, true, null)], PrintLogic);

    /// <summary>
    /// Prints a line of text to the standard output device.
    /// </summary>
    public static readonly InnerFunction PrintLine = new ("println", [new("mask", new String("")), new("values", false, true, null)], PrintLineLogic);

    /// <summary>
    /// Evaluates the expression contained in a string.
    /// </summary>
    public static readonly InnerFunction Evaluate = new ("eval", [new("expression")], EvaluateLogic);

    /// <summary>
    /// Packs several values in a binary string: a way to create structured data objects.
    /// </summary>
    public static readonly InnerFunction Pack = new ("pack", [new("fmt"), new("values", false, true, null)], PackLogic);

    /// <summary>
    /// Unpacks the values combined in a binary string following the given format.
    /// </summary>
    public static readonly InnerFunction UnPack = new ("unpack", [new("fmt"), new("str")], UnPackLogic);

    #endregion

    #region Common methods

    /// <summary>
    /// Checks that both arguments are equal.
    /// </summary>
    public static readonly InnerFunction EqualsFunction = new ("equals", [new("self"), new("other")], EqualsLogic);

    /// <summary>
    /// Gets the hashcode of its argument.
    /// </summary>
    public static readonly InnerFunction HashCodeFunction = new ("hashCode", [new("self")], HashCodeLogic);

    /// <summary>
    /// Compares its arguments.
    /// </summary>
    public static readonly InnerFunction CompareToFunction = new ("compareTo", [new("self"), new("other")], CompareToLogic);

    /// <summary>
    /// Gets the textual representation of its argument.
    /// </summary>
    public static readonly InnerFunction ToStringFunction = new ("toString", [new("self"), new("format", new String(""))], ToStringLogic);

    /// <summary>
    /// Creates a deep copy of its argument.
    /// </summary>
    public static readonly InnerFunction CloneFunction = new ("clone", [new("self")], CloneLogic);

    /// <summary>
    /// Releases the unmanaged resources allocated to its argument.
    /// </summary>
    public static readonly InnerFunction DisposeFunction = new ("dispose", [new("self")], DisposeLogic);

    #endregion

    #region Rational specific methods

    /// <summary>
    /// Extracts the numerator of a rational number.
    /// </summary>
    public static readonly InnerFunction RationalNum = new("num", [new("self")], RationalNumLogic);

    /// <summary>
    /// Extracts the denominator of a rational number.
    /// </summary>
    public static readonly InnerFunction RationalDen = new ("den", [new("self")], RationalDenLogic);

    /// <summary>
    /// Computes the inverse of a rational number.
    /// </summary>
    public static readonly InnerFunction RationalInverse = new ("inverse", [new("self")], RationalInverseLogic);

    #endregion

    #region Complex specific methods

    /// <summary>
    /// Creates a complex number.
    /// </summary>
    public static readonly InnerFunction ComplexOf = new("of", [new("real"), new("imag")], ComplexOfLogic);

    /// <summary>
    /// Extracts the real part of a complex number.
    /// </summary>
    public static readonly InnerFunction ComplexReal = new ("real", [new("self")], ComplexRealLogic);

    /// <summary>
    /// Extracts the imaginary part of a complex number.
    /// </summary>
    public static readonly InnerFunction ComplexImaginary = new ("imag", [new("self")], ComplexImaginaryLogic);

    /// <summary>
    /// Computes the conjugate of a complex number.
    /// </summary>
    public static readonly InnerFunction ComplexConjugate = new ("conjugate", [new("self")], ComplexConjugateLogic);

    #endregion

    #region Date specific methods

    /// <summary>
    /// Creates a date from the given values.
    /// </summary>
    public static readonly InnerFunction DateOf = new ("of", [new("values", false, true, null)], DateOfLogic);

    /// <summary>
    /// Extracts the date part of a date/time value.
    /// </summary>
    public static readonly InnerFunction DateGetDate = new ("date", [new("self")], DateGetDateLogic);

    /// <summary>
    /// Extracts the time part of a date/time value.
    /// </summary>
    public static readonly InnerFunction DateGetTime = new ("time", [new("self")], DateGetTimeLogic);

    /// <summary>
    /// Extracts a date's part.
    /// </summary>
    public static readonly InnerFunction DateGet = new ("get", [new("self"), new("part")], DateGetLogic);

    /// <summary>
    /// Gets the number of ticks of a date/time value.
    /// </summary>
    public static readonly InnerFunction DateGetTicks = new ("ticks", [new("self")], DateGetTicksLogic);

    /// <summary>
    /// Adds some time units to a date.
    /// </summary>
    public static readonly InnerFunction DateAdd = new ("add", [new("self"), new("value"), new("unit")], DateAddLogic);

    /// <summary>
    /// Adds some ticks to a date.
    /// </summary>
    public static readonly InnerFunction DateAddTicks = new ("addTicks", [new("self"), new("ticks")], DateAddTicksLogic);

    /// <summary>
    /// Computes the difference between two dates.
    /// </summary>
    public static readonly InnerFunction DateSubtract = new ("subtract", [new("self"), new("other"), new("unit")], DateSubtractLogic);

    #endregion

    #region String specific methods

    /// <summary>
    /// Determines the length of a string.
    /// </summary>
    public static readonly InnerFunction StringLength = new ("length", [new("self")], StringLengthLogic);

    /// <summary>
    /// Searches for a string in another string.
    /// </summary>
    public static readonly InnerFunction StringIndexOf = new ("indexOf", [new("self"), new("value"), new("start", new Integer(0)), new("length", new Integer(0))], StringIndexOfLogic);

    /// <summary>
    /// Reversely searches for a string in another string.
    /// </summary>
    public static readonly InnerFunction StringLastIndexOf = new ("lastIndexOf", [new("self"), new("value"), new("start", new Integer(-1)), new("length", new Integer(0))], StringLastIndexOfLogic);

    /// <summary>
    /// Converts all characters in a string to lowercase equivalent.
    /// </summary>
    public static readonly InnerFunction StringToLower = new ("toLower", [new("self")], StringToLowerLogic);

    /// <summary>
    /// Converts all characters in a string to uppercase equivalent.
    /// </summary>
    public static readonly InnerFunction StringToUpper = new ("toUpper", [new("self")], StringToUpperLogic);

    /// <summary>
    /// Capitalzes a string.
    /// </summary>
    public static readonly InnerFunction StringCapitalize = new ("capitalize", [new("self")], StringCapitalizeLogic);

    /// <summary>
    /// Uncapitalizes a string.
    /// </summary>
    public static readonly InnerFunction StringUncapitalize = new ("uncapitalize", [new("self")], StringUncapitalizeLogic);

    /// <summary>
    /// Extract a substring.
    /// </summary>
    public static readonly InnerFunction StringSubstring = new ("substring", [new("self"), new("start"), new("length", new Integer(0))], StringSubstringLogic);

    /// <summary>
    /// Inserts a string in another at a given position.
    /// </summary>
    public static readonly InnerFunction StringInsert = new ("insert", [new("self"), new("index"), new("value")], StringInsertLogic);

    /// <summary>
    /// Deletes a sequence of characters from within a string.
    /// </summary>
    public static readonly InnerFunction StringRemove = new ("remove", [new("self"), new("index"), new("count", new Integer(0))], StringRemoveLogic);

    /// <summary>
    /// Replaces of occurences of some substring with another substring.
    /// </summary>
    public static readonly InnerFunction StringReplace = new ("replace", [new("self"), new("pattern"), new("value")], StringReplaceLogic);

    /// <summary>
    /// Removes the given characters from the left of a string.
    /// </summary>
    public static readonly InnerFunction StringTrimLeft = new ("ltrim", [new("self"), new("chars", new String(" "))], StringTrimLeftLogic);

    /// <summary>
    /// Removes the given characters from the right of a string.
    /// </summary>
    public static readonly InnerFunction StringTrimRight = new ("rtrim", [new("self"), new("chars", new String(" "))], StringTrimRightLogic);

    /// <summary>
    /// Removes the given characters around a string.
    /// </summary>
    public static readonly InnerFunction StringTrim = new ("trim", [new("self"), new("chars", new String(" "))], StringTrimLogic);

    /// <summary>
    /// Adds the given characters to the left of a string until it reaches the given width.
    /// </summary>
    public static readonly InnerFunction StringPadLeft = new ("lpad", [new("self"), new("width"), new("padding", new String(" "))], StringPadLeftLogic);

    /// <summary>
    /// Adds the given characters to the right of a string until it reaches the given width.
    /// </summary>
    public static readonly InnerFunction StringPadRight = new ("rpad", [new("self"), new("width"), new("padding", new String(" "))], StringPadRightLogic);

    /// <summary>
    /// Split a string into substrings separated by the given pattern.
    /// </summary>
    public static readonly InnerFunction StringSplit = new ("split", [new("self"), new("pattern", new String("/\\s+/"))], StringSplitLogic);

    #endregion

    #region Blob specific methods

    /// <summary>
    /// Determines the length of a string.
    /// </summary>
    public static readonly InnerFunction BlobLength = new("length", [new("self")], BlobLengthLogic);

    /// <summary>
    /// Creates a blob with the given number of bytes.
    /// </summary>
    public static readonly InnerFunction BlobOf = new("of", [new("length")], BlobOfLogic);

    /// <summary>
    /// Creates from the given hexadecimal string.
    /// </summary>
    public static readonly InnerFunction BlobFromHexString = new("fromHexString", [new("str")], BlobFromHexStringLogic);

    /// <summary>
    /// Converts a blob to a hexadecimal string.
    /// </summary>
    public static readonly InnerFunction BlobToHexString = new("toHexString", [new("self")], BlobToHexStringLogic);

    /// <summary>
    /// Creates from the given base 64 string.
    /// </summary>
    public static readonly InnerFunction BlobFromBase64String = new("fromBase64String", [new("str")], BlobFromBase64StringLogic);

    /// <summary>
    /// Converts a blob to a base 64 string.
    /// </summary>
    public static readonly InnerFunction BlobToBase64String = new("toBase64String", [new("self")], BlobToBase64StringLogic);

    /// <summary>
    /// Searches for a string in another string.
    /// </summary>
    public static readonly InnerFunction BlobIndexOf = new("indexOf", [new("self"), new("value"), new("start", new Integer(0)), new("length", new Integer(0))], BlobIndexOfLogic);

    /// <summary>
    /// Reversely searches for a string in another string.
    /// </summary>
    public static readonly InnerFunction BlobLastIndexOf = new("lastIndexOf", [new("self"), new("value"), new("start", new Integer(-1)), new("length", new Integer(0))], BlobLastIndexOfLogic);

    /// <summary>
    /// Fills a blob with the given value.
    /// </summary>
    public static readonly InnerFunction BlobFill = new("fill", [new("self"), new("fillByte"), new("start", new Integer(0)), new("length", new Integer(0))], BlobFillLogic);

    /// <summary>
    /// Copies a blob to another.
    /// </summary>
    public static readonly InnerFunction BlobCopyTo = new("copyTo", [new("self"), new("other"), new("srcIndex", new Integer(0)), new("destIndex", new Integer(0)), new("length", new Integer(0))], BlobCopyToLogic);

    /// <summary>
    /// Resizes a blob preserving its content.
    /// </summary>
    public static readonly InnerFunction BlobResize = new("resize", [new("self"), new("newSize")], BlobResizeLogic);

    #endregion

    #region Tuple specific methods

    /// <summary>
    /// Gets the number of items in a list.
    /// </summary>
    public static readonly InnerFunction TupleSize = new("size", [new("self")], TupleSizeLogic);

    /// <summary>
    /// Gets the first index of an item in a list.
    /// </summary>
    public static readonly InnerFunction TupleIndexOf = new("indexOf", [new("self"), new("value"), new("start", new Integer(0)), new("count", new Integer(0))], TupleIndexOfLogic);

    /// <summary>
    /// Gets the last index of an item in a list.
    /// </summary>
    public static readonly InnerFunction TupleLastIndexOf = new("lastIndexOf", [new("self"), new("value"), new("start", new Integer(-1)), new("count", new Integer(0))], TupleLastIndexOfLogic);

    #endregion

    #region List specific methods

    /// <summary>
    /// Gets the number of items in a list.
    /// </summary>
    public static readonly InnerFunction ListSize = new("size", [new("self")], ListSizeLogic);

    /// <summary>
    /// Joins several strings into one.
    /// </summary>
    public static readonly InnerFunction ListJoin = new ("join", [new("self"), new("separator", new String(""))], ListJoinLogic);

    /// <summary>
    /// Adds an item in a list.
    /// </summary>
    public static readonly InnerFunction ListAdd = new ("add", [new("self"), new("value")], ListAddLogic);

    /// <summary>
    /// Inserts an item in a list at the specified position.
    /// </summary>
    public static readonly InnerFunction ListInsert = new ("insert", [new("self"), new("index"), new("value")], ListInsertLogic);

    /// <summary>
    /// Inserts a list in anoter list at the specified position.
    /// </summary>
    public static readonly InnerFunction ListInsertAll = new ("insertAll", [new("self"), new("index"), new("other")], ListInsertAllLogic);

    /// <summary>
    /// Gets the first index of an item in a list.
    /// </summary>
    public static readonly InnerFunction ListIndexOf = new ("indexOf", [new("self"), new("value"), new("start", new Integer(0)), new("count", new Integer(0))], ListIndexOfLogic);

    /// <summary>
    /// Gets the last index of an item in a list.
    /// </summary>
    public static readonly InnerFunction ListLastIndexOf = new ("lastIndexOf", [new("self"), new("value"), new("start", new Integer(-1)), new("count", new Integer(0))], ListLastIndexOfLogic);

    /// <summary>
    /// Makes a binary search on a sorted list.
    /// </summary>
    public static readonly InnerFunction ListBinarySearch = new ("bsearch", [new("self"), new("value"), new("start")], ListBinarySearchLogic);

    /// <summary>
    /// Gets the number of occurences of a value in a list.
    /// </summary>
    public static readonly InnerFunction ListFrequencyOf = new ("frequencyOf", [new("self"), new("value"), new("start", new Integer(0)), new("count", new Integer(0))], ListFrequencyOfLogic);

    /// <summary>
    /// Removes an item from a list.
    /// </summary>
    public static readonly InnerFunction ListRemove = new ("remove", [new("self"), new("value")], ListRemoveLogic);

    /// <summary>
    /// Removes an item  or a range of items from a list at the given position.
    /// </summary>
    public static readonly InnerFunction ListRemoveAt = new ("removeAt", [new("self"), new("index"), new("count", new Integer(1))], ListRemoveAtLogic);

    /// <summary>
    /// Clears the content of a list.
    /// </summary>
    public static readonly InnerFunction ListClear = new ("clear", [new("self")], ListClearLogic);

    /// <summary>
    /// Sorts a list in ascending order.
    /// </summary>
    public static readonly InnerFunction ListSort = new ("sort", [new("self"), new("comparison", Void.Value)], ListSortLogic);

    /// <summary>
    /// Gets the inverse of a list.
    /// </summary>
    public static readonly InnerFunction ListInverse = new ("inverse", [new("self")], ListInverseLogic);

    /// <summary>
    /// Gets a sublist of a list.
    /// </summary>
    public static readonly InnerFunction ListSublist = new ("sublist", [new("self"), new("index"), new("count")], ListSublistLogic);

    /// <summary>
    /// Gets a copy of the calling list where any item is unique.
    /// </summary>
    public static readonly InnerFunction ListUnique = new ("unique", [new("self")], ListUniqueLogic);

    /// <summary>
    /// Maps a list to another.
    /// </summary>
    public static readonly InnerFunction ListMapTo = new ("mapTo", [new("self"), new("other")], ListMapToLogic);

    /// <summary>
    /// Sorts a list in a random order.
    /// </summary>
    public static readonly InnerFunction ListShuffle = new ("shuffle", [new("self")], ListShuffleLogic);

    #endregion

    #region Set specific methods

    /// <summary>
    /// Gets the number of items in a set.
    /// </summary>
    public static readonly InnerFunction SetSize = new("size", [new("self")], SetSizeLogic);

    /// <summary>
    /// Adds an item in a set.
    /// </summary>
    public static readonly InnerFunction SetAdd = new ("add", [new("self"), new("value")], SetAddLogic);

    /// <summary>
    /// Removes an item from a set.
    /// </summary>
    public static readonly InnerFunction SetRemove = new ("remove", [new("self"), new("value")], SetRemoveLogic);

    /// <summary>
    /// Clears the content of a set.
    /// </summary>
    public static readonly InnerFunction SetClear = new ("clear", [new("self")], SetClearLogic);

    #endregion

    #region Queue specific methods

    /// <summary>
    /// Gets the number of items in a set.
    /// </summary>
    public static readonly InnerFunction QueueSize = new("size", [new("self")], QueueSizeLogic);

    /// <summary>
    /// Creates a queue with the given initial content.
    /// </summary>
    public static readonly InnerFunction QueueOf = new ("of", [new("values", false, true, null)], QueueOfLogic);

    /// <summary>
    /// Enqueues an item in a set.
    /// </summary>
    public static readonly InnerFunction QueueEnqueue = new ("enqueue", [new("self"), new("value")], QueueEnqueueLogic);

    /// <summary>
    /// Checks if a set contains some value.
    /// </summary>
    public static readonly InnerFunction QueuePeek = new ("peek", [new("self")], QueuePeekLogic);

    /// <summary>
    /// Dequeues an item from a set.
    /// </summary>
    public static readonly InnerFunction QueueDequeue = new ("dequeue", [new("self")], QueueDequeueLogic);

    /// <summary>
    /// Clears the content of a set.
    /// </summary>
    public static readonly InnerFunction QueueClear = new ("clear", [new("self")], QueueClearLogic);

    #endregion

    #region Stack specific methods

    /// <summary>
    /// Gets the number of items in a set.
    /// </summary>
    public static readonly InnerFunction StackSize = new("size", [new("self")], StackSizeLogic);

    /// <summary>
    /// Creates a stack with the given initial content.
    /// </summary>
    public static readonly InnerFunction StackOf = new ("of", [new("values", false, true, null)], StackOfLogic);

    /// <summary>
    /// Pushs an item in a set.
    /// </summary>
    public static readonly InnerFunction StackPush = new ("push", [new("self"), new("value")], StackPushLogic);

    /// <summary>
    /// Checks if a set contains some value.
    /// </summary>
    public static readonly InnerFunction StackPeek = new ("peek", [new("self")], StackPeekLogic);

    /// <summary>
    /// Pops an item from a set.
    /// </summary>
    public static readonly InnerFunction StackPop = new ("pop", [new("self")], StackPopLogic);

    /// <summary>
    /// Clears the content of a set.
    /// </summary>
    public static readonly InnerFunction StackClear = new ("clear", [new("self")], StackClearLogic);

    #endregion

    #region Map specific methods

    /// <summary>
    /// Gets the number of key-value pairs of a map.
    /// </summary>
    public static readonly InnerFunction MapSize = new("size", [new("self")], MapSizeLogic);

    /// <summary>
    /// Checks if a map contains some key.
    /// </summary>
    public static readonly InnerFunction MapContainsKey = new("containsKey", [new("self"), new("key")], MapContainsKeyLogic);

    /// <summary>
    /// Checks if a map contains some value.
    /// </summary>
    public static readonly InnerFunction MapContainsValue = new("containsValue", [new("self"), new("value")], MapContainsValueLogic);

    /// <summary>
    /// Gets the set of keys of a map.
    /// </summary>
    public static readonly InnerFunction MapKeys = new("keys", [new("self")], MapKeysLogic);

    /// <summary>
    /// Gets the set of values of a map.
    /// </summary>
    public static readonly InnerFunction MapValues = new("values", [new("self")], MapValuesLogic);

    /// <summary>
    /// Gets the number of occurences of a value in a map.
    /// </summary>
    public static readonly InnerFunction MapFrequencyOf = new("frequencyOf", [new("self"), new("value")], MapFrequencyOfLogic);

    /// <summary>
    /// Gets the set of distinct keys to which a value is bound in a map.
    /// </summary>
    public static readonly InnerFunction MapKeysOf = new("keysOf", [new("self"), new("value")], MapKeysOfLogic);

    /// <summary>
    /// Creates a new map from the given one where key-value pairs are reversed.
    /// </summary>
    public static readonly InnerFunction MapInverse = new("inverse", [new("self")], MapInverseLogic);

    /// <summary>
    /// Remove a key-value pair from a map.
    /// </summary>
    public static readonly InnerFunction MapRemove = new("remove", [new("self"), new("key")], MapRemoveLogic);

    /// <summary>
    /// Remove a set of key-value pairs from a map.
    /// </summary>
    public static readonly InnerFunction MapRemoveAll = new("removeAll", [new("self"), new("keys")], MapRemoveAllLogic);

    /// <summary>
    /// Empties a map.
    /// </summary>
    public static readonly InnerFunction MapClear = new("clear", [new("self")], MapClearLogic);

    #endregion

    #endregion

    #region Creation of callable wrappers

    /// <summary>
    /// Generates a wrapper <see cref="Function"/> to make an inner function callable with the standard syntax.
    /// </summary>
    /// <returns>A <see cref="Function"/></returns>
    public Function ToFunction()
    {
        var arguments = Parameters.Select(p => new VariableRef(p.Name)).ToArray();
        return new Function(Parameters, Block.Return(new InnerFunctionCall(this, arguments)));
    }

    /// <summary>
    /// Generates a wrapper <see cref="Function"/> to make an inner function callable as a method.
    /// The first arguments is assumed to be the caller; so it's fixed to a <see cref="SelfReference"/>
    /// </summary>
    /// <returns>A <see cref="Function"/></returns>
    public Function ToMethodFunction()
    {
        var arguments = Parameters.Skip(1)
                                  .Select(p => (Expression)new VariableRef(p.Name))
                                  .Prepend(new SelfReference())
                                  .ToArray();

        return new Function(Parameters[1..], Block.Return(new InnerFunctionCall(this, arguments)));
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
        var reader = new ClassMethod(ClassProperty.GetReaderName(Name), Scope.Public, Modifier.Static, ToFunction());
        return new ClassProperty(Name, Scope.Public, Modifier.Static, reader, null);
    }

    /// <summary>
    /// Wraps an inner function into an instance read-only property.
    /// </summary>
    /// <returns>A <see cref="ClassProperty"/></returns>
    public ClassProperty ToInstanceProperty()
    {
        var reader = new ClassMethod(ClassProperty.GetReaderName(Name), Scope.Public, Modifier.Default, ToMethodFunction());
        return new ClassProperty(Name, Scope.Public, Modifier.Default, reader, null);
    }

    /// <summary>
    /// Wraps an inner function into a readonly indexer.
    /// </summary>
    /// <returns>A <see cref="ClassProperty"/></returns>
    public ClassProperty ToIndexer()
    {
        var reader = new ClassMethod(ClassProperty.GetReaderName(ClassProperty.INDEXER_NAME), Scope.Public, Modifier.Default, ToMethodFunction());
        return new ClassProperty(ClassProperty.INDEXER_NAME, Scope.Public, Modifier.Default, reader, null);
    }

    #endregion

    #region Utility

    private static uint NextUInt32(RandomNumberGenerator rng)
    {
        var uintBytes = new byte[4];
        rng.GetBytes(uintBytes);
        return BitConverter.ToUInt32(uintBytes, 0);
    }

    private static double NextDouble(RandomNumberGenerator rng)
    {
        return (double)NextUInt32(rng) / uint.MaxValue;
    }

    private static void CheckArgType(DataItem arg, Class klass, string function, int rank)
    {
        if (arg.Class != klass)
            throw new ArgumentException(string.Format(Resources.ArgMustBeOfType, rank, function, klass.Name));
    }

    private static void CheckSingleChar(string s, string function, int rank)
    {
        if (s.Length != 1)
            throw new ArgumentException(string.Format(Resources.SingleCharExpected, rank, function));
    }

    private static string FormatList(string mask, List<DataItem> values)
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
                        sbItem.Clear();
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

    private static string FormatItem(string mask, List<DataItem> values)
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

        var value = values[index].ToString(format, CultureInfo.CurrentUICulture);

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