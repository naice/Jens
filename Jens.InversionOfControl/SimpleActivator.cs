using System;
using System.Linq;
using System.Reflection;

namespace Jens.InversionOfControl
{
    public static class SimpleActivator
    {
        public static object Activate(Type type, IDependencyResolver dependecyResolver)
        {
            if (dependecyResolver == null)
                throw new ArgumentNullException(nameof(dependecyResolver));

            var constuctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

            foreach (var constructor in constuctors.OrderByDescending(constructor => constructor.GetParameters().Length))
            {
                var parameters = constructor.GetParameters();
                if (parameters == null || parameters.Length == 0)
                {
                    // default constructor
                    return constructor.Invoke(new object[0]);
                }
                
                if (parameters.Any(param => param.ParameterType == type))
                {
                    throw new InvalidOperationException($"Recursion on dependecy detected, {type.FullName}.");
                }

                var ctorTypes = parameters.Select(param => param.ParameterType).ToArray();
                if (!dependecyResolver.AreTypesKnown(ctorTypes))
                    continue;

                // resolve dependencys
                var dependencys = dependecyResolver.GetDependencies(ctorTypes);

                return constructor.Invoke(dependencys) ?? throw new InvalidOperationException($"Could not activate {type.FullName}");
            }

            throw new InvalidOperationException(
                $"{nameof(SimpleActivator)}: Could not find a matching constructor for " +
                $"{type.FullName}. Either provide a parameterless constructor or provide " +
                $"the correct dependencys via {nameof(dependecyResolver)}.");
        }
    }
}
