namespace Chat.Audio.Mixer
{
    using System;
    using System.Collections.Generic;

    public class MixedChunk
    {
        #region Constants

        public const float DEFAULT_VOLUME = 0.75f;

        #endregion Constants

        #region Fields

        private readonly Dictionary<ISampleStream, float[]> _streamToSamples;
        private readonly float[] _mixture;

        #endregion Fields

        #region Properties

        public AudioFormat Format { get; }

        #endregion Properties

        #region Constructors

        public MixedChunk(AudioFormat format)
        {
            Format = format;

            _mixture = new float[format.GetSamples()];
            _streamToSamples = new Dictionary<ISampleStream, float[]>();
        }

        #endregion Constructors

        #region Methods

        public void Pack(IEnumerable<ISampleStream> streams)
        {
            foreach (var stream in streams)
            {
                if (_streamToSamples.ContainsKey(stream))
                {
                    return;
                }

                var samples = new float[_mixture.Length];

                int count = stream.Read(samples, 0, samples.Length);
                if (count != 0)
                {
                    Sum32Bit(samples, _mixture, DEFAULT_VOLUME);

                    _streamToSamples[stream] = samples;
                }
                else
                {
                    _streamToSamples[stream] = _mixture;
                }
            }
        }

        public void Unpack()
        {
            foreach (var pair in _streamToSamples)
            {
                ISampleStream stream = pair.Key;
                float[] samples = pair.Value;

                if (!ReferenceEquals(_mixture, samples))
                {
                    Sub32Bit(_mixture, samples, DEFAULT_VOLUME);
                }

                stream.Write(samples);
            }
        }
        public static void Sum32Bit(float[] source, float[] dest, float volume = DEFAULT_VOLUME)
        {
            for (int i = 0; i < dest.Length; i++)
            {
                dest[i] += volume * source[i];
            }
        }

        public static void Sub32Bit(float[] source, float[] dest, float volume = DEFAULT_VOLUME)
        {
            for (int i = 0; i < dest.Length; i++)
            {
                dest[i] = source[i] - (dest[i] * volume);
            }
        }

        #endregion Methods
    }

}
