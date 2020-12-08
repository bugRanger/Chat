namespace Chat.Server.Network
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using NLog;

    public class UdpProvider : INetworkProvider, INetworkСontroller, IDisposable
    {
        #region Constants

        private const int PACKET_SIZE = ushort.MaxValue;

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private readonly SocketFactory _socketFactory;

        private ISocket _listener;
        private CancellationTokenSource _cancellation;

        private bool _disposing;

        #endregion Fields

        #region Events

        public event PreparePacket PreparePacket;

        #endregion Events

        #region Constructors

        public UdpProvider(SocketFactory socketFactory)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _socketFactory = socketFactory;
        }

        ~UdpProvider()
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
            _listener = _socketFactory(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Bind(endPoint);

            _cancellation = new CancellationTokenSource();

            await ListenAsync(PreparePacket, _cancellation.Token);
        }

        public async Task ListenAsync(PreparePacket prepare, CancellationToken token)
        {
            token.Register(() => _listener?.Close());

            await Task.Run(() =>
            {
                var count = 0;
                var buffer = new byte[PACKET_SIZE];

                while (!token.IsCancellationRequested)
                {
                    int received = 0;
                    EndPoint endPoint = null;

                    try
                    {
                        received = _listener.ReceiveFrom(buffer, count, PACKET_SIZE - count, ref endPoint);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                        break;
                    }

                    if (received == 0)
                    {
                        break;
                    }

                    count += received;

                    int offset = 0;
                    int position = 0;

                    prepare?.Invoke((IPEndPoint)endPoint, buffer, ref offset, count);

                    while (offset > position)
                    {
                        var packet = new ArraySegment<byte>(buffer, position, offset - position);

                        if (_logger.IsTraceEnabled)
                        {
                            _logger.Trace("Received packet: " + BitConverter.ToString(packet.ToArray()));
                        }

                        position += offset;
                    }

                    count -= offset;
                    if (count > 0)
                    {
                        Buffer.BlockCopy(buffer, offset, buffer, 0, count);
                    }
                }
            },
            token);
        }

        public void Stop()
        {
            _listener?.Close();

            FreeToken();
            FreeSocket();
        }

        public void Send(IPEndPoint remote, byte[] bytes)
        {
            _listener.SendTo(bytes, 0, bytes.Length, remote);
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
