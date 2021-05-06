namespace Chat.Audio
{
    using System;

    using NAudio.Wave;

    public interface IWaveStream : IWaveProvider
    {
        #region Methods

        void Write(ArraySegment<byte> buffer);

        void Flush();

        ISampleProvider AsSampleProvider();

        #endregion Methods
    }
}
