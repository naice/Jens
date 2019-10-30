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

        public static InterfaceType Create<InterfaceType>(IInterceptor[] interceptors)
        {
            object proxy = Create<InterfaceType, ProxyGenerator>();
            ((ProxyGenerator)proxy).SetParams(proxy, interceptors);
            return (InterfaceType)proxy;
        }
        /// <summary>
        /// <see cref="Create(object, Type, IInterceptor[])"/>
        /// </summary>
        public static InterfaceType Create<InterfaceType>(IInterceptor interceptor)
        {
            return Create<InterfaceType>(new IInterceptor[] { interceptor });
        }

        //[DebuggerStepThrough]
        //[DebuggerHidden]
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            try
            {
                var invocation = new Invocation(targetMethod, _instance, args);
                foreach (var interceptor in _interceptors)
                {
                    interceptor.Intercept(invocation);
                }

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
