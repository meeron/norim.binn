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
                default:
                    throw new NotSupportedException($"Type '0x{type.ToString("X").ToLower()}' not supported.");
            }
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