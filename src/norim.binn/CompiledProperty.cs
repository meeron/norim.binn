using System;
using System.Linq.Expressions;
using System.Reflection;

namespace norim.binn
{
    public class CompiledProperty<T> : IProperty
    {
        private readonly Func<T, object> _getter;

        public CompiledProperty(PropertyInfo propertyInfo)
        {
            _getter = BuildUntypedGetter(propertyInfo);
            CanWrite = propertyInfo.CanWrite;
            CanRead = propertyInfo.CanRead;
            Name = propertyInfo.Name;
            DeclaringType = propertyInfo.DeclaringType;
        }

        public object GetValue(object instance) => _getter((T)instance);

        public bool CanWrite { get; }

        public bool CanRead { get; }

        public string Name { get; }

        public Type DeclaringType { get; }

        // https://stackoverflow.com/questions/17660097/is-it-possible-to-speed-this-method-up/17669142#17669142
        private static Func<T, object> BuildUntypedGetter(PropertyInfo propertyInfo)
        {
            var targetType = propertyInfo.DeclaringType;
            var methodInfo = propertyInfo.GetGetMethod();

            var exTarget = Expression.Parameter(targetType, "t");
            var exBody = Expression.Call(exTarget, methodInfo);
            var exBody2 = Expression.Convert(exBody, typeof(object));

            var lambda = Expression.Lambda<Func<T, object>>(exBody2, exTarget);

            return lambda.Compile();
        }
    }    
}