namespace Chat.Media
{
    using System;

    using NAudio.Wave;

    public class AudioController : IAudioController, IDisposable
    {
        #region Fields

        private readonly AudioCapture _capture;
        private readonly AudioPlayer _player;

        private bool _disposed;

        #endregion Fields

        #region Properties

        public WaveFormat Format { get; set; }

        #endregion Properties

        #region Events

        public event Action<ArraySegment<byte>> Received;

        #endregion Events

        #region Constructors

        public AudioController(WaveFormat format)
        {
            Format = format;

            _player = new AudioPlayer(Format);
            _capture = new AudioCapture(Format);
            _capture.Received += OnCaptureReceived;
        }

        ~AudioController()
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

        public void Append(ISampleProvider provider) 
        {
            _player.Append(provider);
        }

        public void Remove(ISampleProvider provider)
        {
            _player.Remove(provider);
        }

        private void OnCaptureReceived(ArraySegment<byte> bytes)
        {
            Received?.Invoke(bytes);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _capture.Received -= OnCaptureReceived;
                _capture.Dispose();
                _player.Dispose();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
