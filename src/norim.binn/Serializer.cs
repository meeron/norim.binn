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

        private int SerializeInternal(object value)
        {
            if (value == null)
                return WriteNull();

            if (value is bool)
                return Write((bool)value);

            if (value is string)
                return Write((string)value);

            if (value is int)
                return Write((int)value);

            if (value is long)
                return Write((long)value);

            if (value is double)
                return Write((double)value);

            if (value is Single)
                return Write((Single)value);

            if (value is decimal)
                return Write((decimal)value);

            if(value is byte[])
                return Write((byte[])value);

            if (value is IEnumerable)
                return Write((IEnumerable)value);

            if (value is Guid)
                return Write((Guid)value);

            if (value is DateTime)
                return Write((DateTime)value);

            if (value.GetType().IsClass)
                return WriteClass(value);

            return 0;
        }

        private int WriteNull()
        {
            _mem.WriteByte(Types.Null);

            return 1;
        }

        private int Write(bool value)
        {
            _mem.WriteByte(value ? Types.True : Types.False);

            return 1;
        }

        private int Write(int value)
        {
            if (value >= 0)
                return Write((uint)value);

            if (value >= sbyte.MinValue)
                return Write((sbyte)value);

            if (value >= short.MinValue)
                return Write((short)value);

            _mem.WriteByte(Types.Int32);
            _mem.Write(BitConverter.GetBytes(value), 0, sizeof(int));

            return 1 + sizeof(int);
        }

        private int Write(sbyte value)
        {
            _mem.WriteByte(Types.Int8);
            _mem.WriteByte((byte)value);

            return 2;
        }

        private int Write(uint value)
        {
            if (value <= byte.MaxValue)
                return Write((byte)value);

            if (value <= ushort.MaxValue)
                return Write((ushort)value);
                
            _mem.WriteByte(Types.UInt32);
            _mem.Write(BitConverter.GetBytes(value), 0, sizeof(uint));

            return 1 + sizeof(uint);
        }

        private int Write(byte value)
        {
            _mem.WriteByte(Types.UInt8);
            _mem.WriteByte(value);

            return 2;
        }

        private int Write(ushort value)
        {
            _mem.WriteByte(Types.UInt16);
            _mem.Write(BitConverter.GetBytes(value), 0, sizeof(ushort));

            return 1 + sizeof(short);
        }

        private int Write(short value)
        {
            _mem.WriteByte(Types.Int16);
            _mem.Write(BitConverter.GetBytes(value), 0, sizeof(short));

            return 1 + sizeof(short);
        }

        private int Write(string value)
        {
            var valueData = Encoding.UTF8.GetBytes(value);
            var varintData = ToVarint(valueData.Length);

            _mem.WriteByte(Types.String);
            _mem.Write(varintData, 0, varintData.Length);
            _mem.Write(valueData, 0, value.Length);

            return 1 + varintData.Length + valueData.Length;
        }

        private int Write(long value)
        {
            if (value >= 0)
                return Write((ulong)value);

            _mem.WriteByte(Types.Int64);
            _mem.Write(BitConverter.GetBytes(value), 0, sizeof(long));

            return 1 + sizeof(long);
        }

        private int Write(ulong value)
        {
            _mem.WriteByte(Types.UInt64);
            _mem.Write(BitConverter.GetBytes(value), 0, sizeof(ulong));

            return 1 + sizeof(ulong);
        }

        private int Write(double value)
        {
            _mem.WriteByte(Types.Float64);
            _mem.Write(BitConverter.GetBytes(value), 0, sizeof(double));

            return 1 + sizeof(double);        
        }

        private int Write(decimal value)
        {
            return Write((double)value);
        }

        private int Write(byte[] value)
        {
            _mem.WriteByte(Types.Blob);
            _mem.Write(BitConverter.GetBytes(value.Length), 0, sizeof(int));
            _mem.Write(value, 0, value.Length);

            return 1 + sizeof(int) + value.Length;
        }

        private int Write(IEnumerable value)
        {
            var enumerator = value.GetEnumerator();
            var count = 0;

            using(var buffer = new MemoryStream())
            {
                using(var serializer = new Serializer())
                {
                    while(enumerator.MoveNext())
                    {
                        var data = serializer.Serialize(enumerator.Current);
                        buffer.Write(data, 0, data.Length);

                        count++;
                    }                   
                }

                var bufferVarintData = ToVarint((int)buffer.Length + 3);
                var countVaringData = ToVarint(count);

                _mem.WriteByte(Types.List);
                _mem.Write(bufferVarintData, 0, bufferVarintData.Length);
                _mem.Write(countVaringData, 0 , countVaringData.Length);
                
                buffer.Seek(0, SeekOrigin.Begin);
                buffer.CopyTo(_mem);

                return 1 + bufferVarintData.Length + countVaringData.Length + (int)buffer.Length;             
            }
        }

        public int Write(Guid value)
        {
            var data = value.ToByteArray();

            _mem.WriteByte(Types.Guid);
            _mem.Write(data, 0, data.Length);

            return 1 + data.Length;
        }

        public int Write(DateTime value)
        {
            var data = BitConverter.GetBytes(value.Ticks);

            _mem.WriteByte(Types.DateTime);
            _mem.Write(data, 0, data.Length);

            return 1 + data.Length;
        }

        private int WriteClass(object value)
        {
            var type = value.GetType();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            using(var buffer = new MemoryStream())
            {
                using(var serializer = new Serializer())
                {
                    foreach (var prop in properties)
                    {
                        if (!prop.CanRead || !prop.CanWrite)
                            continue;

                        if (prop.Name.Length > 255)
                            throw new OverflowException("Property name should have max length 255.");

                        var data = serializer.Serialize(prop.GetValue(value));

                        buffer.WriteByte((byte)prop.Name.Length);
                        buffer.Write(Encoding.UTF8.GetBytes(prop.Name), 0, prop.Name.Length);
                        buffer.Write(data, 0, data.Length);
                    }

                    var bufferVarintData = ToVarint((int)buffer.Length + 3);
                    var countVaringData = ToVarint(properties.Length);

                    _mem.WriteByte(Types.Object);
                    _mem.Write(bufferVarintData, 0, bufferVarintData.Length);
                    _mem.Write(countVaringData, 0 , countVaringData.Length);
                    
                    buffer.Seek(0, SeekOrigin.Begin);
                    buffer.CopyTo(_mem);

                    return 1 + bufferVarintData.Length + countVaringData.Length + (int)buffer.Length;  
                }
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