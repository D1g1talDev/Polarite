using System;
using System.Text;

using UnityEngine;

public class BinaryPacketReader
{
    public byte[] buffer;
    private int index;

    public BinaryPacketReader(byte[] data)
    {
        buffer = data;
        index = 0;
    }

    public byte ReadByte()
    {
        if (index >= buffer.Length) throw new IndexOutOfRangeException("Attempted to read past end of buffer");
        return buffer[index++];
    }

    public bool ReadBool()
    {
        return buffer[index++] == 1;
    }

    public int ReadInt()
    {
        if (index + 4 > buffer.Length) throw new IndexOutOfRangeException("Attempted to read past end of buffer");
        int value = BitConverter.ToInt32(buffer, index);
        index += 4;
        return value;
    }

    public float ReadFloat()
    {
        if (index + 4 > buffer.Length) throw new IndexOutOfRangeException("Attempted to read past end of buffer");
        float value = BitConverter.ToSingle(buffer, index);
        index += 4;
        return value;
    }

    public ulong ReadULong()
    {
        if (index + 8 > buffer.Length) throw new IndexOutOfRangeException("Attempted to read past end of buffer");
        ulong value = BitConverter.ToUInt64(buffer, index);
        index += 8;
        return value;
    }

    public string ReadString()
    {
        int length = ReadInt();
        if (length < 0 || index + length > buffer.Length) throw new IndexOutOfRangeException("Attempted to read past end of buffer");
        string value = Encoding.UTF8.GetString(buffer, index, length);
        index += length;
        return value;
    }

    public Vector3 ReadVector3()
    {
        float x = ReadFloat();
        float y = ReadFloat();
        float z = ReadFloat();
        return new Vector3(x, y, z);
    }

    public Quaternion ReadQuaternion()
    {
        float x = ReadFloat();
        float y = ReadFloat();
        float z = ReadFloat();
        float w = ReadFloat();
        return new Quaternion(x, y, z, w);
    }

    public int[] ReadIntArray()
    {
        int len = ReadInt();
        if (len < 0) return new int[0];
        int[] arr = new int[len];
        for (int i = 0; i < len; i++)
            arr[i] = ReadInt();
        return arr;
    }

    public float[] ReadFloatArray()
    {
        int len = ReadInt();
        if (len < 0) return new float[0];
        float[] arr = new float[len];
        for (int i = 0; i < len; i++)
            arr[i] = ReadFloat();
        return arr;
    }

    public byte[] ReadByteArray()
    {
        int len = ReadInt();
        if (len < 0) return new byte[0];
        if (index + len > buffer.Length) throw new IndexOutOfRangeException("Attempted to read past end of buffer");
        byte[] arr = new byte[len];
        System.Buffer.BlockCopy(buffer, index, arr, 0, len);
        index += len;
        return arr;
    }

    public string[] ReadStringArray()
    {
        int len = ReadInt();
        if (len < 0) return new string[0];
        string[] arr = new string[len];
        for (int i = 0; i < len; i++)
            arr[i] = ReadString();
        return arr;
    }
    public T ReadEnum<T>() where T : Enum
    {
        // default behavior: enums in binary packets may be transmitted as a single byte
        // but keep a fallback to int if necessary
        if (index < buffer.Length)
        {
            byte b = ReadByte();
            return (T)Enum.ToObject(typeof(T), (int)b);
        }
        return (T)Enum.ToObject(typeof(T), 0);
    }

    public byte[] ReadBytes()
    {
        int length = ReadInt(); // read length first
        if (length < 0) return new byte[0];
        if (index + length > buffer.Length) throw new IndexOutOfRangeException("Attempted to read past end of buffer");
        byte[] data = new byte[length];
        Array.Copy(buffer, index, data, 0, length);
        index += length;
        return data;
    }

    public byte[] ReadBytes(int length)
    {
        byte[] data = new byte[length];
        Array.Copy(buffer, index, data, 0, length);
        index += length;
        return data;
    }

}
