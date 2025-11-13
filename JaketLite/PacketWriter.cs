using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace Polarite.Multiplayer
{
    public class PacketWriter
    {
        private List<byte> buffer = new List<byte>();

        public void WriteByte(byte value)
        {
            buffer.Add(value);
        }

        public void WriteBool(bool value)
        {
            buffer.Add((byte)(value ? 1 : 0));
        }

        public void WriteInt(int value)
        {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void WriteFloat(float value)
        {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void WriteULong(ulong value)
        {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void WriteString(string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            WriteInt(data.Length);
            buffer.AddRange(data);
        }

        public void WriteVector3(Vector3 v)
        {
            WriteFloat(v.x);
            WriteFloat(v.y);
            WriteFloat(v.z);
        }

        public void WriteQuaternion(Quaternion q)
        {
            WriteFloat(q.x);
            WriteFloat(q.y);
            WriteFloat(q.z);
            WriteFloat(q.w);
        }

        public void WriteIntArray(int[] values)
        {
            WriteInt(values.Length);
            for (int i = 0; i < values.Length; i++)
                WriteInt(values[i]);
        }

        public void WriteFloatArray(float[] values)
        {
            WriteInt(values.Length);
            for (int i = 0; i < values.Length; i++)
                WriteFloat(values[i]);
        }

        public void WriteByteArray(byte[] values)
        {
            WriteInt(values.Length);
            buffer.AddRange(values);
        }

        public void WriteStringArray(string[] values)
        {
            WriteInt(values.Length);
            for (int i = 0; i < values.Length; i++)
                WriteString(values[i]);
        }

        public void WriteEnum<T>(T value) where T : Enum
        {
            WriteString(Enum.GetName(typeof(T), value));
        }

        public void WriteBytes(byte[] data)
        {
            WriteInt(data.Length); // store length first
            buffer.AddRange(data);
        }

        public byte[] GetBytes()
        {
            return buffer.ToArray();
        }
    }
}
