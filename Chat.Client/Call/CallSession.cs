namespace Chat.Client.Call
{
    using System;

    using Chat.Media;
    using NAudio.Wave;

    class CallSession : IDisposable
    {
        #region Constants

        private const int BUFFERING_MS = 150;

        #endregion Constants

        #region Fields

        private readonly IAudioCodec _codec;
        private readonly IAudioController _controller;
        private readonly AudioBuffer _buffer;

        private uint _sequenceId;
        private bool _disposed;

        #endregion Fields

        #region Properties

        public int Id { get; set; }

        public int RouteId { get; set; }

        #endregion Properties

        #region Events

        public event Action<IAudioPacket> PacketPrepared;

        #endregion Events

        #region Constructors

        public CallSession(IAudioController controller, Func<WaveFormat, IAudioCodec> codecFactory)
        {
            _codec = codecFactory(controller.Format);
            _buffer = new AudioBuffer(_codec, BUFFERING_MS);

            _controller = controller;
            _controller.Append(_buffer);
            _controller.Received += OnAudioReceived;
        }

        ~CallSession()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Handle(IAudioPacket packet)
        {
            if (packet.SessionId != Id)
                return;

            _buffer.Enqueue(packet);
        }

        private void OnAudioReceived(ArraySegment<byte> uncompressed)
        {
            // TODO Use enqueue.
            byte[] compressed = _codec.Encode(uncompressed);

            // TODO Move transport layer.
            var packet = new AudioPacket
            {
                SessionId = Id,
                RouteId = RouteId,
                Payload = compressed,
                SequenceId = ++_sequenceId,
            };

            PacketPrepared?.Invoke(packet);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _controller.Received -= OnAudioReceived;
                _controller.Remove(_buffer);

                _buffer.Dispose();
                _codec.Dispose();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
