using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace norim.binn
{
    public static class Serializer
    {
        private static readonly Dictionary<Type, IProperty[]> _classCache = new Dictionary<Type, IProperty[]>();

        public static byte[] Serialize(object value)
        {
            using(var memBuffer = new MemoryStream())
            {
                if (SerializeInternal(value, memBuffer) == 0)
                    throw new NotSupportedException($"Type '{value.GetType()}' is not supported.");

                return memBuffer.ToArray();
            }
        }

        public static void RegisterType<T>()
        {
            var type = typeof(T);
            _classCache.Add(type,
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Select(p => new CompiledProperty<T>(p)).ToArray());
        }

        private static int SerializeInternal(object value, Stream buffer)
        {
            if (value == null)
                return WriteNull(buffer);

            if (value is bool)
                return Write((bool)value, buffer);

            if (value is string)
                return Write((string)value, buffer);

            if (value is int)
                return Write((int)value, buffer);

            if (value is long)
                return Write((long)value, buffer);

            if (value is double)
                return Write((double)value, buffer);

            if (value is Single)
                return Write((Single)value, buffer);

            if (value is decimal)
                return Write((decimal)value, buffer);

            if(value is byte[])
                return Write((byte[])value, buffer);

            if (value is IEnumerable)
                return Write((IEnumerable)value, buffer);

            if (value is Guid)
                return Write((Guid)value, buffer);

            if (value is DateTime)
                return Write((DateTime)value, buffer);

            if (value.GetType().IsClass)
                return WriteClass(value, buffer);

            return 0;
        }

        private static int WriteNull(Stream buffer)
        {
            buffer.WriteByte(Types.Null);

            return 1;
        }

        private static int Write(bool value, Stream buffer)
        {
            buffer.WriteByte(value ? Types.True : Types.False);

            return 1;
        }

        private static int Write(int value, Stream buffer)
        {
            if (value >= 0)
                return Write((uint)value, buffer);

            if (value >= sbyte.MinValue)
                return Write((sbyte)value, buffer);

            if (value >= short.MinValue)
                return Write((short)value, buffer);

            buffer.WriteByte(Types.Int32);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(int));

            return 1 + sizeof(int);
        }

        private static int Write(sbyte value, Stream buffer)
        {
            buffer.WriteByte(Types.Int8);
            buffer.WriteByte((byte)value);

            return 2;
        }

        private static int Write(uint value, Stream buffer)
        {
            if (value <= byte.MaxValue)
                return Write((byte)value, buffer);

            if (value <= ushort.MaxValue)
                return Write((ushort)value, buffer);
                
            buffer.WriteByte(Types.UInt32);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(uint));

            return 1 + sizeof(uint);
        }

        private static int Write(byte value, Stream buffer)
        {
            buffer.WriteByte(Types.UInt8);
            buffer.WriteByte(value);

            return 2;
        }

        private static int Write(ushort value, Stream buffer)
        {
            buffer.WriteByte(Types.UInt16);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(ushort));

            return 1 + sizeof(short);
        }

        private static int Write(short value, Stream buffer)
        {
            buffer.WriteByte(Types.Int16);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(short));

            return 1 + sizeof(short);
        }

        private static int Write(string value, Stream buffer)
        {
            var valueData = Encoding.UTF8.GetBytes(value);
            var varintData = ToVarint(valueData.Length);

            buffer.WriteByte(Types.String);
            buffer.Write(varintData, 0, varintData.Length);
            buffer.Write(valueData, 0, value.Length);

            return 1 + varintData.Length + valueData.Length;
        }

        private static int Write(long value, Stream buffer)
        {
            if (value >= 0)
                return Write((ulong)value, buffer);

            buffer.WriteByte(Types.Int64);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(long));

            return 1 + sizeof(long);
        }

        private static int Write(ulong value, Stream buffer)
        {
            buffer.WriteByte(Types.UInt64);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(ulong));

            return 1 + sizeof(ulong);
        }

        private static int Write(double value, Stream buffer)
        {
            buffer.WriteByte(Types.Float64);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(double));

            return 1 + sizeof(double);        
        }

        private static int Write(decimal value, Stream buffer)
        {
            return Write((double)value, buffer);
        }

        private static int Write(byte[] value, Stream buffer)
        {
            buffer.WriteByte(Types.Blob);
            buffer.Write(BitConverter.GetBytes(value.Length), 0, sizeof(int));
            buffer.Write(value, 0, value.Length);

            return 1 + sizeof(int) + value.Length;
        }

        private static int Write(IEnumerable value, Stream buffer)
        {
            var enumerator = value.GetEnumerator();
            var count = 0;

            using(var enumeratorBuffer = new MemoryStream())
            {
                while(enumerator.MoveNext())
                {
                    if (SerializeInternal(enumerator.Current, enumeratorBuffer) == 0)
                        throw new NotSupportedException($"Type '{enumerator.Current.GetType()}' is not supported.");

                    count++;
                }   

                var bufferVarintData = ToVarint((int)enumeratorBuffer.Length + 3);
                var countVaringData = ToVarint(count);

                buffer.WriteByte(Types.List);
                buffer.Write(bufferVarintData, 0, bufferVarintData.Length);
                buffer.Write(countVaringData, 0 , countVaringData.Length);
                
                enumeratorBuffer.Seek(0, SeekOrigin.Begin);
                enumeratorBuffer.CopyTo(buffer);

                return 1 + bufferVarintData.Length + countVaringData.Length + (int)enumeratorBuffer.Length;             
            }
        }

        public static int Write(Guid value, Stream buffer)
        {
            var data = value.ToByteArray();

            buffer.WriteByte(Types.Guid);
            buffer.Write(data, 0, data.Length);

            return 1 + data.Length;
        }

        public static int Write(DateTime value, Stream buffer)
        {
            var data = BitConverter.GetBytes(value.Ticks);

            buffer.WriteByte(Types.DateTime);
            buffer.Write(data, 0, data.Length);

            return 1 + data.Length;
        }

        private static int WriteClass(object value, Stream buffer)
        {
            var valueType = value.GetType();
            if (!_classCache.ContainsKey(valueType))
            {
                _classCache.Add(valueType,
                    valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Select(p => new Property(p)).ToArray());
            }

            var properties = _classCache[valueType];

            using(var objBuffer = new MemoryStream())
            {
                foreach (var prop in properties)
                {
                    if (!prop.CanRead || !prop.CanWrite)
                        continue;

                    if (prop.Name.Length > 255)
                        throw new OverflowException("Property name should have max length 255.");

                    if (SerializeInternal(prop.GetValue(value), objBuffer) == 0)
                        throw new NotSupportedException($"Type '{prop.DeclaringType}' is not supported.");                    
                }

                var bufferSize = (int)objBuffer.Length;
                var bufferVarintData = ToVarint(bufferSize + 3);
                var countVaringData = ToVarint(properties.Length);

                buffer.WriteByte(Types.Object);
                buffer.Write(bufferVarintData, 0, bufferVarintData.Length);
                buffer.Write(countVaringData, 0 , countVaringData.Length);                

                objBuffer.Seek(0, SeekOrigin.Begin);
                objBuffer.CopyTo(buffer);

                return 1 + bufferVarintData.Length + countVaringData.Length + bufferSize;          
            } 
        }

        private static byte[] ToVarint(int value)
        {
            if (value > 127)
                return BitConverter.GetBytes(value | 0x80000000);

            return new byte[] { (byte)value };
        }
    }
}