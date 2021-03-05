namespace Chat.Client.Audio
{
    using System;

    using Chat.Audio;

    using NAudio.Wave;

    public class AudioRoute : IAudioRoute, IDisposable
    {
        #region Fields

        private readonly AudioBuffer _buffer;
        private readonly IAudioCodec _codec;
        private readonly IAudioTransport _transport;

        private uint _sequenceId;
        private bool _disposed;

        #endregion Fields

        #region Properties

        public int Id { get; set; }

        public WaveFormat WaveFormat { get; }

        #endregion Properties

        #region Constructors

        public AudioRoute(IAudioCodec codec, IAudioTransport transport)
        {
            _codec = codec;
            _transport = transport;
            _transport.Received += OnTransportReceived;

            _buffer = new AudioBuffer(codec);

            WaveFormat = _codec.Format.ToWaveFormat();
        }

        #endregion Constructors

        #region Methods

        public void Write(ArraySegment<byte> bytes)
        {
            // TODO thread queued.
            var compressed = _codec.Encode(bytes);

            var packet = new AudioPacket
            {
                SequenceId = ++_sequenceId,
                RouteId = Id,
                Payload = compressed,
            };

            _transport.Send(packet);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _buffer.Read(buffer, offset, count);
        }

        public void Dispose() 
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void OnTransportReceived(IAudioPacket packet)
        {
            if (packet.RouteId != packet.RouteId)
            {
                return;
            }

            _buffer.Enqueue(packet);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _transport.Received -= OnTransportReceived;

                _buffer.Dispose();
                _codec.Dispose();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
