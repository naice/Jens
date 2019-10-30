using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jens.InversionOfControl
{
    /// <summary>
    /// Intercepts an invocation when the calling method has the given attribute set.
    /// </summary>
    public abstract class AttributeInterceptor<Attr> : IInterceptor
        where Attr : Attribute
    {

        public void Intercept(IInvocation invocation)
        {
            var implementationType = invocation.Target.GetType();
            var interfaceMethod = invocation.TargetMethod;
            var implementationMethod = GetMethodImplementation(implementationType, interfaceMethod);

            if (!implementationMethod.CustomAttributes.Any())
                return;
            var attribute = implementationMethod.GetCustomAttribute(typeof(Attr));
            if (attribute == null)
                return;

            Intercept(invocation, (Attr)attribute);
        }
        private static MethodInfo GetMethodImplementation(Type implementationType, MethodInfo ifaceMethod)
        {
            InterfaceMapping ifaceMap = implementationType.GetInterfaceMap(ifaceMethod.DeclaringType);
            for (int i = 0; i < ifaceMap.InterfaceMethods.Length; i++)
            {
                if (ifaceMap.InterfaceMethods[i].Equals(ifaceMethod))
                    return ifaceMap.TargetMethods[i];
            }
            // We shouldn't get here
            throw new InvalidOperationException($"Implemented method ({ifaceMethod.Name}) missing from Interface ({ifaceMethod.DeclaringType.FullName}) on ({implementationType.FullName})"); 
            
        }

        public abstract void Intercept(IInvocation invocation, Attr attribute);
    }
}
