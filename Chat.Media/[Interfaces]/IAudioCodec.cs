namespace Chat.Audio
{
    using System;
    
    public interface IAudioCodec : IDisposable
    {
        #region Properties

        AudioFormat Format { get; }

        #endregion Properties

        #region Methods

        byte[] Encode(ArraySegment<byte> uncompressed);

        byte[] Decode(ArraySegment<byte> compressed);

        byte[] Restore(ArraySegment<byte> compressed);

        #endregion Methods
    }
}
