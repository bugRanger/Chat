namespace Chat.Audio
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    using NAudio.Wave;

    public class AudioBuffer : ISampleProvider
    {
        #region Fields
        
        private readonly BufferedWaveProvider _waveProvider;
        private readonly ISampleProvider _sampleProvider;
        private readonly IAudioCodec _codec;

        #endregion Fields

        #region Properties

        public WaveFormat WaveFormat => _waveProvider.WaveFormat;

        #endregion Properties

        #region Constructors

        public AudioBuffer(IAudioCodec codec)
        {
            _codec = codec;
            _waveProvider = new BufferedWaveProvider(_codec.Format.ToWaveFormat());
            _waveProvider.DiscardOnBufferOverflow = false;

            _sampleProvider = _waveProvider.ToSampleProvider();
        }

        #endregion Constructors

        #region Methods

        public void Enqueue(IAudioPacket packet)
        {
            // TODO:
            // 1. Кадр уже есть, не смотря на время. Передаем его дальше.
            // 2. Есть еще время для ожидания кадра.
            // 3. Время вышло переходим к следующему кадру.

            var uncompressed = _codec.Decode(packet.Payload);
            _waveProvider.AddSamples(uncompressed, 0, uncompressed.Length);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _sampleProvider.Read(buffer, offset, count);
        }

        #endregion Methods
    }
}