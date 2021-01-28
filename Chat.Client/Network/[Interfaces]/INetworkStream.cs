namespace Chat.Client.Network
{
    using System;

    public delegate void PreparePacket(byte[] bytes, ref int offset, int count);

    public interface INetworkStream
    {
        #region Events

        event PreparePacket PreparePacket;

        #endregion Events

        #region Methods

        void Send(ArraySegment<byte> bytes);

        #endregion Methods
    }
}
