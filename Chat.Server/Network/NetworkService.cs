﻿namespace Chat.Server.Network
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    using NLog;

    public class NetworkService : INetworkService, INetworkСontroller, IDisposable
    {
        #region Constants

        private const int INACTIVE_INTERVAL = 100;

        private const int ENABLED = 1;
        private const int DISABLE = 0;

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<EndPoint, IConnection> _connections;

        private Socket _listener;
        private CancellationTokenSource _cancelation;

        private int _active;
        private bool _disposing;

        #endregion Fields

        #region Events

        public event Action<IPEndPoint> ConnectionAccepted;
        public event Action<IPEndPoint, bool> ConnectionClosing;
        public event PreparePacket PreparePacket;

        #endregion Events

        #region Constructors

        public NetworkService()
        {
            _logger = LogManager.GetCurrentClassLogger();

            _active = DISABLE;
            _connections = new ConcurrentDictionary<EndPoint, IConnection>();
        }

        ~NetworkService()
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

        public void Start(IPEndPoint endPoint, int limit = 1)
        {
            _ = StartAsync(endPoint, limit);
        }

        public async Task StartAsync(IPEndPoint endPoint, int limit = 1)
        {
            if (Interlocked.CompareExchange(ref _active, ENABLED, DISABLE) == ENABLED)
                return;

            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(endPoint);

            _cancelation = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                _listener.Listen(limit);

                var token = _cancelation.Token;

                while (!token.IsCancellationRequested)
                {
                    NetworkConnection client = null;

                    try
                    {
                        Socket socket = _listener.Accept();

                        if (!_connections.TryAdd(socket.RemoteEndPoint, client = new NetworkConnection(socket)))
                            continue;

                        client.Closing += (s, inactive) =>
                        {
                            ConnectionClosing?.Invoke(client.RemoteEndPoint, inactive);
                            _connections.TryRemove(client.RemoteEndPoint, out _);
                        };

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

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _active, DISABLE, ENABLED) == DISABLE)
                return;

            FreeToken();

            _listener.Close();
        }

        public void Send(IPEndPoint remote, byte[] bytes)
        {
            if (!_connections.TryGetValue(remote, out IConnection connection))
            {
                _logger?.Warn($"Send error, connection not found - {remote}");
                return;
            }

            connection.Send(bytes);
        }

        public void Disconnect(IPEndPoint remote, bool inactive)
        {
            if (!_connections.TryGetValue(remote, out IConnection connection))
            {
                _logger?.Warn($"Disconnet error, connection not found - {remote}");
                return;
            }

            connection.Disconnect(inactive);
        }

        private void FreeToken()
        {
            var cancelation = _cancelation;
            if (cancelation == null)
                return;

            _cancelation = null;

            cancelation.Cancel();
            cancelation.Dispose();
        }

        protected void Dispose(bool disposing)
        {
            if (_disposing)
                return;

            if (disposing)
            {
                FreeToken();
            }

            _disposing = true;
        }

        #endregion Methods
    }
}