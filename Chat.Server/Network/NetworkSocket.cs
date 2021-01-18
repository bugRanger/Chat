namespace Chat.Server.Network
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;

    public delegate ISocket SocketFactory(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType);

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

        public static NetworkSocket Create(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            return new NetworkSocket(addressFamily, socketType, protocolType);
        }

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

        public int SendTo(byte[] bytes, int offset, int count, EndPoint remote)
        {
            return _socket.SendTo(bytes, offset, count, SocketFlags.None, remote);
        }

        public int ReceiveFrom(byte[] bytes, int offset, int count, ref EndPoint remote)
        {
            return _socket.ReceiveFrom(bytes, offset, count, SocketFlags.None, ref remote);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
        {
            _socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        public int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue) 
        {
            return _socket.IOControl(ioControlCode, optionInValue, optionOutValue);
        }

        public void Close() 
        {
            _socket.Close();
        }

        protected virtual void Dispose(bool disposing)
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
