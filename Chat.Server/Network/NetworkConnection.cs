namespace Chat.Server.Network
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using NLog;

    public class NetworkConnection : IConnection, IDisposable
    {
        #region Constants

        private const int PACKET_SIZE = ushort.MaxValue;
        private const int ENABLED = 1;
        private const int DISABLE = 0;

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private Socket _socket;
        private NetworkStream _stream;
        private bool _disposed;
        private int _active;

        #endregion Fields

        #region Properties

        public IPEndPoint RemoteEndPoint { get; }

        #endregion Properties

        #region Events

        public event EventHandler<bool> Closing;

        #endregion Events

        #region Constructors

        public NetworkConnection(Socket socket)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _active = ENABLED;
            _socket = socket;
            _stream = new NetworkStream(socket);

            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        ~NetworkConnection()
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

        public void Send(byte[] bytes)
        {
            _stream.Write(bytes, 0, bytes.Length);
        }

        public async Task ListenAsync(PreparePacket prepare, CancellationToken token)
        {
            token.Register(() => Disconnect(false));

            await Task.Run(() =>
            {
                var count = 0;
                var buffer = new byte[PACKET_SIZE];

                while (!token.IsCancellationRequested)
                {
                    int received = 0;
                    try
                    {
                        received = _stream.Read(buffer, count, PACKET_SIZE - count);
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

                    prepare(RemoteEndPoint, buffer, ref offset, count);

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

        public void Disconnect(bool inactive)
        {
            if (Interlocked.CompareExchange(ref _active, DISABLE, ENABLED) == DISABLE)
                return;

            Closing?.Invoke(this, inactive);

            _socket.Close();

            FreeStream();
            FreeSocket();
        }

        private void FreeStream()
        {
            NetworkStream stream = _stream;
            if (stream == null)
                return;

            _stream = null;

            stream.Close();
            stream.Dispose();
        }

        private void FreeSocket()
        {
            var socket = _socket;
            if (socket == null)
                return;

            _socket = null;

            socket.Close();
            socket.Dispose();
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                FreeStream();
                FreeSocket();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
