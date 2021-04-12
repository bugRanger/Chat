namespace Chat.Audio.Codecs
{
    using System;


    public class PcmCodec : IAudioCodec
    {
        #region Properties

        public AudioFormat Format { get; }

        #endregion Properties

        #region Constructors

        public PcmCodec(AudioFormat format) 
        {
            Format = format;
        }

        #endregion Constructors

        #region Methods

        public byte[] Encode(ArraySegment<byte> compressed)
        {
            return compressed.ToArray();
        }

        public byte[] Decode(ArraySegment<byte> uncompressed)
        {
            return uncompressed.ToArray();
        }

        public byte[] Restore(ArraySegment<byte> compressed)
        {
            // TODO: Impl PLC.
            return new byte[Format.GetSamples()];
        }

        public void Dispose()
        {
            // Ignore.
        }

        #endregion Methods
    }
}
