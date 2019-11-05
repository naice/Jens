using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Jens.RestServer.Sample
{
    internal class SampleApiFullBodyResponse
    {
        public string Body { get; set; }
    }

    internal class WildcardTest
    {
        public int WildcardInt { get; set; }
        public string WildcardString { get; set; }
        public string Body { get; set; }
    }

    [RestServerServiceInstance(RestServerServiceInstanceType.Instance)]
    internal class SampleApiService : IRestServerService
    {
        private readonly RestServerHostedServiceConfiguration _config;

        public HttpListenerRequest Request { get; set; }
        public HttpListenerResponse Response { get; set; }

        public SampleApiService(RestServerHostedServiceConfiguration configuration)
        {
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [RestServerServiceCall("/sampleapi/getconfiguration")]
        public RestServerHostedServiceConfiguration GetConfiguration()
        {
            return _config;
        }

        [RestServerServiceCall("/sampleapi/fullbody")]
        public SampleApiFullBodyResponse FullBody(string body)
        {
            return new SampleApiFullBodyResponse() { Body = body };
        }

        [RestServerServiceCall("/sampleapi/wildcard/{WildcardInt}/{WildcardString}")]
        public WildcardTest FullBody(WildcardTest wildcardTest)
        {
            return wildcardTest;
        }

        [RestServerServiceCall("/sampleapi/wildcardAndBody/{WildcardInt}/{WildcardString}")]
        public WildcardTest FullBody(WildcardTest wildcardTest, string body)
        {
            wildcardTest.Body = body;

            return wildcardTest;
        }
    }
}
