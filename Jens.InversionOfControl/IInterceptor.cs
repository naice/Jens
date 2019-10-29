namespace Jens.InversionOfControl
{
    /// <summary>
    /// Intercepts an invocation
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// Handle invocation interception here.
        /// </summary>
        /// <param name="invocation">The invocation.</param>
        void Intercept(IInvocation invocation);
    }
}
