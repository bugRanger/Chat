namespace Chat.Audio
{
    using System;

    using NAudio.Wave;

    public interface ISampleStream : ISampleProvider
    {
        #region Methods

        void Write(ArraySegment<float> buffer);

        void Flush();

        #endregion Methods
    }
}
