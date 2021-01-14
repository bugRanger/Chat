namespace Chat.Server.Network
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    using NLog;

    public class TcpProvider : INetworkProvider, ITcpСontroller, IDisposable
    {
        #region Constants

        private const int INACTIVE_INTERVAL = 100;

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private readonly SocketFactory _socketFactory;
        private readonly ConcurrentDictionary<EndPoint, ITcpConnection> _connections;

        private ISocket _listener;
        private CancellationTokenSource _cancellation;

        private int _limit;
        private bool _disposing;

        #endregion Fields

        #region Events

        public event Action<IPEndPoint> ConnectionAccepted;
        public event Action<IPEndPoint, bool> ConnectionClosing;
        public event PreparePacket PreparePacket;

        #endregion Events

        #region Constructors

        public TcpProvider(SocketFactory socketFactory, int limit = 1)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _socketFactory = socketFactory;
            _limit = limit;

            _connections = new ConcurrentDictionary<EndPoint, ITcpConnection>();
        }

        ~TcpProvider()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Constructors

        #region Methods

        public void Start(IPEndPoint endPoint)
        {
            _ = StartAsync(endPoint);
        }

        public async Task StartAsync(IPEndPoint endPoint)
        {
            _listener = _socketFactory(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(endPoint);

            _cancellation = new CancellationTokenSource();

            await ListenAsync(_cancellation.Token);
        }

        public async Task ListenAsync(CancellationToken token)
        {
            token.Register(() => _listener?.Close());

            await Task.Run(async () =>
            {
                _listener.Listen(_limit);

                while (!token.IsCancellationRequested)
                {
                    ISocket socket = null;
                    try
                    {
                        socket = _listener.Accept();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                        break;
                    }

                    TcpConnection client = null;
                    try
                    {
                        if (!_connections.TryAdd(socket.RemoteEndPoint, client = new TcpConnection(socket)))
                            continue;

                        client.Closing += Client_Closing;

                        _ = client.ListenAsync(PreparePacket, token);

                        ConnectionAccepted?.Invoke(client.RemoteEndPoint);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }

                    token.ThrowIfCancellationRequested();
                    await Task.Delay(INACTIVE_INTERVAL);
                }
            });
        }

        private void Client_Closing(ITcpConnection client, bool inactive)
        {
            client.Closing -= Client_Closing;

            _connections.TryRemove(client.RemoteEndPoint, out _);
            ConnectionClosing?.Invoke(client.RemoteEndPoint, inactive);
        }

        public void Stop()
        {
            FreeToken();
            FreeSocket();
        }

        public void Send(IPEndPoint remote, ArraySegment<byte> bytes)
        {
            if (!_connections.TryGetValue(remote, out ITcpConnection connection))
            {
                _logger?.Warn($"Send error, connection not found - {remote}");
                return;
            }

            connection.Send(bytes);
        }

        public void Disconnect(IPEndPoint remote, bool inactive)
        {
            if (!_connections.TryGetValue(remote, out ITcpConnection connection))
            {
                _logger?.Warn($"Disconnet error, connection not found - {remote}");
                return;
            }

            connection.Disconnect(inactive);
        }

        private void FreeToken()
        {
            var cancellation = _cancellation;
            if (cancellation == null)
                return;

            _cancellation = null;

            cancellation.Cancel();
            cancellation.Dispose();
        }

        private void FreeSocket()
        {
            var socket = _listener;
            if (socket == null)
                return;

            _listener = null;

            socket.Close();
            socket.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposing)
                return;

            if (disposing)
            {
                FreeToken();
                FreeSocket();
            }

            _disposing = true;
        }

        #endregion Methods
    }
}
