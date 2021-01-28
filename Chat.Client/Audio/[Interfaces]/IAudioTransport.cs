namespace Chat.Client.Audio
{
    using System;

    using Chat.Audio;

    public interface IAudioTransport
    {
        #region Events

        event Action<IAudioPacket> Received;

        #endregion Events

        #region Methods

        void Send(IAudioPacket packet);

        #endregion Methods
    }
}
