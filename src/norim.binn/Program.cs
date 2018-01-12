using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ProtoBuf;

namespace norim.binn
{
    class Program
    {
        static void Main(string[] args)
        {
            DeserializeTest();
        }

        static void SerializeTest()
        {
            var list = new List<Person>();
            // list.Add(null);
            // list.AddRange(new object[] { true, false });
            // list.AddRange(new object[] { 0, 1, -1, 2, -2, 4, -4, 6, -6 });
            // list.AddRange(new object[] { 0x10, -0x10, 0x20, -0x20, 0x40, -0x40 });
            // list.AddRange(new object[] { 0x80, -0x80, 0x100, -0x100, 0x200, -0x100 });
            // list.AddRange(new object[] { 0x1000, -0x1000, 0x10000, -0x10000 });
            // list.AddRange(new object[] { 0x20000, -0x20000, 0x40000, -0x40000 });
            // list.AddRange(new object[] { 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000 });
            // list.AddRange(new object[] { 10000000000, 100000000000, 1000000000000 });
            // list.AddRange(new object[] { -10, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000 });
            // list.AddRange(new object[] { -1000000000, -10000000000, -100000000000, -1000000000000 });
            // list.AddRange(new object[] { 1.1, 0.1, -0.02, 3.5F, -6.3F, 9.99m });
            // list.Add(new byte[] { 16, 123, 123 });
            // list.Add("hello binn!");
            // list.Add(Guid.NewGuid());
            // list.Add(DateTime.Now);

            // list.Add(new[] { "hello", "world"});
            // list.Add(new[] { 0, 1, -1, 2, -2, 4, -4, 6, -6 });
            // list.Add(new[] { 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000 });
            // list.Add(new[] { 1.1, 0.1, -0.02 });


            Console.WriteLine("Preparing...");
            var count = 100000;
            for (int i = 0; i < count; i++)
            {
                list.Add(new Person
                { 
                    Name = "Miron",
                    Age = 32,
                    Id = Guid.NewGuid(),
                    DateOfBirth = new DateTime(1985, 4, 14),
                    Parent = new Person { Id = Guid.NewGuid(), Name = "Stanisław", DateOfBirth = new DateTime(1956, 11, 4), Age = 56 }
                });                
            }

            Console.WriteLine("Serializing BINN...");


            var sw = new Stopwatch();
            Serializer.RegisterType<Person>();       
            sw.Start();
            var data = Serializer.Serialize(list);
            sw.Stop();
            Console.WriteLine($"BINN: Count={count}, Duration={sw.ElapsedMilliseconds}ms, Size={data.Length}");

            Console.WriteLine("Serializing JSON...");
            var sw1 = new Stopwatch();
            sw1.Start();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
            sw1.Stop();
            Console.WriteLine($"JSON: Count={count}, Duration={sw1.ElapsedMilliseconds}ms, Size={json.Length}");

            Console.WriteLine("Serializing ProtoBuffer...");
            var sw2 = new Stopwatch();
            sw2.Start();
            var serializer = new ProtoBuffer.SimpleSerializer();
            var protoData = serializer.ToByteArray(list);
            sw2.Stop();
            Console.WriteLine($"ProtoBuffer: Count={count}, Duration={sw2.ElapsedMilliseconds}ms, Size={protoData.Length}");
        }

        static void DeserializeTest()
        {
            DeserializeTest(null);
            DeserializeTest("This is test");
            DeserializeTest(true);
            DeserializeTest(false);
            DeserializeTest(Guid.NewGuid());
            DeserializeTest(new DateTime(1985, 4, 14, 15, 0, 0));
            DeserializeTest(1);
            DeserializeTest(-1);
            DeserializeTest(346);
            DeserializeTest(-346);
            DeserializeTest(new int[] { 0, 1, -1, 2, -2, 4, -4, 6, -6 });            
        }

        static void DeserializeTest(object value)
        {
            var result = Deserializer.Deserialize(Serializer.Serialize(value));

            if (value == null)
                Log("null", string.Empty, result == null);
            else
                Log(value.GetType().Name, value.ToString(), value.Equals(result));              
        }

        static void Log(string type, string value, bool result)
        {
            Console.Write($"{type}={value} ");
            Console.ForegroundColor = result ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"{result}\n");
            Console.ResetColor();
        }
    }

    [ProtoContract]
    public class Person
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }

        [ProtoMember(3)]
        public int Age { get; set; }

        [ProtoMember(4)]
        public DateTime DateOfBirth { get; set; }

        [ProtoMember(5)]
        public Person Parent { get; set; }
    }
}
