using System;

namespace Jens.InversionOfControl
{
    public interface IDependencyResolver
    {
        object[] GetDependencies(Type[] types);

        object GetDependency(Type type);

        Type GetDependency<Type>()
            where Type : class;

        Type Activate<Type>()
            where Type : class;

        object Activate(Type type);

        bool AreTypesKnown(Type[] types);
    }
}
