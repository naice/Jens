using Jens.InversionOfControl;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jens.RestServer.Sample
{
    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AuthorizationAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string[] _rights;

        // This is a positional argument
        public AuthorizationAttribute(params string[] rights)
        {
            _rights = rights;
        }

        public string[] Rights
        {
            get { return _rights; }
        }
    }

    public class UnauthorizedException : RestServerServiceCallException
    {
        public UnauthorizedException(string message) : base(401, "Unauthorized", message)
        {
            
        }
    }

    public class AspectOrientedAuthorizationAttributeInterceptor : AttributeInterceptor<AuthorizationAttribute>
    {
        private readonly IReadOnlyList<string> _givenRights;

        public AspectOrientedAuthorizationAttributeInterceptor()
        {
            _givenRights = new List<string>() {
                "TestRecht01",
                "TestRecht02",
                "TestRecht03",
            };
        }

        public override void Intercept(IInvocation invocation, AuthorizationAttribute attribute)
        {
            if (!HasRights(attribute.Rights))
                throw new UnauthorizedException();
        }
    }
}
