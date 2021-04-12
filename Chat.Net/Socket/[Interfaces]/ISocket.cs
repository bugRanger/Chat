namespace Chat.Net.Socket
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;

    public interface ISocket : IDisposable
    {
        #region Properties

        EndPoint RemoteEndPoint { get; }

        EndPoint LocalEndPoint { get; }

        #endregion Properties

        #region Methods

        void Bind(EndPoint localEP);

        void Listen(int backlog);

        ISocket Accept();

        int Send(byte[] bytes, int offset, int count);

        int Receive(byte[] bytes, int offset, int count);

        int SendTo(byte[] bytes, int offset, int count, EndPoint remote);

        int ReceiveFrom(byte[] bytes, int offset, int count, ref EndPoint remote);

        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue);

        int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue);

        Stream GetStream();

        void Connect(EndPoint remote);

        void Close();

        #endregion Methods
    }
}
