using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jens.RestServer
{
    /// <summary>
    /// Base class for Services.
    /// </summary>
    public interface IRestServerService
    {
        /// <summary>
        /// The current request. Only safe to access while inside a <see cref="RestServerServiceCallAttribute"/> function.
        /// </summary>
        HttpListenerRequest Request { get; set; }
        /// <summary>
        /// The current response. Only safe to access while inside a <see cref="RestServerServiceCallAttribute"/> function.
        /// </summary>
        HttpListenerResponse Response { get; set; }
    }
}
