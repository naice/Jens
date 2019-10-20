using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    class TcpListenerAdapter : IDisposable
    {
        private TcpListener _tcpListener;
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _cancellationTokenRegistration;

        public TcpListenerAdapter(IPEndPoint localEndpoint, CancellationToken cancellationToken)
        {
            LocalEndpoint = localEndpoint;
            _cancellationToken = cancellationToken;
            _cancellationTokenRegistration = _cancellationToken.Register(() => Stop());
            Initialize();
        }

        public IPEndPoint LocalEndpoint { get; private set; }

        public Task<TcpClientAdapter> AcceptTcpClientAsync()
        {
            return AcceptTcpClientInternalAsync();
        }

        private void Initialize()
        {
            _tcpListener = new TcpListener(LocalEndpoint);
        }

        private async Task<TcpClientAdapter> AcceptTcpClientInternalAsync()
        {
            var tcpClient = await _tcpListener.AcceptTcpClientAsync();
            return new TcpClientAdapter(tcpClient);
        }

        public void Start()
        {
            _tcpListener.Start();
        }

        public void Stop()
        {
            _tcpListener.Stop();
        }

        public void Dispose()
        {
            if (_cancellationTokenRegistration != null)
                _cancellationTokenRegistration.Dispose();
        }

        public Socket Socket => _tcpListener.Server;
    }
}