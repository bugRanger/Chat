namespace Chat.Server
{
    using System;
    using System.Net;

    public interface INetworkСontroller
    {
        #region Events

        event Action<IPEndPoint, bool> Closing;

        #endregion Events

        #region Methods

        void Send(IPEndPoint target, byte[] bytes);

        void Disconnect(IPEndPoint remote, bool inactive);

        #endregion Methods
    }
}
