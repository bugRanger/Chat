namespace Chat.Audio
{
    using System;

    using NAudio.Wave;

    public interface IAudioStream : ISampleProvider
    {
        #region Methods

        void Write(ArraySegment<byte> buffer);

        void Flush();

        #endregion Methods
    }
}
