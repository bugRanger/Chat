namespace Chat.Net.Socket
{
    using System;

    public delegate void Received(byte[] bytes, ref int offset, int count);

    public interface INetworkStream
    {
        #region Events

        event Received Received;

        #endregion Events

        #region Methods

        void Send(ArraySegment<byte> bytes);

        #endregion Methods
    }
}
