namespace Chat.Audio.Mixer
{
    using System;

    using NAudio.Wave;

    public class SampleStream : ISampleStream
    {
        #region Fields

        private readonly IWaveStream _waveStream;
        private readonly ISampleProvider _sampleProvider;

        private byte[] _writeBytes;

        #endregion Fields

        #region Properties

        public WaveFormat WaveFormat => _waveStream.WaveFormat;

        #endregion Properties

        #region Constructors

        public SampleStream(IWaveStream stream)
        {
            _waveStream = stream ?? throw new ArgumentNullException(nameof(stream));
            _sampleProvider = stream.ToSampleProvider();
        }

        #endregion Constructors

        #region Methods

        public int Read(float[] buffer, int offset, int count)
        {
            return _sampleProvider.Read(buffer, offset, count);
        }

        public void Write(ArraySegment<float> buffer)
        {
            int bytes = buffer.Count * 4;
            if (_writeBytes == null || bytes > _writeBytes.Length)
                _writeBytes = new byte[bytes];

            int offset = buffer.Offset;

            unsafe
            {
                fixed (byte* pBytes = &_writeBytes[0])
                {
                    float* pFloat = (float*)pBytes;
                    for (int n = 0, i = 0; n < bytes; n += 4, i++)
                    {
                        *(pFloat + i) = buffer[offset++];
                    }
                }
            }

            _waveStream.Write(_writeBytes);
        }

        public void Flush()
        {
            _waveStream.Flush();
        }

        #endregion Methods
    }
}
