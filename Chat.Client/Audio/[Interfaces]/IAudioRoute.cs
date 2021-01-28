namespace Chat.Client.Audio
{
    using System;

    using NAudio.Wave;

    public interface IAudioRoute : ISampleProvider
    {
        #region Methods

        void Write(ArraySegment<byte> buffer);

        #endregion Methods
    }
}
