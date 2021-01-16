namespace Chat.Media
{
    using System;

    using NAudio.Wave;

    public class AudioPlayer : IDisposable
    {
        #region Fields

        private readonly IAudioCodec _codec;
        private readonly IAudioReceiver _receiver;
        private readonly IWavePlayer _waveOut;
        private readonly BufferedWaveProvider _waveProvider;

        #endregion Fields

        #region Constructors

        public AudioPlayer(IAudioCodec codec, IAudioReceiver receiver)
        {
            _codec = codec;
            _receiver = receiver;
            _receiver.Received += OnReceived;

            _waveProvider = new BufferedWaveProvider(codec.Format);
            _waveOut = new WaveOut();
            _waveOut.Init(_waveProvider);
            _waveOut.Play();
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            _receiver.Received -= OnReceived;
            _receiver.Dispose();
            _codec.Dispose();

            _waveOut.Dispose();
        }

        private void OnReceived(ArraySegment<byte> compressed)
        {
            byte[] decoded = _codec.Decode(compressed);
            _waveProvider.AddSamples(decoded, 0, decoded.Length);
        }

        #endregion Methods
    }
}
