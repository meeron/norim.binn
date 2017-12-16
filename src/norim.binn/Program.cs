using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace norim.binn
{
    class Program
    {
        static void Main(string[] args)
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
            var count = 1000000;
            for (int i = 0; i < count; i++)
            {
                list.Add(new Person
                { 
                    Name = "Miron",
                    Id = Guid.NewGuid(),
                    DateOfBirth = new DateTime(1985, 4, 14),
                    Parent = new Person { Id = Guid.NewGuid(), Name = "Stanisław", DateOfBirth = new DateTime(1956, 11, 4) }
                });                
            }

            Console.WriteLine("Serializing BINN...");


            var sw = new Stopwatch();
            Serializer.RegisterType<Person>();
            
            sw.Start();
            var data = Serializer.Serialize(list);
            sw.Stop();

            Console.WriteLine($"BINN: Count={count}, Duration={sw.ElapsedMilliseconds}ms, Size={data.Length}");
            //Console.WriteLine($"Duration={sw.ElapsedMilliseconds}ms, Size={data.Length}, Bytes={Encoding.UTF8.GetString(data)}, Base64={Convert.ToBase64String(data)}");

            Console.WriteLine("Serializing JSON...");

            var sw1 = new Stopwatch();
            sw1.Start();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
            sw1.Stop();

            Console.WriteLine($"JSON: Count={count}, Duration={sw1.ElapsedMilliseconds}ms, Size={json.Length}");

        }
    }

    public class Person
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public int Age => (int)((DateTime.Now - DateOfBirth).TotalDays / 365);

        public DateTime DateOfBirth { get; set; }

        public Person Parent { get; set; }
    }
}
