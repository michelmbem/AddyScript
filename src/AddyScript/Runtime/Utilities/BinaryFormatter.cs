using System;
using System.IO;


namespace AddyScript.Runtime.Utilities;


public class BinaryFormatter(Endianness endianness, Stream stream) : IDisposable
{
    public Endianness Endianness => endianness;

    public Stream Stream => stream;

    public static void SwapBytes(byte[] buffer)
    {
        for (int i = 0, j = buffer.Length - 1; i < j; ++i, --j)
            (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
    }

    public void Flush() => Stream.Flush();

    public void Close() => Stream.Close();

    public void Dispose()
    {
        Stream.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Reading

    public bool ReadBoolean() => Stream.ReadByte() != 0;

    public sbyte ReadSByte() => (sbyte)Stream.ReadByte();

    public byte ReadByte() => (byte)Stream.ReadByte();

    public short ReadInt16()
    {
        var buffer = new byte[2];
        Stream.Read(buffer, 0, buffer.Length);

        return Endianness switch
        {
            Endianness.BigEndian => (short)((buffer[0] << 8) | buffer[1]),
            _ => (short)((buffer[1] << 8) | buffer[0]),
        };
    }

    public ushort ReadUInt16()
    {
        var buffer = new byte[2];
        Stream.Read(buffer, 0, buffer.Length);

        return Endianness switch
        {
            Endianness.BigEndian => (ushort)((buffer[0] << 8) | buffer[1]),
            _ => (ushort)((buffer[1] << 8) | buffer[0]),
        };
    }

    public int ReadInt32()
    {
        var buffer = new byte[4];
        Stream.Read(buffer, 0, buffer.Length);

        return Endianness switch
        {
            Endianness.BigEndian => (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3],
            _ => (buffer[3] << 24) | (buffer[2] << 16) | (buffer[1] << 8) | buffer[0],
        };
    }

    public uint ReadUInt32()
    {
        var buffer = new byte[4];
        Stream.Read(buffer, 0, buffer.Length);

        return Endianness switch
        {
            Endianness.BigEndian => ((uint)buffer[0] << 24) | ((uint)buffer[1] << 16) | ((uint)buffer[2] << 8) | buffer[3],
            _ => ((uint)buffer[3] << 24) | ((uint)buffer[2] << 16) | ((uint)buffer[1] << 8) | buffer[0],
        };
    }

    public long ReadInt64()
    {
        var buffer = new byte[8];
        Stream.Read(buffer, 0, buffer.Length);

        return Endianness switch
        {
            Endianness.BigEndian => ((long)buffer[0] << 56) | ((long)buffer[1] << 48) | ((long)buffer[2] << 40) | ((long)buffer[3] << 32) |
                                    ((long)buffer[4] << 24) | ((long)buffer[5] << 16) | ((long)buffer[6] << 8) | buffer[7],
            _ => ((long)buffer[7] << 56) | ((long)buffer[6] << 48) | ((long)buffer[5] << 40) | ((long)buffer[4] << 32) |
                 ((long)buffer[3] << 24) | ((long)buffer[2] << 16) | ((long)buffer[1] << 8) | buffer[0],
        };
    }

    public ulong ReadUInt64()
    {
        var buffer = new byte[8];
        Stream.Read(buffer, 0, buffer.Length);

        return Endianness switch
        {
            Endianness.BigEndian => ((ulong)buffer[0] << 56) | ((ulong)buffer[1] << 48) | ((ulong)buffer[2] << 40) | ((ulong)buffer[3] << 32) |
                                    ((ulong)buffer[4] << 24) | ((ulong)buffer[5] << 16) | ((ulong)buffer[6] << 8) | buffer[7],
            _ => ((ulong)buffer[7] << 56) | ((ulong)buffer[6] << 48) | ((ulong)buffer[5] << 40) | ((ulong)buffer[4] << 32) |
                 ((ulong)buffer[3] << 24) | ((ulong)buffer[2] << 16) | ((ulong)buffer[1] << 8) | buffer[0],
        };
    }

    public float ReadSingle()
    {
        var buffer = new byte[4];
        Stream.Read(buffer, 0, buffer.Length);

        if (Endianness == Endianness.BigEndian)
            SwapBytes(buffer);

        return BitConverter.ToSingle(buffer, 0);
    }

    public double ReadDouble()
    {
        var buffer = new byte[8];
        Stream.Read(buffer, 0, buffer.Length);

        if (Endianness == Endianness.BigEndian)
            SwapBytes(buffer);

        return BitConverter.ToDouble(buffer, 0);
    }

    public byte[] ReadBytes(int count)
    {
        var buffer = new byte[count];
        Stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    #endregion

    #region Writing

    public void Write(bool value) => Stream.WriteByte(value ? (byte)1 : (byte)0);

    public void Write(sbyte value) => Stream.WriteByte((byte)value);

    public void Write(byte value) => Stream.WriteByte(value);

    public void Write(short value)
    {
        if (Endianness == Endianness.BigEndian)
        {
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)(value & 0xff));
        }
        else
        {
            Stream.WriteByte((byte)(value & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
        }
    }

    public void Write(ushort value)
    {
        if (Endianness == Endianness.BigEndian)
        {
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)(value & 0xff));
        }
        else
        {
            Stream.WriteByte((byte)(value & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
        }
    }

    public void Write(int value)
    {
        if (Endianness == Endianness.BigEndian)
        {
            Stream.WriteByte((byte)((value >> 24) & 0xff));
            Stream.WriteByte((byte)((value >> 16) & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)(value & 0xff));
        }
        else
        {
            Stream.WriteByte((byte)(value & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)((value >> 16) & 0xff));
            Stream.WriteByte((byte)((value >> 24) & 0xff));
        }
    }

    public void Write(uint value)
    {
        if (Endianness == Endianness.BigEndian)
        {
            Stream.WriteByte((byte)((value >> 24) & 0xff));
            Stream.WriteByte((byte)((value >> 16) & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)(value & 0xff));
        }
        else
        {
            Stream.WriteByte((byte)(value & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)((value >> 16) & 0xff));
            Stream.WriteByte((byte)((value >> 24) & 0xff));
        }
    }

    public void Write(long value)
    {
        if (Endianness == Endianness.BigEndian)
        {
            Stream.WriteByte((byte)((value >> 56) & 0xff));
            Stream.WriteByte((byte)((value >> 48) & 0xff));
            Stream.WriteByte((byte)((value >> 40) & 0xff));
            Stream.WriteByte((byte)((value >> 32) & 0xff));
            Stream.WriteByte((byte)((value >> 24) & 0xff));
            Stream.WriteByte((byte)((value >> 16) & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)(value & 0xff));
        }
        else
        {
            Stream.WriteByte((byte)(value & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)((value >> 16) & 0xff));
            Stream.WriteByte((byte)((value >> 24) & 0xff));
            Stream.WriteByte((byte)((value >> 32) & 0xff));
            Stream.WriteByte((byte)((value >> 40) & 0xff));
            Stream.WriteByte((byte)((value >> 48) & 0xff));
            Stream.WriteByte((byte)((value >> 56) & 0xff));
        }
    }

    public void Write(ulong value)
    {
        if (Endianness == Endianness.BigEndian)
        {
            Stream.WriteByte((byte)((value >> 56) & 0xff));
            Stream.WriteByte((byte)((value >> 48) & 0xff));
            Stream.WriteByte((byte)((value >> 40) & 0xff));
            Stream.WriteByte((byte)((value >> 32) & 0xff));
            Stream.WriteByte((byte)((value >> 24) & 0xff));
            Stream.WriteByte((byte)((value >> 16) & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)(value & 0xff));
        }
        else
        {
            Stream.WriteByte((byte)(value & 0xff));
            Stream.WriteByte((byte)((value >> 8) & 0xff));
            Stream.WriteByte((byte)((value >> 16) & 0xff));
            Stream.WriteByte((byte)((value >> 24) & 0xff));
            Stream.WriteByte((byte)((value >> 32) & 0xff));
            Stream.WriteByte((byte)((value >> 40) & 0xff));
            Stream.WriteByte((byte)((value >> 48) & 0xff));
            Stream.WriteByte((byte)((value >> 56) & 0xff));
        }
    }

    public void Write(float value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        if (Endianness == Endianness.BigEndian)
            SwapBytes(buffer);
        Stream.Write(buffer, 0, buffer.Length);
    }

    public void Write(double value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        if (Endianness == Endianness.BigEndian)
            SwapBytes(buffer);
        Stream.Write(buffer, 0, buffer.Length);
    }

    public void Write(byte[] buffer) => Stream.Write(buffer, 0, buffer.Length);

    #endregion
}