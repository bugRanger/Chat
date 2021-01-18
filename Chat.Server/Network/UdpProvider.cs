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
        private const uint IOC_IN = 0x80000000; 
        private const uint IOC_VENDOR = 0x18000000; 
        private const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12; 

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
            unchecked
            {
                _listener.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }
            _listener.Bind(endPoint);

            _cancellation = new CancellationTokenSource();

            await ListenAsync(PreparePacket, _cancellation.Token);
        }

        public void Stop()
        {
            _listener?.Close();
        }

        private async Task ListenAsync(PreparePacket prepare, CancellationToken token)
        {
            token.Register(() => _listener?.Close());

            await Task.Run(() =>
            {
                var count = 0;
                var buffer = new byte[PACKET_SIZE];

                while (!token.IsCancellationRequested)
                {
                    int received = 0;
                    EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

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
            
            FreeToken();
            FreeSocket();
        }

        public void Send(IPEndPoint remote, ArraySegment<byte> bytes)
        {
            _listener.SendTo(bytes.Array, bytes.Offset, bytes.Count, remote);
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
