namespace Chat.Audio
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using NAudio.Wave;

    public class AudioCapture : IAudioConsumer, IDisposable
    {
        #region Fields

        private readonly HashSet<IAudioStream> _streams;
        private readonly IWaveIn _waveIn;
        private bool _disposed;

        #endregion Fields

        #region Events

        public event Action<ArraySegment<byte>> Received;

        #endregion Events

        #region Constructors

        public AudioCapture(AudioFormat format)
        {
            _streams = new HashSet<IAudioStream>();
            _waveIn = new WaveInEvent()
            {
                WaveFormat = format.ToWaveFormat(),
                BufferMilliseconds = format.Duration,
                NumberOfBuffers = 2,
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

        public void Append(IAudioStream stream)
        {
            if (_streams.Add(stream))
            {
                Received += stream.Write;
            }
        }

        public void Remove(IAudioStream stream)
        {
            if (_streams.Remove(stream))
            {
                Received -= stream.Write;
            }
        }

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

                foreach (var provider in _streams.ToArray())
                {
                    Remove(provider);
                }
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
