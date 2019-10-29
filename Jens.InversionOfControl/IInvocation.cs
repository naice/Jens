using System.Reflection;

namespace Jens.InversionOfControl
{
    public interface IInvocation
    {
        /// <summary>
        /// Target method name.
        /// </summary>
        MethodInfo TargetMethod { get; }

        /// <summary>
        /// Target of the invocation.
        /// </summary>
        object Target { get; }

        /// <summary>
        /// The parameters for this call.
        /// </summary>
        object[] Parameters { get; }
    }
}
