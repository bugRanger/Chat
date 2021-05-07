namespace Chat.Audio.Mixer
{
    using System;

    using NAudio.Wave;

    public class SampleStream : ISampleStream
    {
        #region Fields

        private readonly IWaveStream _waveStream;

        private byte[] _readBytes;
        private byte[] _writeBytes;

        #endregion Fields

        #region Properties

        public WaveFormat WaveFormat => _waveStream.WaveFormat;

        #endregion Properties

        #region Constructors

        public SampleStream(IWaveStream stream)
        {
            _waveStream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        #endregion Constructors

        #region Methods

        public int Read(float[] buffer, int offset, int count)
        {
            int bytes = count * 4;
            if (_readBytes == null || bytes > _readBytes.Length)
                _readBytes = new byte[bytes];

            bytes = _waveStream.Read(_readBytes, 0, bytes);
            int samples = bytes / 4;

            unsafe
            {
                fixed (byte* pBytes = &_readBytes[0])
                {
                    float* pFloat = (float*)pBytes;
                    for (int n = 0, i = 0; n < bytes; n += 4, i++)
                    {
                        buffer[offset++] = *(pFloat + i);
                    }
                }
            }

            return samples;
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
