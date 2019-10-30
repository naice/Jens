using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Jens.InversionOfControl
{
    // TODO: Make use of DispatchProxy as Remoting namespace is discontinued for dot net core. 
    // https://github.com/dotnet/corefx/tree/master/src/System.Reflection.DispatchProxy/src/System/Reflection

    /// <summary>
    /// Generates a proxy for a given interface that routes to the given implementing instance. Interceptors will be called for each method call.
    /// </summary>
    public class ProxyGenerator : DispatchProxy
    {
        private object _instance;
        private IInterceptor[] _interceptors;

        private void SetParams(object instance, IInterceptor[] interceptors)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _interceptors = interceptors ?? throw new ArgumentNullException(nameof(interceptors));
        }

        public static object Create(Type interfaceType, object instance,  IInterceptor[] interceptors)
        {
            var genericCreateMethod = typeof(ProxyGenerator).GetMethod("CreateInternal", BindingFlags.NonPublic | BindingFlags.Static);
            var constructedMethod = genericCreateMethod.MakeGenericMethod(interfaceType);
            return constructedMethod.Invoke(null, new object[] { instance, interceptors });
        }

        private static InterfaceType CreateInternal<InterfaceType>(InterfaceType instance, IInterceptor[] interceptors)
        {
            object proxy = DispatchProxy.Create<InterfaceType, ProxyGenerator>();
            ((ProxyGenerator)proxy).SetParams(instance, interceptors);
            return (InterfaceType)proxy;
        }

        public static InterfaceType Create<InterfaceType>(InterfaceType instance, IInterceptor[] interceptors)
        {
            return CreateInternal(instance, interceptors);
        }

        public static InterfaceType Create<InterfaceType>(InterfaceType instance, IInterceptor interceptors)
        {
            return CreateInternal(instance, new IInterceptor[] { interceptors });
        }

        // Invoked via the exposed Interface-Proxy.
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            try
            {
                // Do interception of function execution. 
                var invocation = new Invocation(targetMethod, _instance, args);
                foreach (var interceptor in _interceptors)
                {
                    // Invoke interception.
                    interceptor.Intercept(invocation);
                }

                // Invoke "real" method. 
                return targetMethod.Invoke(_instance, args);
            }
            catch (Exception ex)
            { 
                if (ex is TargetInvocationException && ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw ex;
            }
        }
    }
}
