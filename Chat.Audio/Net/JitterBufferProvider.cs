namespace Chat.Audio
{
    using System;

    using NAudio.Wave;

    using Chat.Net.Jitter;

    public class JitterBufferProvider : ISampleProvider
    {
        #region Fields

        private readonly BufferedWaveProvider _waveProvider;
        private readonly ISampleProvider _sampleProvider;
        private readonly IAudioCodec _codec;

        private JitterTimer<IAudioPacket> _jitter;
        private bool _disposed;

        #endregion Fields

        #region Properties

        public WaveFormat WaveFormat => _waveProvider.WaveFormat;

        #endregion Properties

        #region Constructors

        public JitterBufferProvider(IAudioCodec codec)
        {
            _codec = codec;
            _waveProvider = new BufferedWaveProvider(_codec.Format.ToWaveFormat());
            _waveProvider.DiscardOnBufferOverflow = false;

            _sampleProvider = _waveProvider.ToSampleProvider();

            _jitter = new JitterTimer<IAudioPacket>(codec.Format.Duration);
            _jitter.Completed += OnCompleted; 
        }

        #endregion Constructors

        #region Methods

        public void Enqueue(IAudioPacket packet)
        {
            _jitter.Append(packet);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _sampleProvider.Read(buffer, offset, count);
        }

        internal int Read(byte[] buffer, int offset, int count)
        {
            return _waveProvider.Read(buffer, offset, count);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void OnCompleted(IAudioPacket packet, bool recover)
        {
            byte[] uncompressed;

            if (recover)
            {
                uncompressed = _codec.Restore(packet?.Payload ?? null);
            }
            else
            {
                uncompressed = _codec.Decode(packet.Payload);
            }

            _waveProvider.AddSamples(uncompressed, 0, uncompressed.Length);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _jitter.Completed -= OnCompleted;
                _jitter.Dispose();
                _jitter = null;
            }

            _disposed = true;
        }

        #endregion Methods
    }
}