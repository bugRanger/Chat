namespace Chat.Server
{
    using System;
    using System.Net;


    public interface ITcpСontroller : INetworkСontroller
    {
        #region Events

        event Action<IPEndPoint> ConnectionAccepted;
        event Action<IPEndPoint, bool> ConnectionClosing;

        #endregion Events

        #region Methods

        void Disconnect(IPEndPoint remote, bool inactive);

        #endregion Methods
    }
}
