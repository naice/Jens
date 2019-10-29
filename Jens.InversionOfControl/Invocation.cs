using System.Reflection;

namespace Jens.InversionOfControl
{
    public class Invocation : IInvocation
    {
        public MethodInfo TargetMethod { get; }
        public object Target { get; }
        public object[] Parameters { get; }

        public Invocation(MethodInfo methodInfo, object target, object[] parameters)
        {
            TargetMethod = methodInfo;
            Target = target;
            Parameters = parameters;
        }
    }
}
