namespace Chat.Audio
{
    using System;

    using NAudio.Wave;

    using Chat.Net.Jitter;

    public class JitterQueueProvider : IJitterProvider
    {
        #region Fields

        private readonly JitterQueue<IAudioPacket> _jitter;
        private readonly IAudioCodec _codec;

        private bool _disposed;

        #endregion Fields

        #region Properties

        public WaveFormat WaveFormat { get; }

        #endregion Properties

        #region Constructors

        public JitterQueueProvider(IAudioCodec codec)
        {
            _codec = codec;
            _jitter = new JitterQueue<IAudioPacket>(JitterTimer<IAudioPacket>.JITTER_MAX_DURATION / codec.Format.Duration);

            WaveFormat = codec.Format.ToWaveFormat();
        }

        #endregion Constructors

        #region Methods

        public void Enqueue(IAudioPacket packet)
        {
            _jitter.Push(packet);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            // TODO: Не успеваем выгребать, необходимо исправить.
            if (!_jitter.Pull(false, out var packet).HasValue || packet == null)
                return 0;

            byte[] uncompressed = _codec.Decode(packet.Payload);
            Buffer.BlockCopy(uncompressed, 0, buffer, offset, count);

            return count;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                //_jitter = null;
            }

            _disposed = true;
        }

        #endregion Methods
    }
}