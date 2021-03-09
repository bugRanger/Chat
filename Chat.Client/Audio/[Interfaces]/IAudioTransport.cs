namespace Chat.Client.Audio
{
    using System;

    using Chat.Audio;

    public interface IAudioTransport
    {
        #region Methods

        void Send(IAudioPacket packet);

        #endregion Methods
    }
}
