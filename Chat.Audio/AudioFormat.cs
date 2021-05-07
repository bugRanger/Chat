namespace Chat.Audio
{
    using System;

    using NAudio.Wave;

    public class AudioFormat
    {
        #region Constants

        private const int DEFAULT_SAMPLERATE = 48000;
        private const int DEFAULT_BIT_DETPTH = 16;
        private const int DEFAULT_DURATION = 20;
        private const int DEFAULT_CHANNELS = 1;

        #endregion Constants

        #region Properties

        public int SampleRate { get; set; }

        public ushort Duration { get; set; }

        public int BitDepth { get; set; }

        public int Channels { get; set; }

        #endregion Properties

        #region Constructor

        public AudioFormat() 
        {
            SampleRate = DEFAULT_SAMPLERATE;
            BitDepth = DEFAULT_BIT_DETPTH;
            Duration = DEFAULT_DURATION;
            Channels = DEFAULT_CHANNELS;
        }

        #endregion Constructor

        #region Methods

        public WaveFormat ToWaveFormat() 
        {
            return new WaveFormat(SampleRate, BitDepth, Channels);
        }

        public int GetBytes() 
        {
            return SampleRate / 1000 * Duration * (BitDepth / 8) * Channels;
        }

        public int GetSamples()
        {
            return GetBytes() / 4;
        }

        #endregion Methods
    }
}
