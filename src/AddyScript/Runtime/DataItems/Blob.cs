using System;
using System.Collections.Generic;
using System.IO;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;


namespace AddyScript.Runtime.DataItems;


public sealed class Blob(byte[] buffer) : DataItem
{
    public override Class Class => Class.Blob;

    public override byte[] AsByteArray => buffer;

    public override object AsNativeObject => buffer;

    public override object Clone()
    {
        var cloneBuffer = new byte[buffer.Length];
        Array.Copy(buffer, cloneBuffer, buffer.Length);
        return new Blob(cloneBuffer);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
        => $"b'{Convert.ToBase64String(buffer)}'";

    protected override bool UnsafeEquals(DataItem other)
    {
        var otherBuffer = other.AsByteArray;
        if (buffer.Length != otherBuffer.Length) return false;

        for (int i = 0; i < buffer.Length; ++i)
            if (buffer[i] != otherBuffer[i])
                return false;

        return true;
    }

    public override int GetHashCode() => buffer.GetHashCode();

    protected override int UnsafeCompareTo(DataItem other)
    {
        var otherBuffer = other.AsByteArray;
        int l = Math.Min(buffer.Length, otherBuffer.Length);

        for (int i = 0; i < l; ++i)
        {
            int cmp = Math.Sign(buffer[i] - otherBuffer[i]);
            if (cmp != 0) return cmp;
        }

        if (buffer.Length < otherBuffer.Length) return -1;
        if (buffer.Length > otherBuffer.Length) return +1;
        return 0;
    }

    public override object ConvertTo(Type targetType)
    {
        return Type.GetTypeCode(targetType) switch
        {
            TypeCode.String => StringUtil.ByteArray2String(buffer),
            _ => base.ConvertTo(targetType)
        };
    }

    public override bool IsEmpty() => buffer.Length <= 0;

    public override DataItem BinaryOperation(BinaryOperator _operator, DataItem operand)
    {
        switch (_operator)
        {
            case BinaryOperator.Plus:
                {
                    var operandBuffer = operand.AsByteArray;
                    var result = new byte[buffer.Length + operandBuffer.Length];
                    Array.Copy(buffer, result, buffer.Length);
                    Array.Copy(operandBuffer, 0, result, buffer.Length, operandBuffer.Length);
                    return new Blob(result);
                }
            case BinaryOperator.Times:
                {
                    var result = new MemoryStream();
                    int n = operand.AsInt32;
                    for (int i = 0; i < n; ++i) result.Write(buffer);
                    return new Blob(result.ToArray());
                }
            case BinaryOperator.Contains:
                return Boolean.FromBool(Array.IndexOf(buffer, (byte)operand.AsInt32) >= 0); // Todo: Am�liorer!!
            default:
                return base.BinaryOperation(_operator, operand);
        }
    }

    public override DataItem GetItem(DataItem index)
    {
        int n = index.AsInt32, l = buffer.Length;
        if (l <= 0 || n >= l) return null;
        while (n < 0) n += l;
        return new Integer(buffer[n]);
    }

    public override void SetItem(DataItem index, DataItem value)
    {
        int n = index.AsInt32, l = buffer.Length;
        if (l <= 0 || n >= l) throw new ArgumentOutOfRangeException();
        while (n < 0) n += l;
        buffer[n] = (byte)value.AsInt32;
    }

    public override IEnumerable<KeyValuePair<DataItem, DataItem>> GetEnumerable()
    {
        for (int i = 0; i < buffer.Length; ++i)
        {
            var key = new Integer(i);
            var val = new Integer(buffer[i]);
            yield return new KeyValuePair<DataItem, DataItem>(key, val);
        }
    }
}