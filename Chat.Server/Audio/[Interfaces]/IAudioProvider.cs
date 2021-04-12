namespace Chat.Server
{
    using System;
    using System.Net;

    using Chat.Audio;

    public interface IAudioProvider
    {
        #region Events

        event Action<IPEndPoint, IAudioPacket> Received;

        #endregion Events

        #region Methods

        void SendTo(IPEndPoint target, IAudioPacket packet);

        #endregion Methods
    }
}