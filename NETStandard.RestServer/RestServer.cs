using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace NETStandard.RestServer
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
        
        public RestServer(IPEndPoint endPoint, IRestServerServiceDependencyResolver restServerDependencyResolver, params Assembly[] assemblys)
        {
            _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            _restServerRouteHandlers = new List<IRestServerRouteHandler>();

            if (assemblys != null || assemblys.Length != 0)
            {
                RegisterRouteHandler(
                    new RestServerServiceRouteHandler(endPoint, restServerDependencyResolver, assemblys));
            }
        }

        public RestServer RegisterRouteHandler(IRestServerRouteHandler handler)
        {
            if (IsRunning) throw new InvalidOperationException("Can't modify route handlers while running.");
            _restServerRouteHandlers.Add(handler);
            return this;
        }

        public void Start()
        {
            if (_httpListener == null)
            {
                Log.i($"Starting server on {_endPoint.ToString()}");
                _httpListener = new System.Net.Http.HttpListener(_endPoint, this);
                _httpListener.Start();

                return;
            }

            throw new InvalidOperationException("RestServerServer already running.");
        }
        public void Stop()
        {
            if (_httpListener != null)
            {
                _httpListener.Close();
                _httpListener.Dispose();
                _httpListener = null;
            }
        }
        
        private async void ProcessHttpRequest(System.Net.Http.HttpListenerContext context)
        {
            foreach (var routeHandler in RestServerRouteHandlers)
            {
                var isHandled = await routeHandler.HandleRouteAsync(context);

                if (isHandled)
                {
                    if (!context.Response.IsClosed)
                        context.Response.Close();
                    return;
                }
            }

            if (!context.Response.IsClosed)
            {
                context.Response.NotFound();
                context.Response.Close();
            }
        }

        public void Dispose()
        {
            if (_httpListener != null)
            {
                _httpListener.Close();
                _httpListener.Dispose();
                _httpListener = null;
            }
        }

        public void HandleContext(System.Net.Http.HttpListenerContext context)
        {
            Task.Factory.StartNew(() => ProcessHttpRequest(context));
        }
    }
}
