namespace Chat.Net.Socket
{
    using System;
    using System.Net;

    public interface ITcpConnection
    {
        #region Properties

        IPEndPoint RemoteEndPoint { get; }

        #endregion Properties

        #region Events

        event Action<ITcpConnection> Closing;

        #endregion Events

        #region Methods

        void Send(ArraySegment<byte> bytes);

        void Disconnect();

        #endregion Methods
    }
}
