using Jens.InversionOfControl;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Jens.RestServer.Sample
{
    /// <summary>
    /// Api Response object
    /// </summary>
    public class AspectOrientedAuthorizationApiTestResult 
    {
        public string Name { get; set; }
    }
    /// <summary>
    /// Api Request object
    /// </summary>
    public class AspectOrientedAuthorizationApiTestRequest 
    {
        public string Name { get; set; }
    }

    /// <summary>
    /// Define interface for the interception. 
    /// </summary>
    public interface IAspectOrientedAuthorizationApiTest : IRestServerService
    {
        AspectOrientedAuthorizationApiTestResult Allowed_01(AspectOrientedAuthorizationApiTestRequest request);
        AspectOrientedAuthorizationApiTestResult Allowed_02(AspectOrientedAuthorizationApiTestRequest request);
        AspectOrientedAuthorizationApiTestResult Allowed_03(AspectOrientedAuthorizationApiTestRequest request);
        AspectOrientedAuthorizationApiTestResult Deny_01(AspectOrientedAuthorizationApiTestRequest request);
    }

    /// <summary>
    /// Implement API service using <see cref="IAspectOrientedAuthorizationApiTest"/> interface, and mark as interceptable. 
    /// </summary>
    [RestServerServiceInstance(RestServerServiceInstanceType.Instance)]
    [Intercept(typeof(IAspectOrientedAuthorizationApiTest), typeof(AspectOrientedAuthorizationAttributeInterceptor))]
    public class AspectOrientedAuthorizationApiTest : IAspectOrientedAuthorizationApiTest
    {
        public HttpListenerRequest Request { get; set; }
        public HttpListenerResponse Response { get; set; }

        [Authorization("TestRight01")]
        [RestServerServiceCall("/auth/testright01")]
        public AspectOrientedAuthorizationApiTestResult Allowed_01(AspectOrientedAuthorizationApiTestRequest request)
            => new AspectOrientedAuthorizationApiTestResult() { Name = request?.Name };


        [Authorization("TestRight03")]
        [RestServerServiceCall("/auth/testright03")]
        public AspectOrientedAuthorizationApiTestResult Allowed_02(AspectOrientedAuthorizationApiTestRequest request)
            => new AspectOrientedAuthorizationApiTestResult() { Name = request?.Name };


        [Authorization("TestRight03", "TestRight02", "TestRight01")]
        [RestServerServiceCall("/auth/testright010203")]
        public AspectOrientedAuthorizationApiTestResult Allowed_03(AspectOrientedAuthorizationApiTestRequest request)
            => new AspectOrientedAuthorizationApiTestResult() { Name = request?.Name };


        [Authorization("TestRight04")]
        [RestServerServiceCall("/auth/testright04")]
        public AspectOrientedAuthorizationApiTestResult Deny_01(AspectOrientedAuthorizationApiTestRequest request)
            => new AspectOrientedAuthorizationApiTestResult() { Name = request?.Name };
    }
}
