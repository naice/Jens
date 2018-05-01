using NETStandard.RestServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer
{
    [RestServerServiceInstance(RestServerServiceInstanceType.Instance)]
    internal class SampleApiService : RestServerService
    {
        private readonly Configuration _config;

        public SampleApiService(Configuration configuration)
        {
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [RestServerServiceCall("/sampleapi/getconfiguration")]
        public Configuration GetConfiguration()
        {
            return _config;
        }
    }
}
