namespace Chat.Media
{
    using System;

    using NAudio.Wave;

    public class AudioCapture : IDisposable
    {
        #region Fields

        private readonly IAudioCodec _codec;
        private readonly IAudioSender _audioSender;
        private readonly WaveIn _waveIn;

        #endregion Fields

        #region Constructors

        public AudioCapture(IAudioCodec codec, int inputDeviceNumber, IAudioSender audioSender)
        {
            _codec = codec;
            _audioSender = audioSender;

            _waveIn = new WaveIn
            {
                DeviceNumber = inputDeviceNumber,
                WaveFormat = _codec.Format,
                BufferMilliseconds = 50,
            };
            _waveIn.DataAvailable += OnAudioCaptured;
            _waveIn.StartRecording();
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            _waveIn.DataAvailable -= OnAudioCaptured;
            _waveIn.StopRecording();
            _waveIn.Dispose();

            _audioSender.Dispose();
        }

        private void OnAudioCaptured(object sender, WaveInEventArgs e)
        {
            byte[] encoded = _codec.Encode(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded));
            _audioSender.Send(new ArraySegment<byte>(encoded));
        }

        #endregion Methods
    }
}
