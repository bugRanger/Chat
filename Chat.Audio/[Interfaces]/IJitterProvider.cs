namespace Chat.Audio
{
    using System;

    using NAudio.Wave;

    public interface IJitterProvider : IWaveProvider, IDisposable
    {
        #region Methods

        void Enqueue(IAudioPacket packet);

        #endregion Methods
    }
}