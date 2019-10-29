using System;

namespace Jens.InversionOfControl
{
    /// <summary>
    /// Marks the given class for interception. A proxy is created intercepting every call to the created instance. 
    /// Will be tested for - during activation in <see cref="SimpleActivator"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class InterceptAttribute : Attribute
    {
        /// <summary>
        /// <see cref="IInterceptor"/> types to apply when creating instance. Has to be assignable to <see cref="IInterceptor"/>.
        /// </summary>
        public Type[] Interceptors { get; }

        /// <summary>
        /// The <see cref="interface"/> the interception proxy is made for.
        /// </summary>
        public Type InterfaceType { get; }

        /// <summary>
        /// Creates and instance of the <see cref="InterceptAttribute"/> class.
        /// </summary>
        /// <param name="interfaceType">The <see cref="interface"/> the interception proxy is made for.</param>
        /// <param name="interceptors"><see cref="IInterceptor"/> types to apply when creating instance. Has to be assignable to <see cref="IInterceptor"/>.</param>
        public InterceptAttribute(Type interfaceType, params Type[] interceptors)
        {
            // sanity checks
            InterfaceType = interfaceType ?? throw new ArgumentNullException(nameof(interfaceType));

            if (!interfaceType.IsInterface)
                throw new ArgumentException("Has to be an interface.", nameof(interfaceType));

            var iInterceptorType = typeof(IInterceptor);
            foreach (var interceptor in interceptors ?? throw new ArgumentNullException(nameof(interceptors)))
            {
                if (interceptor == null)
                    continue;

                if (!iInterceptorType.IsAssignableFrom(interceptor))
                    throw new ArgumentException($"{interceptor.FullName} does not inherit {nameof(IInterceptor)}.", nameof(interceptors));
            }
            Interceptors = interceptors;
        }
    }
}
