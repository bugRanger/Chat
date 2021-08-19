namespace Chat.Net.Socket
{
    using System;
    using System.Net;

    public delegate void ReceivedFrom(IPEndPoint remote, byte[] bytes, ref int offset, int count);

    public interface INetworkСontroller
    {
        #region Events

        event ReceivedFrom ReceivedFrom;

        #endregion Events

        #region Methods

        void SendTo(IPEndPoint target, ArraySegment<byte> bytes);

        #endregion Methods
    }
}
