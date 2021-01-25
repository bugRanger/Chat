namespace Chat.Media
{
    using System;

    using NAudio.Wave;

    public class AudioCapture : IDisposable
    {
        #region Fields

        private readonly IWaveIn _waveIn;
        private bool _disposed;

        #endregion Fields

        #region Events

        public event Action<ArraySegment<byte>> Received;

        #endregion Events

        #region Constructors

        public AudioCapture(WaveFormat format)
        {
            _waveIn = new WaveInEvent() 
            {
                BufferMilliseconds = 100,
                NumberOfBuffers = 2,
                WaveFormat = format,
            };
            _waveIn.DataAvailable += OnAudioCaptured;
            _waveIn.StartRecording();
        }

        ~AudioCapture()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void OnAudioCaptured(object sender, WaveInEventArgs e)
        {
            Received?.Invoke(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _waveIn.DataAvailable -= OnAudioCaptured;
                _waveIn.StopRecording();
                _waveIn.Dispose();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
