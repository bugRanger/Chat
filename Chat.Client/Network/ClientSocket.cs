namespace Chat.Client.Network
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Chat.Net.Socket;

    public class ClientSocket : INetworkStream
    {
        #region Constants

        private const int RECONNECT_INTERVAL = 1500;

        #endregion Constants

        #region Fields

        private readonly Func<ISocket> _factory;

        private ISocket _socket;
        private CancellationTokenSource _cancellation;

        #endregion Fields

        #region Events

        public event Received Received;

        #endregion Events

        #region Properties

        public IPEndPoint Local => (IPEndPoint)_socket?.LocalEndPoint;

        #endregion Properties

        #region Constructors

        public ClientSocket(Func<ISocket> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        #endregion Constructors

        #region Methods

        public void Connection(EndPoint remote)
        {
            if (_socket != null)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                _cancellation = new CancellationTokenSource();
                _cancellation.Token.Register(() => _socket?.Close());

                try
                {
                    while (!_cancellation.Token.IsCancellationRequested)
                    {
                        try
                        {
                            _socket = _factory();
                            _socket.Connect(remote);

                            int remain = 0;
                            int received = 0;

                            var buffer = new byte[ushort.MaxValue];

                            while (!_cancellation.Token.IsCancellationRequested)
                            {
                                received = _socket.Receive(buffer, remain, buffer.Length - remain);

                                if (received == 0)
                                {
                                    break;
                                }

                                remain += received;
                                int offset = 0;

                                Received?.Invoke(buffer, ref offset, remain);

                                remain -= offset;
                                if (remain > 0)
                                {
                                    Buffer.BlockCopy(buffer, offset, buffer, 0, remain);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        finally
                        {
                            _socket?.Dispose();
                            _socket = null;
                        }

                        _cancellation.Token.ThrowIfCancellationRequested();
                        await Task.Delay(RECONNECT_INTERVAL);
                    }
                }
                finally
                {
                    _cancellation.Dispose();
                    _cancellation = null;
                }
            });
        }

        public void Disconnect()
        {
            _cancellation?.Cancel();
        }

        public void Send(ArraySegment<byte> bytes)
        {
            _socket?.Send(bytes.Array, bytes.Offset, bytes.Count);
        }

        #endregion Methods
    }
}
