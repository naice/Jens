using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jens.RestServer.Sample
{
    public class RestServerHostedService : IHostedService, IDisposable
    {
        private readonly RestServer _restServer;
        private readonly RestServerHostedServiceConfiguration _config;
        private readonly RestServerContainer _container;
        private readonly CancellationTokenSource _tokenSource;
        private readonly ILogger<RestServerHostedService> _logger;

        private Task _serverTask;
        public RestServerHostedService(IOptions<RestServerHostedServiceConfiguration> configuration, ILogger<RestServerHostedService> logger)
        {
            _tokenSource = new CancellationTokenSource();
            _config = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var loggerAdapter = new ILoggerAdapter(_logger);
            // initialize inversion of control container.
            _container = new RestServerContainer()
                .WithSingleton(_config)
                .WithSingleton<AspectOrientedAuthorizationAttributeInterceptor>()
                as RestServerContainer;

            // initialize simple rest server with config defaults.
            _restServer = new RestServer(
                _tokenSource.Token,
                new System.Net.IPEndPoint(System.Net.IPAddress.Any, _config.Port),
                _container,
                loggerAdapter,
                Assembly.GetAssembly(typeof(RestServerHostedService))); 
            // register public file route
            _restServer.RegisterRouteHandler(new RestServerServiceFileRouteHandler("InetPub"));
        }

        public void Dispose()
        {
            if (_container != null)
            {
                _tokenSource.Dispose();
                _container.Dispose();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => {
                _serverTask = _restServer.Launch();
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => {
                _tokenSource.Cancel();
            }, cancellationToken);
        }
    }
}
