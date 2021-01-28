namespace Chat.Audio
{
    using System;

    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    public class AudioPlayback : IDisposable
    {
        #region Fields

        private readonly MixingSampleProvider _waveProvider;
        private readonly IWavePlayer _waveOut;
        private bool _disposed;

        #endregion Fields

        #region Constructors

        public AudioPlayback(AudioFormat format)
        {
            _waveProvider = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels));
            _waveProvider.AddMixerInput(new BufferedWaveProvider(format.ToWaveFormat()));

            _waveOut = new DirectSoundOut(DirectSoundOut.DSDEVID_DefaultVoicePlayback);
            _waveOut.Init(_waveProvider);
            _waveOut.Play();
        }

        ~AudioPlayback() 
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
            _waveProvider.AddMixerInput(provider);
        }

        public void Remove(ISampleProvider provider)
        {
            _waveProvider.RemoveMixerInput(provider);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _waveOut.Stop();
                _waveOut.Dispose();

                _waveProvider.RemoveAllMixerInputs();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
