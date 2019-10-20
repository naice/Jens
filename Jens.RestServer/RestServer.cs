using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Jens.RestServer
{
    public partial class RestServer : IDisposable, IHttpHandler
    {
        private readonly IPEndPoint _endPoint;
        private readonly List<IRestServerRouteHandler> _restServerRouteHandlers;

        public bool IsRunning {
            get {
                return _httpListener?.IsListening ?? false;
            }
        }
        public IPEndPoint IPEndPoint
        {
            get
            {
                return new IPEndPoint(_endPoint.Address, _endPoint.Port);
            }
        }

        public IReadOnlyList<IRestServerRouteHandler> RestServerRouteHandlers => _restServerRouteHandlers;

        private System.Net.Http.HttpListener _httpListener;
        private IRestServerRouteHandler _restServerRoute404Handler;
        private readonly CancellationToken _cancellationToken;
        private readonly ILogger _logger;

        public RestServer(
            CancellationToken cancellationToken, 
            IPEndPoint endPoint, 
            IRestServerDependencyResolver restServerDependencyResolver, 
            params Assembly[] assemblys)
            : this(cancellationToken, endPoint, restServerDependencyResolver, new DefaultLogger(), assemblys)
        {
        }
        public RestServer(CancellationToken cancellationToken, IPEndPoint endPoint, IRestServerDependencyResolver restServerDependencyResolver, ILogger logger, params Assembly[] assemblys)
        {
            _cancellationToken = cancellationToken;
            _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            _restServerRouteHandlers = new List<IRestServerRouteHandler>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (assemblys != null || assemblys.Length != 0)
            {
                RegisterRouteHandler(
                    new RestServerServiceRouteHandler(endPoint, restServerDependencyResolver, _logger, assemblys));
            }
        }

        public RestServer RegisterRouteHandler(IRestServerRouteHandler handler)
        {
            if (IsRunning) throw new InvalidOperationException("Can't modify route handlers while running.");
            _restServerRouteHandlers.Add(handler);
            return this;
        }

        public RestServer With404Handler(IRestServerRouteHandler handler)
        {
            if (IsRunning) throw new InvalidOperationException("Can't modify route handlers while running.");
            _restServerRoute404Handler = handler;
            return this;
        }

        public Task Launch()
        {
            if (_cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            if (_httpListener == null)
            {
                _logger.Info($"Starting server on {_endPoint.ToString()}");
                _httpListener = new System.Net.Http.HttpListener(_endPoint, this, _cancellationToken);
                return _httpListener.GetListenerTask();
            }

            throw new InvalidOperationException("RestServerServer already running.");
        }
                
        private async void ProcessHttpRequest(System.Net.Http.HttpListenerContext context, CancellationToken cancellationToken)
        {
            bool isHandled = false;
            foreach (var routeHandler in RestServerRouteHandlers)
            {
                if (cancellationToken.IsCancellationRequested) break;
                isHandled = await routeHandler.HandleRouteAsync(context);

                if (isHandled)
                {
                    break;
                }
            }

            if (!isHandled && _restServerRoute404Handler != null)
            {
                isHandled = await _restServerRoute404Handler.HandleRouteAsync(context);
            }

            if (!context.Response.IsClosed)
            {
                if (!isHandled)
                    context.Response.NotFound();

                context.Response.Close();
            }
        }

        public void Dispose()
        {
            if (_httpListener != null)
            {
                _httpListener.Dispose();
                _httpListener = null;
            }
        }

        public void HandleContext(System.Net.Http.HttpListenerContext context, CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() => ProcessHttpRequest(context, cancellationToken));
        }
    }
}
