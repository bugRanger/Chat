namespace Chat.Server.Network
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using NLog;

    public class TcpConnection : ITcpConnection, IDisposable
    {
        #region Constants

        private const int PACKET_SIZE = ushort.MaxValue;

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private ISocket _socket;
        private Stream _stream;
        private bool _disposed;

        #endregion Fields

        #region Properties

        public IPEndPoint RemoteEndPoint { get; }

        #endregion Properties

        #region Events

        public event Action<bool> Closing;

        #endregion Events

        #region Constructors

        public TcpConnection(ISocket socket)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _socket = socket;
            _stream = socket.GetStream();

            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        ~TcpConnection()
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

        public void Send(ArraySegment<byte> bytes)
        {
            _stream.Write(bytes.Array, bytes.Offset, bytes.Count);
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

                    prepare?.Invoke(RemoteEndPoint, buffer, ref offset, count);

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
            Closing?.Invoke(inactive);

            _socket.Close();

            FreeStream();
            FreeSocket();
        }

        private void FreeStream()
        {
            Stream stream = _stream;
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
