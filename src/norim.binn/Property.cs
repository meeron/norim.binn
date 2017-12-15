using System;
using System.Reflection;

namespace norim.binn
{
    public class Property : IProperty
    {
        private readonly Func<object, object> _getter;

        public Property(PropertyInfo propertyInfo)
        {
            _getter = propertyInfo.GetValue;
            CanWrite = propertyInfo.CanWrite;
            CanRead = propertyInfo.CanRead;
            Name = propertyInfo.Name;
            DeclaringType = propertyInfo.DeclaringType;            
        }

        public bool CanWrite { get; }

        public bool CanRead { get; }

        public string Name { get; }

        public Type DeclaringType { get; }

        public object GetValue(object instance) => _getter(instance);
    }
}