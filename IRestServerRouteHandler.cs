using System.Net.Http;
using System.Threading.Tasks;

namespace NETStandard.RestServer
{
    public interface IRestServerRouteHandler
    {
        /// <summary>
        /// Handles a route. If a request is not closed must return false! Return true, if request is handled, 
        /// otherwise other handlers would process your context.
        /// </summary>
        Task<bool> HandleRouteAsync(HttpListenerContext context);
    }
}
