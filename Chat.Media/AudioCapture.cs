namespace Chat.Media
{
    using System;

    using NAudio.Wave;

    public class AudioCapture : IDisposable
    {
        #region Constants

        private const int DEVICE_DEFAULT = 1;

        #endregion Constants

        #region Fields

        private readonly IAudioCodec _codec;
        private readonly IAudioSender _audioSender;
        private readonly IWaveIn _waveIn;

        #endregion Fields

        #region Constructors

        public AudioCapture(IAudioCodec codec, IAudioSender audioSender, int inputDeviceNumber = DEVICE_DEFAULT)
        {
            _codec = codec;
            _audioSender = audioSender;

            _waveIn = new WasapiLoopbackCapture();
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
            _codec.Dispose();
        }

        private void OnAudioCaptured(object sender, WaveInEventArgs e)
        {
            try
            {
                byte[] encoded = _codec.Encode(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded));
                _audioSender.Send(new ArraySegment<byte>(encoded));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion Methods
    }
}
