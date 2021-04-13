namespace Chat.Audio
{
    using System;

    using NAudio.Wave;

    public interface IAudioStream : ISampleProvider
    {
        #region Methods

        void Write(ArraySegment<byte> buffer);

        int Read(byte[] buffer, int offset, int count);

        void Flush();

        #endregion Methods
    }
}
