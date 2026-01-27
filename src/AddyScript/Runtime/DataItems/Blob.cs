using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AddyScript.Ast.Expressions;
using AddyScript.Runtime.OOP;
using AddyScript.Runtime.Utilities;
using AddyScript.Translators;


namespace AddyScript.Runtime.DataItems;


public sealed class Blob(byte[] buffer) : DataItem
{
    public override Class Class => Class.Blob;

    public override byte[] AsByteArray => buffer;

    private IEnumerable<DataItem> Items => buffer.Select(b => new Integer(b));

    public override DataItem[] AsArray => [..Items];

    public override List<DataItem> AsList => [..Items];

    public override HashSet<DataItem> AsHashSet => [..Items];

    public override object AsNativeObject => buffer;

    public override object Clone()
    {
        var cloneBuffer = new byte[buffer.Length];
        Array.Copy(buffer, cloneBuffer, buffer.Length);
        return new Blob(cloneBuffer);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        string str = StringUtil.ByteArray2String(buffer);
        return format switch
        {
            "x" or "X" => CodeGenerator.EscapedString(str, false),
            _ => str,
        };
    }

    protected override bool UnsafeEquals(DataItem other)
    {
        var otherBuffer = other.AsByteArray;
        return buffer.Length == otherBuffer.Length &&
               !buffer.Where((item, index) => item != otherBuffer[index])
                      .Any();
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

    public override bool IsEmpty() => buffer.Length == 0;

    public override DataItem UnaryOperation(UnaryOperator _operator)
    {
        if (_operator == UnaryOperator.BitwiseNot)
        {
            var result = new byte[buffer.Length];

            for (int i = 0; i < buffer.Length; ++i)
                result[i] = (byte)(~buffer[i]);

            return new Blob(result);
        }

        return base.UnaryOperation(_operator);
    }

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
            case BinaryOperator.And:
            {
                var operandBuffer = operand.AsByteArray;
                var result = new byte[Math.Max(buffer.Length, operandBuffer.Length)];
                int i = 0, limit = Math.Min(buffer.Length, operandBuffer.Length);
                
                for (; i < limit; ++i)
                    result[i] = (byte)(buffer[i] & operandBuffer[i]);
                
                return new Blob(result);
            }
            case BinaryOperator.Or:
            {
                var operandBuffer = operand.AsByteArray;
                var result = new byte[Math.Max(buffer.Length, operandBuffer.Length)];
                int i = 0, limit = Math.Min(buffer.Length, operandBuffer.Length);
                
                for (; i < limit; ++i)
                    result[i] = (byte)(buffer[i] | operandBuffer[i]);

                while (i < buffer.Length) result[i] = buffer[i++];
                while (i < operandBuffer.Length) result[i] = operandBuffer[i++];

                return new Blob(result);
            }
            case BinaryOperator.ExclusiveOr:
            {
                var operandBuffer = operand.AsByteArray;
                var result = new byte[Math.Max(buffer.Length, operandBuffer.Length)];
                int i = 0, limit = Math.Min(buffer.Length, operandBuffer.Length);
                
                for (; i < limit; ++i)
                    result[i] = (byte)(buffer[i] ^ operandBuffer[i]);

                while (i < result.Length) result[i] = 255;

                return new Blob(result);
            }
            case BinaryOperator.Contains:
                return Boolean.FromBool(Array.IndexOf(buffer, (byte)operand.AsInt32) >= 0); // Todo: Improve!!
            default:
                return base.BinaryOperation(_operator, operand);
        }
    }

    public override DataItem GetProperty(string propertyName) => propertyName switch
    {
        "empty" => Boolean.FromBool(IsEmpty()),
        "length" => new Integer(buffer.Length),
        _ => base.GetProperty(propertyName),
    };

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

    public override DataItem GetItemRange(int lBound, int uBound)
    {
        AdjustBounds(buffer.Length, ref lBound, ref uBound);
        return new Blob(buffer[lBound..uBound]);
    }

    public override void SetItemRange(int lBound, int uBound, DataItem value)
    {
        AdjustBounds(buffer.Length, ref lBound, ref uBound);
        buffer = [..buffer[..lBound], ..value.AsByteArray, ..buffer[uBound..]];
    }

    public override IEnumerable<(DataItem, DataItem)> GetEnumerable()
    {
        for (int i = 0; i < buffer.Length; ++i)
            yield return (new Integer(i), new Integer(buffer[i]));
    }

    public void Resize(int newLength)
    {
        var newBuffer = new byte[newLength];
        Array.Copy(buffer, 0, newBuffer, 0, Math.Min(buffer.Length, newLength));
        buffer = newBuffer;
    }
}
