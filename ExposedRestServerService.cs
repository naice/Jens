using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace NETStandard.RestServer
{
    internal class ExposedRestServerService
    {
        public Type ServiceType { get; set; }
        public RestServerServiceInstanceType InstanceType { get; set; } = RestServerServiceInstanceType.Instance;
        public RestServerService SingletonInstance { get; set; }
        public List<ExposedRestServerAction> Routes { get; set; } = new List<ExposedRestServerAction>();

        private readonly IRestServerServiceDependencyResolver _RestServerDependencyResolver;

        public ExposedRestServerService(IRestServerServiceDependencyResolver RestServerDependencyResolver)
        {
            _RestServerDependencyResolver = RestServerDependencyResolver;
        }

        public object GetInstance(HttpListenerContext context)
        {
            if (InstanceType == RestServerServiceInstanceType.Instance)
            {
                return ApplyContext(InternalCreateInstance(), context);
            }

            if (InstanceType == RestServerServiceInstanceType.SingletonStrict ||
                InstanceType == RestServerServiceInstanceType.SingletonLazy)
            {
                if (SingletonInstance == null)
                {
                    SingletonInstance = InternalCreateInstance();
                }

                return ApplyContext(SingletonInstance, context);
            }

            throw new NotImplementedException($"{nameof(ExposedRestServerService)}: InstanceType not implemented. {InstanceType}");
        }

        private RestServerService InternalCreateInstance()
        {
            var activator = new SimpleActivator(); // TODO: Extract dependency!!

            return activator.ActivateRestServerService(ServiceType, _RestServerDependencyResolver);
        }

        private static RestServerService ApplyContext(RestServerService RestServerService, HttpListenerContext context)
        {
            if (RestServerService == null)
            {
                throw new ArgumentNullException(nameof(RestServerService));
            }

            RestServerService.Request = context?.Request;
            RestServerService.Response = context?.Response;

            return RestServerService;
        }
    }
}

