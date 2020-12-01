namespace Chat.Server
{
    using System;
    using System.Net;

    public interface IConnection
    {
        #region Properties

        IPEndPoint RemoteEndPoint { get; }

        #endregion Properties

        #region Events

        event EventHandler<bool> Closing;

        #endregion Events

        #region Methods

        void Send(byte[] bytes);

        void Disconnect(bool inactive);

        #endregion Methods
    }
}
