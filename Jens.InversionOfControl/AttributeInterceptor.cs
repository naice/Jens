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
            var attribute = invocation.TargetMethod.GetCustomAttribute(typeof(Attr));
            if (attribute == null)
                return;

            Intercept(invocation, (Attr)attribute);
        }

        public abstract void Intercept(IInvocation invocation, Attr attribute);
    }
}
