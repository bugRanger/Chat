namespace Chat.Server
{
    using System;
    using System.Net;

    public delegate void PackedReceived(int routeId, ArraySegment<byte> bytes);

    public interface IAudioProvider
    {
        #region Events

        event PackedReceived Received;

        #endregion Events

        #region Methods

        void Send(IPEndPoint target, int routeId, ArraySegment<byte> bytes);

        #endregion Methods
    }
}