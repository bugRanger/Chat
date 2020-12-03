namespace Chat.Server.Network
{
    using System;
    using System.IO;
    using System.Net;

    public interface ISocket : IDisposable
    {
        #region Properties

        EndPoint RemoteEndPoint { get; }

        #endregion Properties

        #region Methods

        void Bind(EndPoint localEP);

        void Listen(int backlog);

        ISocket Accept();

        Stream GetStream();

        void Close();

        #endregion Methods
    }
}
