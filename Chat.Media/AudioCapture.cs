namespace Chat.Media
{
    using System;

    using NAudio.Wave;

    public class AudioCapture : IDisposable
    {
        #region Fields

        private readonly IAudioSender _audioSender;
        private readonly IAudioCodec _codec;
        private readonly IWaveIn _waveIn;

        #endregion Fields

        #region Constructors

        public AudioCapture(IAudioCodec codec, IAudioSender audioSender)
        {
            _codec = codec;
            _audioSender = audioSender;

            _waveIn = new WaveInEvent() 
            {
                BufferMilliseconds = 100,
                NumberOfBuffers = 2,
                WaveFormat = codec.Format,
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
