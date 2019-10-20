using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace System.Net.Http
{
    public interface IHttpHandler
    {
        void HandleContext(HttpListenerContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Listenes for Http requests.
    /// </summary>
    public sealed class HttpListener : IDisposable
    {
        private readonly TcpListenerAdapter _tcpListener;
        private readonly IHttpHandler _handler;
        private CancellationToken _cancellationToken;

        private HttpListener(IHttpHandler handler, CancellationToken cancellationToken)
        {
            _handler = handler;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListener"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        public HttpListener(IPAddress address, int port, IHttpHandler handler, CancellationToken cancellationToken) 
            : this(new IPEndPoint(address, port), handler, cancellationToken)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListener"/> class.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public HttpListener(IPEndPoint endpoint, IHttpHandler handler, CancellationToken cancellationToken) : this(handler, cancellationToken)
        {
            _tcpListener = new TcpListenerAdapter(endpoint, cancellationToken);
        }

        /// <summary>
        /// Gets a value indicating whether the HttpListener is running or not.
        /// </summary>
        public bool IsListening => !_cancellationToken.IsCancellationRequested;

        /// <summary>
        /// Gets the underlying Socket.
        /// </summary>
        public Socket Socket => _tcpListener.Socket;

        /// <summary>
        /// Gets the local endpoint on which the StartListenerAsync is running.
        /// </summary>
        public IPEndPoint LocalEndpoint
        {
            get;
        }

        /// <summary>
        /// Starts the StartListenerAsync.
        /// </summary>
        public Task GetListenerTask()
        {
            if (_disposedValue)
                throw new ObjectDisposedException("Object has been disposed.");

            if (_cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            return Task.Run(StartListenerAsync, _cancellationToken);
        }

        private async Task StartListenerAsync()
        {
            _tcpListener.Start();

            while (!_cancellationToken.IsCancellationRequested)
            {
                // Await request.
                var client = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                _cancellationToken.ThrowIfCancellationRequested();
                var request = new HttpListenerRequest(client, _cancellationToken);

                // Handle request in a separate thread.
                Task.Run(async () =>
                {
                    // Process request.
                    var response = new HttpListenerResponse(request, client);

                    try
                    {
                        await request.ProcessAsync();

                        response.Initialize();

                        _cancellationToken.ThrowIfCancellationRequested();

                        if (_handler == null)
                        {
                            // No Request handler exist. Respond with "Not Found".
                            response.NotFound();
                            response.Close();
                        }
                        else
                        {
                            // execute handler.
                            _handler.HandleContext(new HttpListenerContext(request, response), _cancellationToken);
                        }
                    }
                    catch (Exception)
                    {
                        response.CloseSocket();
                    }
                });
            }
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _tcpListener.Dispose();
                _disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed