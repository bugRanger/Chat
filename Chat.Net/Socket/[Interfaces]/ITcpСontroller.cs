namespace Chat.Net.Socket
{
    using System;
    using System.Net;


    public interface ITcpСontroller : INetworkСontroller
    {
        #region Events

        event Action<IPEndPoint> ConnectionAccepted;
        event Action<IPEndPoint> ConnectionClosing;

        #endregion Events

        #region Methods

        void Disconnect(IPEndPoint remote);

        #endregion Methods
    }
}
