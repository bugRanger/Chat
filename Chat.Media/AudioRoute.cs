namespace Chat.Media
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using NAudio.Wave;

    public class AudioRoute : IDisposable
    {
        #region Fields

        private readonly IAudioSender _sender;
        private readonly IAudioReceiver _receiver;
        private readonly IAudioCodec _senderCodec;
        private readonly IAudioCodec _receiverCodec;
        private readonly BufferedWaveProvider _waveProvider;
        private readonly MediaFoundationResampler _resampler;
        private readonly CancellationTokenSource _cancellation;
        private readonly bool _forwarding;

        #endregion Fields

        #region Constructors

        public AudioRoute(IAudioCodec receivedCodec, IAudioReceiver receiver, IAudioSender sender, IAudioCodec senderCodec)
        {
            _receiverCodec = receivedCodec;
            _receiver = receiver;
            _receiver.Received += OnRecevied;

            _senderCodec = senderCodec;
            _sender = sender;

            _forwarding = receivedCodec.Format.Equals(senderCodec.Format);

            if (!_forwarding)
            {
                _cancellation = new CancellationTokenSource();
                _waveProvider = new BufferedWaveProvider(receivedCodec.Format);
                _resampler = new MediaFoundationResampler(_waveProvider, senderCodec.Format);
                _ = TranscodingAsync(_cancellation.Token);
            }
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();

            _receiver.Received -= OnRecevied;

            _resampler?.Dispose();
        }

        private async Task TranscodingAsync(CancellationToken token) 
        {
            var length = _resampler.WaveFormat.AverageBytesPerSecond * 20;

            var buffer = new byte[length];
            while (!token.IsCancellationRequested)
            {
                int bytesRead = _resampler.Read(buffer, 0, buffer.Length);
                if (bytesRead < length)
                {
                    await Task.Delay(10);
                    continue;
                }

                byte[] encoded = _senderCodec.Encode(new ArraySegment<byte>(buffer, 0, bytesRead));
                _sender.Send(new ArraySegment<byte>(encoded, 0, encoded.Length));
            }
        }

        private void OnRecevied(ArraySegment<byte> compressed)
        {
            if (_forwarding)
            {
                _sender.Send(compressed);
            }
            else
            {
                byte[] decoded = _receiverCodec.Decode(compressed);
                _waveProvider.AddSamples(decoded, 0, decoded.Length);
            }
        }

        #endregion Methods
    }
}
