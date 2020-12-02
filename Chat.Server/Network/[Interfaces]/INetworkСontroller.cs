namespace Chat.Server
{
    using System;
    using System.Net;


    public delegate bool PreparePacket(IPEndPoint remote, byte[] bytes, ref int offset, int count);

    public interface INetworkСontroller
    {
        #region Events

        event PreparePacket PreparePacket;
        event Action<IPEndPoint> ConnectionAccepted;
        event Action<IPEndPoint, bool> ConnectionClosing;

        #endregion Events

        #region Methods

        void Send(IPEndPoint target, byte[] bytes);

        void Disconnect(IPEndPoint remote, bool inactive);

        #endregion Methods
    }
}
