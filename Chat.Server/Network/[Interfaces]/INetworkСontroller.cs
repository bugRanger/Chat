namespace Chat.Server
{
    using System;
    using System.Net;


    public delegate void PreparePacket(IPEndPoint remote, byte[] bytes, ref int offset, int count);

    public interface INetworkСontroller
    {
        #region Events

        event PreparePacket PreparePacket;

        #endregion Events

        #region Methods

        void Send(IPEndPoint target, byte[] bytes);

        #endregion Methods
    }
}
