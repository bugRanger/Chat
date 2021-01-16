namespace Chat.Media.Codecs
{
    using System;

    using NAudio.Wave;

    public class PcmCodec : IAudioCodec
    {
        #region Properties

        public WaveFormat Format { get; }

        #endregion Properties

        #region Constructors

        public PcmCodec() 
        {
            Format = new WaveFormat(8000, 1);
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

        public void Dispose()
        {
            // Ignore.
        }

        #endregion Methods
    }
}
