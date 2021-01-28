namespace Chat.Audio
{
    using System;

    using NAudio.Wave;

    public class AudioFormat 
    {
        #region Properties

        public int SampleRate { get; }

        public int Channels { get; }

        public int BitsPerSample { get; }

        #endregion Properties

        #region Constructor

        public AudioFormat(int rate, int channels = 1, int bits = 16) 
        {
            SampleRate = rate;
            Channels = channels;
            BitsPerSample = bits;
        }

        #endregion Constructor

        #region Methods

        public WaveFormat ToWaveFormat() 
        {
            return new WaveFormat(SampleRate, BitsPerSample, Channels);
        }

        #endregion Methods
    }
}
