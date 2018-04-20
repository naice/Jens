using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NETStandard.RestServer
{
    public class SimpleActivator
    {
        public RestServerService ActivateRestServerService(Type serviceType, IRestServerServiceDependencyResolver dependecyResolver)
        {
            var service = EasyActivation(serviceType, dependecyResolver) as RestServerService;

            if (service == null)
            {
                throw new InvalidOperationException($"Could not activate {serviceType.FullName}.");
            }

            return service;
        }
        public object EasyActivation(Type type, IRestServerServiceDependencyResolver dependecyResolver)
        {
            var constuctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

            foreach (var constructor in constuctors.OrderByDescending(constructor => constructor.GetParameters().Length))
            {
                var parameters = constructor.GetParameters();
                if (parameters == null || parameters.Length == 0)
                {
                    // default constructor
                    return constructor.Invoke(new object[0]);
                }

                if (dependecyResolver != null)
                {
                    if (parameters.Any(param => param.ParameterType == type))
                    {
                        throw new InvalidOperationException($"Recursion on dependecy detected, {type.FullName}.");
                    }

                    // resolve dependencys
                    var dependencys = dependecyResolver.GetDependecys(parameters.Select(param => param.ParameterType).ToArray());
                    if (dependencys == null || parameters.Length != dependencys.Length)
                    {
                        // no dependecys found.
                        continue;
                    }

                    return constructor.Invoke(dependencys) ?? throw new InvalidOperationException($"Could not activate {type.FullName}");
                }
            }

            throw new InvalidOperationException(
                $"{nameof(SimpleActivator)}: Could not find a matching constructor for " +
                $"{type.FullName}. Either provide a parameterless constructor or provide " +
                $"the correct dependencys via {nameof(IRestServerServiceDependencyResolver)}.");
        }
    }
}
