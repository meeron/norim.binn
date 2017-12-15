using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

namespace norim.binn
{
    public sealed class Serializer : IDisposable
    {
        private readonly MemoryStream _mem;

        public Serializer()
        {
            _mem = new MemoryStream();
        }

        public byte[] Serialize(object value)
        {
            _mem.Seek(0, SeekOrigin.Begin);
            _mem.SetLength(0);

            if (SerializeInternal(value) == 0)
                throw new NotSupportedException($"Type '{value.GetType()}' is not supported.");

            return _mem.ToArray();
        }

        public void Dispose()
        {
            _mem.Dispose();
        }

        private int SerializeInternal(object value, Stream buffer = null)
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

        private int WriteNull(Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            buffer.WriteByte(Types.Null);

            return 1;
        }

        private int Write(bool value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            buffer.WriteByte(value ? Types.True : Types.False);

            return 1;
        }

        private int Write(int value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            if (value >= 0)
                return Write((uint)value);

            if (value >= sbyte.MinValue)
                return Write((sbyte)value);

            if (value >= short.MinValue)
                return Write((short)value);

            buffer.WriteByte(Types.Int32);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(int));

            return 1 + sizeof(int);
        }

        private int Write(sbyte value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            buffer.WriteByte(Types.Int8);
            buffer.WriteByte((byte)value);

            return 2;
        }

        private int Write(uint value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            if (value <= byte.MaxValue)
                return Write((byte)value);

            if (value <= ushort.MaxValue)
                return Write((ushort)value);
                
            buffer.WriteByte(Types.UInt32);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(uint));

            return 1 + sizeof(uint);
        }

        private int Write(byte value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            buffer.WriteByte(Types.UInt8);
            buffer.WriteByte(value);

            return 2;
        }

        private int Write(ushort value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            buffer.WriteByte(Types.UInt16);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(ushort));

            return 1 + sizeof(short);
        }

        private int Write(short value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            buffer.WriteByte(Types.Int16);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(short));

            return 1 + sizeof(short);
        }

        private int Write(string value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            var valueData = Encoding.UTF8.GetBytes(value);
            var varintData = ToVarint(valueData.Length);

            buffer.WriteByte(Types.String);
            buffer.Write(varintData, 0, varintData.Length);
            buffer.Write(valueData, 0, value.Length);

            return 1 + varintData.Length + valueData.Length;
        }

        private int Write(long value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            if (value >= 0)
                return Write((ulong)value);

            buffer.WriteByte(Types.Int64);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(long));

            return 1 + sizeof(long);
        }

        private int Write(ulong value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            buffer.WriteByte(Types.UInt64);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(ulong));

            return 1 + sizeof(ulong);
        }

        private int Write(double value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            buffer.WriteByte(Types.Float64);
            buffer.Write(BitConverter.GetBytes(value), 0, sizeof(double));

            return 1 + sizeof(double);        
        }

        private int Write(decimal value, Stream buffer = null)
        {
            return Write((double)value, buffer);
        }

        private int Write(byte[] value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            buffer.WriteByte(Types.Blob);
            buffer.Write(BitConverter.GetBytes(value.Length), 0, sizeof(int));
            buffer.Write(value, 0, value.Length);

            return 1 + sizeof(int) + value.Length;
        }

        private int Write(IEnumerable value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

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

        public int Write(Guid value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            var data = value.ToByteArray();

            buffer.WriteByte(Types.Guid);
            buffer.Write(data, 0, data.Length);

            return 1 + data.Length;
        }

        public int Write(DateTime value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            var data = BitConverter.GetBytes(value.Ticks);

            _mem.WriteByte(Types.DateTime);
            _mem.Write(data, 0, data.Length);

            return 1 + data.Length;
        }

        private int WriteClass(object value, Stream buffer = null)
        {
            if (buffer == null)
                buffer = _mem;

            var type = value.GetType();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

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

        private byte[] ToVarint(int value)
        {
            if (value > 127)
                return BitConverter.GetBytes(value | 0x80000000);

            return new byte[] { (byte)value };
        }
    }
}