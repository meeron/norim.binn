using System;

namespace norim.binn
{
    public interface IProperty
    {
        object GetValue(object instance);

        bool CanWrite { get; }

        bool CanRead { get; }

        string Name { get; }

        Type DeclaringType { get; }
    }
}