namespace Chat.Server.Network
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;

    public class NetworkSocket : ISocket
    {
        #region Fields

        private Socket _socket;
        private bool _disposing;

        #endregion Fields

        #region Properties

        public EndPoint RemoteEndPoint => _socket?.RemoteEndPoint;

        #endregion Properties

        #region Constructors

        public NetworkSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
            : this(new Socket(addressFamily, socketType, protocolType)) 
        {
        }

        public NetworkSocket(Socket socket) 
        {
            _socket = socket;
            _disposing = false;
        }

        ~NetworkSocket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Constructors

        #region Methods

        public void Bind(EndPoint localEP)
        {
            _socket.Bind(localEP);
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);
        }

        public ISocket Accept()
        {
            return new NetworkSocket(_socket.Accept());
        }

        public Stream GetStream()
        {
            return new NetworkStream(_socket);
        }

        public void Close() 
        {
            _socket.Close();
        }

        protected void Dispose(bool disposing)
        {
            if (_disposing)
                return;

            if (disposing)
            {
                var socket = _socket;
                if (socket == null)
                    return;

                _socket = null;

                socket.Close();
                socket.Dispose();
            }

            _disposing = true;
        }

        #endregion Methods
    }
}
