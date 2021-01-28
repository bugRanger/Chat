namespace Chat.Client.Network
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    
    public class EasySocket : INetworkStream
    {
        #region Fields

        private readonly Func<Socket> _factory;

        private Socket _socket;
        private CancellationTokenSource _cancellation;

        #endregion Fields

        #region Events

        public event PreparePacket PreparePacket;

        #endregion Events

        #region Properties

        public IPEndPoint Local => (IPEndPoint)_socket?.LocalEndPoint;

        #endregion Properties

        #region Constructors

        public EasySocket(Func<Socket> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        #endregion Constructors

        #region Methods

        public void Connection(IPAddress address, int port)
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
                            _socket.Connect(address, port);

                            int remain = 0;
                            int received = 0;

                            var buffer = new byte[ushort.MaxValue];

                            while (!_cancellation.Token.IsCancellationRequested)
                            {
                                received = _socket.Receive(buffer, remain, buffer.Length - remain, SocketFlags.None);

                                if (received == 0)
                                {
                                    break;
                                }

                                remain += received;
                                int offset = 0;

                                PreparePacket?.Invoke(buffer, ref offset, remain);

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
                        await Task.Delay(1500);
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
            _socket?.Send(bytes);
        }

        #endregion Methods
    }
}
