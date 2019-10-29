using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace Jens.InversionOfControl
{
    // TODO: Make use of DispatchProxy as Remoting namespace is discontinued for dot net core. 
    // https://github.com/dotnet/corefx/tree/master/src/System.Reflection.DispatchProxy/src/System/Reflection

    /// <summary>
    /// Generates a proxy for a given interface that routes to the given implementing instance. Interceptors will be called for each method call.
    /// </summary>
    public class ProxyGenerator : RealProxy
    {
        private readonly object _instance;
        private readonly IInterceptor[] _interceptors;

        private ProxyGenerator(object instance, Type interfaceType, IInterceptor[] interceptors)
            : base(interfaceType)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _interceptors = interceptors ?? throw new ArgumentNullException(nameof(interceptors));

            if (!interfaceType.IsAssignableFrom(instance.GetType()))
                throw new ArgumentException($"{instance.GetType().FullName} does not implement {interfaceType.FullName}");
        }

        /// <summary>
        /// Creates a interface proxy.
        /// </summary>
        /// <param name="instance">The target implementing instance for the proxy.</param>
        /// <param name="interfaceType">The interface <see cref="Type"/> to generate the proxy for.</param>
        /// <param name="interceptors">Array of <see cref="IInterceptor"/>'s</param>
        /// <returns>Proxy for the given interface targeting the given instance.</returns>
        public static object Create(object instance, Type interfaceType, IInterceptor[] interceptors)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"{interfaceType.FullName} is not an interface.");

            return new ProxyGenerator(instance, interfaceType, interceptors).GetTransparentProxy();
        }
        /// <summary>
        /// Creates a interface proxy.
        /// </summary>
        /// <param name="instance">The target implementing instance for the proxy.</param>
        /// <param name="interfaceType">The interface <see cref="Type"/> to generate the proxy for.</param>
        /// <param name="interceptors"><see cref="IInterceptor"/> to bind.</param>
        /// <returns>Proxy for the given interface targeting the given instance.</returns>
        public static object Create(object instance, Type interfaceType, IInterceptor interceptor)
        {
            return Create(instance, interfaceType, new IInterceptor[] { interceptor });
        }
        /// <summary>
        /// <see cref="Create(object, Type, IInterceptor)"/>
        /// </summary>
        public static InterfaceType Create<InterfaceType>(InterfaceType instance, IInterceptor[] interceptors)
        {
            var interfaceType = typeof(InterfaceType);

            return (InterfaceType)Create(instance, interfaceType, interceptors);
        }
        /// <summary>
        /// <see cref="Create(object, Type, IInterceptor[])"/>
        /// </summary>
        public static InterfaceType Create<InterfaceType>(InterfaceType instance, IInterceptor interceptor)
        {
            return Create(instance, new IInterceptor[] { interceptor });
        }

        //[DebuggerStepThrough]
        //[DebuggerHidden]
        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = (IMethodCallMessage)msg;
            var method = (MethodInfo)methodCall.MethodBase;

            try
            {
                var invocation = new Invocation(method, _instance, methodCall.InArgs);
                foreach (var interceptor in _interceptors)
                {
                    interceptor.Intercept(invocation);
                }

                var result = method.Invoke(_instance, methodCall.InArgs);
                return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException && ex.InnerException != null)
                {
                    return new ReturnMessage(ex.InnerException, msg as IMethodCallMessage);
                }

                return new ReturnMessage(ex, msg as IMethodCallMessage);
            }
        }
    }
}
