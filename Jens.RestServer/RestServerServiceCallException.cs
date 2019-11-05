using System;
using System.Collections.Generic;
using System.Text;

namespace Jens.RestServer
{
    /// <summary>
    /// Will return the given http status code and reason phrase when thrown inside a rest server call
    /// </summary>
    public class RestServerServiceCallException : Exception
    {
        public int HttpStatusCode { get; set; }
        public string HttpReasonPhrase { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpStatusCode">HTTP-StatusCode.</param>
        /// <param name="httpReasonPhrase">A reason phrase.</param>
        /// <param name="message">Exception Message.</param>
        public RestServerServiceCallException(int httpStatusCode, string httpReasonPhrase, string message) : base(message)
        {
            HttpReasonPhrase = httpReasonPhrase;
            HttpStatusCode = httpStatusCode;
        }
    }
}
