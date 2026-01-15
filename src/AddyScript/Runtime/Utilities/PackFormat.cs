using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using AddyScript.Properties;


namespace AddyScript.Runtime.Utilities;


public enum Endianness
{
    Default,
    LittleEndian,
    BigEndian
}


public enum PackFormatType
{
    Boolean,
    Character,
    SByte,
    Byte,
    Short,
    UShort,
    Integer,
    UInteger,
    Long,
    ULong,
    Float,
    Double,
    CString,
    PascalString,
    Pointer,
    PaddingByte
}


public class PackFormatItem
{
    public PackFormatType Type { get; }

    public int Count { get; }

    public PackFormatItem(PackFormatType type, int count = 1)
    {
        Debug.Assert(count > 0);
        Type = type;
        Count = count;
    }

    public static string ToString(PackFormatType type)
    {
        return type switch
        {
            PackFormatType.Boolean => "?",
            PackFormatType.Character => "c",
            PackFormatType.SByte => "b",
            PackFormatType.Byte => "B",
            PackFormatType.Short => "h",
            PackFormatType.UShort => "H",
            PackFormatType.Integer => "i",
            PackFormatType.UInteger => "I",
            PackFormatType.Long => "q",
            PackFormatType.ULong => "Q",
            PackFormatType.Float => "f",
            PackFormatType.Double => "d",
            PackFormatType.CString => "s",
            PackFormatType.PascalString => "p",
            PackFormatType.Pointer => "P",
            PackFormatType.PaddingByte => "x",
            _ => string.Empty,
        };
    }

    public override string ToString() => Count > 1 ? Count + ToString(Type) : ToString(Type);
}


public class PackFormat(Endianness endianness, IEnumerable<PackFormatItem> items)
{
    public Endianness Endianness => endianness;

    public List<PackFormatItem> Items => [..items];

    public int Length
    {
        get
        {
            int l = 0;
            foreach (PackFormatItem item in Items)
                switch (item.Type)
                {
                    case PackFormatType.CString or PackFormatType.PascalString:
                        ++l;
                        break;
                    case not PackFormatType.PaddingByte:
                        l += item.Count;
                        break;
                }
            return l;
        }
    }

    public static PackFormat Parse(string s)
    {
        Debug.Assert(!string.IsNullOrEmpty(s));

        var endianness = Endianness.Default;
        List<PackFormatItem> items = [];
        int i = 0, count = 1;

        switch (s[0])
        {
            case '<':
                endianness = Endianness.LittleEndian;
                ++i;
                break;
            case '>':
                endianness = Endianness.BigEndian;
                ++i;
                break;
            case '@' or '!':
                ++i;
                break;
        }

        while (i < s.Length)
        {
            switch (s[i])
            {
                case '?':
                    items.Add(new PackFormatItem(PackFormatType.Boolean, count));
                    ++i;
                    count = 1;
                    break;
                case 'c':
                    items.Add(new PackFormatItem(PackFormatType.Character, count));
                    ++i;
                    count = 1;
                    break;
                case 'b':
                    items.Add(new PackFormatItem(PackFormatType.SByte, count));
                    ++i;
                    count = 1;
                    break;
                case 'B':
                    items.Add(new PackFormatItem(PackFormatType.Byte, count));
                    ++i;
                    count = 1;
                    break;
                case 'h':
                    items.Add(new PackFormatItem(PackFormatType.Short, count));
                    ++i;
                    count = 1;
                    break;
                case 'H':
                    items.Add(new PackFormatItem(PackFormatType.UShort, count));
                    ++i;
                    count = 1;
                    break;
                case 'i' or 'l':
                    items.Add(new PackFormatItem(PackFormatType.Integer, count));
                    ++i;
                    count = 1;
                    break;
                case 'I' or 'L':
                    items.Add(new PackFormatItem(PackFormatType.UInteger, count));
                    ++i;
                    count = 1;
                    break;
                case 'q':
                    items.Add(new PackFormatItem(PackFormatType.Long, count));
                    ++i;
                    count = 1;
                    break;
                case 'Q':
                    items.Add(new PackFormatItem(PackFormatType.ULong, count));
                    ++i;
                    count = 1;
                    break;
                case 'f' or 'e':
                    items.Add(new PackFormatItem(PackFormatType.Float, count));
                    ++i;
                    count = 1;
                    break;
                case 'd':
                    items.Add(new PackFormatItem(PackFormatType.Double, count));
                    ++i;
                    count = 1;
                    break;
                case 's':
                    items.Add(new PackFormatItem(PackFormatType.CString, count));
                    ++i;
                    count = 1;
                    break;
                case 'p':
                    items.Add(new PackFormatItem(PackFormatType.PascalString, count));
                    ++i;
                    count = 1;
                    break;
                case 'P':
                    items.Add(new PackFormatItem(PackFormatType.Pointer, count));
                    ++i;
                    count = 1;
                    break;
                case 'x':
                    items.Add(new PackFormatItem(PackFormatType.PaddingByte, count));
                    ++i;
                    count = 1;
                    break;
                case var c when char.IsWhiteSpace(c):
                    ++i;
                    break;
                case var c when char.IsDigit(c):
                {
                    int j = i;
                    do
                        ++j;
                    while (j < s.Length && char.IsDigit(s[j]));
                    if (j >= s.Length)
                        throw new ArgumentException(Resources.PackInvalidFormat);
                    count = int.Parse(s[i..j]);
                    i = j;
                    break;
                }
                default:
                    throw new ArgumentException(Resources.PackInvalidFormat);
            }
        }

        return new PackFormat(endianness, items);
    }

    public override string ToString()
    {
        StringBuilder sb = new ();

        switch (Endianness)
        {
            case Endianness.LittleEndian:
                sb.Append('<');
                break;
            case Endianness.BigEndian:
                sb.Append('>');
                break;
        }

        foreach (var item in Items)
            sb.Append(item);

        return sb.ToString();
    }
}
