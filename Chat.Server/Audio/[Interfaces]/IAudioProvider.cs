namespace Chat.Server
{
    using System;
    using System.Net;

    using Chat.Media;

    public interface IAudioProvider
    {
        #region Events

        event Action<IAudioPacket> Received;

        #endregion Events

        #region Methods

        void Send(IPEndPoint target, IAudioPacket packet);

        #endregion Methods
    }
}