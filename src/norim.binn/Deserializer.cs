using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace norim.binn
{
    public static class Deserializer
    {
        public static T Deserialize<T>(byte[] data)
        {
            using(var buffer = new MemoryStream(data))
            {
                return (T)DeserializeInternal(buffer);
            }
        }

        public static object Deserialize(byte[] data)
        {
            using(var buffer = new MemoryStream(data))
            {
                return DeserializeInternal(buffer);
            }            
        }

        public static object DeserializeInternal(Stream buffer)
        {
            var type = (byte)buffer.ReadByte();

            switch(type)
            {
                case Types.Null:
                    return null;
                case Types.String:
                    return ReadString(buffer);
                case Types.True:
                    return true;
                case Types.False:
                    return false;
                case Types.Guid:
                    return ReadGuid(buffer);
                case Types.DateTime:
                    return ReadDateTime(buffer);
                case Types.UInt8:
                    return ReadByte(buffer);
                case Types.Int8:
                    return ReadSByte(buffer);
                case Types.UInt16:
                    return ReadUInt16(buffer);
                case Types.Int16:
                    return ReadInt16(buffer);
                default:
                    throw new NotSupportedException($"Type '0x{type.ToString("X").ToLower()}' not supported.");
            }
        }

        private static int ReadInt16(Stream buffer)
        {
            var valueBuffer = new byte[sizeof(short)];

            buffer.Read(valueBuffer, 0, valueBuffer.Length);

            return BitConverter.ToInt16(valueBuffer, 0);
        }

        private static int ReadUInt16(Stream buffer)
        {
            var valueBuffer = new byte[sizeof(ushort)];

            buffer.Read(valueBuffer, 0, valueBuffer.Length);

            return BitConverter.ToUInt16(valueBuffer, 0);
        }

        private static int ReadSByte(Stream buffer)
        {
            return buffer.ReadByte() - (byte.MaxValue + 1);
        }

        private static int ReadByte(Stream buffer)
        {
            return buffer.ReadByte();
        }

        private static DateTime ReadDateTime(Stream buffer)
        {
            var valueBuffer = new byte[sizeof(long)];

            buffer.Read(valueBuffer, 0, valueBuffer.Length);

            return new DateTime(BitConverter.ToInt64(valueBuffer, 0));            
        }

        private static Guid ReadGuid(Stream buffer)
        {
            var valueBuffer = new byte[Marshal.SizeOf(typeof(Guid))];

            buffer.Read(valueBuffer, 0, valueBuffer.Length);

            return new Guid(valueBuffer);
        }

        private static string ReadString(Stream buffer)
        {
            var length = ReadVarint(buffer);
            var valueBuffer = new byte[length];

            buffer.Read(valueBuffer, 0, valueBuffer.Length);

            return Encoding.UTF8.GetString(valueBuffer);
        }

        private static int ReadVarint(Stream buffer)
        {
            var value = buffer.ReadByte();

            if ((value & 0x80) == 0)
                return value;

            var valueBuffer = new byte[4];

            buffer.Seek(-1, SeekOrigin.Current);
            buffer.Read(valueBuffer, 0, valueBuffer.Length);

            return BitConverter.ToInt32(valueBuffer, 0) & 0x7FFFFFFF;
        }
    }
}